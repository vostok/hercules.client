﻿using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Binary;
using Vostok.Commons.Collections;
using Vostok.Commons.Helpers.Disposable;
using Vostok.Commons.Time;
using Vostok.Hercules.Client.Abstractions;
using Vostok.Hercules.Client.Abstractions.Events;
using Vostok.Hercules.Client.Abstractions.Queries;
using Vostok.Hercules.Client.Abstractions.Results;
using Vostok.Hercules.Client.Internal;
using Vostok.Hercules.Client.Serialization.Builders;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Client
{
    /// <inheritdoc />
    [PublicAPI]
    public class HerculesGateClient : IHerculesGateClient
    {
        private const int InitialBodyBufferSize = 4096;

        private readonly UnboundedObjectPool<BinaryBufferWriter> writersPool
            = new UnboundedObjectPool<BinaryBufferWriter>(() => new BinaryBufferWriter(InitialBodyBufferSize) {Endianness = Endianness.Big});

        private readonly HerculesGateClientSettings settings;
        private readonly IGateRequestSender sender;
        private readonly ILog log;

        public HerculesGateClient([NotNull] HerculesGateClientSettings settings, [CanBeNull] ILog log)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.log = log = (log ?? LogProvider.Get()).ForContext<HerculesGateClient>();

            var bufferPool = new BufferPool(settings.MaxPooledBufferSize, settings.MaxPooledBuffersPerBucket);
            sender = new GateRequestSender(settings.Cluster, log, bufferPool, settings.AdditionalSetup);
        }

        /// <inheritdoc />
        public async Task<InsertEventsResult> InsertAsync(
            InsertEventsQuery query,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using (var disposable = writersPool.Acquire(out var buffer))
                {
                    buffer.Reset();

                    var content = new ValueDisposable<Content>(CreateContent(query, buffer), disposable);

                    var response = await sender
                        .SendAsync(query.Stream, settings.ApiKeyProvider(), content, timeout, cancellationToken)
                        .ConfigureAwait(false);

                    return response;
                }
            }
            catch (Exception error)
            {
                log.Warn(error);
                return new InsertEventsResult(HerculesStatus.UnknownError, error.Message);
            }
        }

        private static Content CreateContent(InsertEventsQuery query, BinaryBufferWriter buffer)
        {
            buffer.Write(query.Events.Count);

            foreach (var @event in query.Events)
            {
                using (var eventBuilder = new BinaryEventBuilder(buffer, () => PreciseDateTime.UtcNow, Constants.EventProtocolVersion))
                {
                    eventBuilder
                        .SetTimestamp(@event.Timestamp)
                        .AddTags(@event.Tags);
                }
            }

            return new Content(buffer.FilledSegment);
        }
    }
}