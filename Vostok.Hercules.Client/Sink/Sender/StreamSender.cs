using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Hercules.Client.Abstractions.Results;
using Vostok.Hercules.Client.Client;
using Vostok.Hercules.Client.Internal;
using Vostok.Hercules.Client.Sink.Analyzer;
using Vostok.Hercules.Client.Sink.Buffers;
using Vostok.Hercules.Client.Sink.Requests;
using Vostok.Hercules.Client.Sink.State;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Client.Sink.Sender
{
    internal class StreamSender : IStreamSender
    {
        private readonly Func<string> globalApiKeyProvider;
        private readonly IStreamState streamState;
        private readonly IBufferSnapshotBatcher snapshotBatcher;
        private readonly IRequestContentFactory contentFactory;
        private readonly IGateRequestSender gateRequestSender;
        private readonly IResponseAnalyzer responseAnalyzer;
        private readonly IStatusAnalyzer statusAnalyzer;
        private readonly ILog log;

        public StreamSender(
            [NotNull] Func<string> globalApiKeyProvider,
            [NotNull] IStreamState streamState,
            [NotNull] IBufferSnapshotBatcher snapshotBatcher,
            [NotNull] IRequestContentFactory contentFactory,
            [NotNull] IGateRequestSender gateRequestSender,
            [NotNull] IResponseAnalyzer responseAnalyzer,
            [NotNull] IStatusAnalyzer statusAnalyzer,
            [NotNull] ILog log)
        {
            this.globalApiKeyProvider = globalApiKeyProvider;
            this.streamState = streamState;
            this.snapshotBatcher = snapshotBatcher;
            this.contentFactory = contentFactory;
            this.gateRequestSender = gateRequestSender;
            this.responseAnalyzer = responseAnalyzer;
            this.statusAnalyzer = statusAnalyzer;
            this.log = log;
        }

        public async Task<StreamSendResult> SendAsync(TimeSpan perRequestTimeout, CancellationToken cancellationToken)
        {
            var watch = Stopwatch.StartNew();

            IBuffer someBuffer = null;
            if (streamState.MemoryAnalyzer.ShouldFreeMemory(streamState.BufferPool))
                streamState.BufferPool.TryAcquire(out someBuffer);

            HerculesStatus currentStatus;

            try
            {
                currentStatus = await SendInnerAsync(perRequestTimeout, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (someBuffer != null)
                {
                    if (someBuffer.UsefulDataSize == 0)
                        streamState.BufferPool.Free(someBuffer);
                    else
                        streamState.BufferPool.Release(someBuffer);
                }
            }

            return new StreamSendResult(currentStatus, watch.Elapsed);
        }

        private static void RequestGarbageCollection([NotNull] IEnumerable<BufferSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
                snapshot.Source.ReportGarbage(snapshot.State);
        }

        private async Task<HerculesStatus> SendInnerAsync(TimeSpan perRequestTimeout, CancellationToken cancellationToken)
        {
            var currentStatus = HerculesStatus.Success;

            var snapshots = CollectSnapshots();

            foreach (var batch in snapshotBatcher.Batch(snapshots))
            {
                cancellationToken.ThrowIfCancellationRequested();

                currentStatus = await SendBatchAsync(batch, perRequestTimeout, cancellationToken).ConfigureAwait(false);

                if (!statusAnalyzer.ShouldDropStoredRecords(currentStatus))
                    break;
            }

            return currentStatus;
        }

        [NotNull]
        private IEnumerable<BufferSnapshot> CollectSnapshots()
            => streamState.BufferPool
                .Select(buffer => buffer.TryMakeSnapshot())
                .Where(snapshot => snapshot != null)
                .Where(snapshot => snapshot.State.RecordsCount > 0);

        [CanBeNull]
        private string ObtainApiKey()
            => streamState.Settings.ApiKeyProvider?.Invoke() ?? globalApiKeyProvider();

        private async Task<HerculesStatus> SendBatchAsync([NotNull] IReadOnlyList<BufferSnapshot> batch, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var watch = Stopwatch.StartNew();

            using (var content = contentFactory.CreateContent(batch, out var recordsCount, out var recordsSize))
            {
                var response = await gateRequestSender
                    .SendAsync(streamState.Name, ObtainApiKey(), content, timeout, cancellationToken)
                    .ConfigureAwait(false);

                var status = response.Status;

                if (statusAnalyzer.ShouldDropStoredRecords(status))
                {
                    RequestGarbageCollection(batch);

                    if (status == HerculesStatus.Success)
                        streamState.Statistics.ReportSuccessfulSending(recordsCount, recordsSize);
                    else
                        streamState.Statistics.ReportSendingFailure(recordsCount, recordsSize);
                }

                if (status == HerculesStatus.Success)
                    LogBatchSendSuccess(recordsCount, recordsSize, watch.Elapsed);
                else
                    LogBatchSendFailure(recordsCount, recordsSize, status, response.ErrorDetails);

                return status;
            }
        }

        private void LogBatchSendSuccess(int recordsCount, long recordsSize, TimeSpan elapsed)
            => log.Info(
                "Successfully sent {RecordsCount} record(s) of size {RecordsSize} to stream '{StreamName}' in {ElapsedTime}.",
                recordsCount,
                recordsSize,
                streamState.Name,
                elapsed.ToPrettyString());

        private void LogBatchSendFailure(int recordsCount, long recordsSize, HerculesStatus status, string error)
        {
            if (status == HerculesStatus.Canceled)
                return;

            log.Warn(
                "Failed to send {RecordsCount} record(s) of size {RecordsSize} to stream '{StreamName}'. " +
                "Status: {Status}. Error: '{Error}'.",
                recordsCount,
                recordsSize,
                streamState.Name,
                status,
                error);

            if (statusAnalyzer.ShouldDropStoredRecords(status))
                log.Warn("Dropped {RecordsCount} record(s) as a result of non-retriable failure.", recordsCount);
        }
    }
}