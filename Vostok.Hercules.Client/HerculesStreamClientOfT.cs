﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kontur.Lz4;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport;
using Vostok.Commons.Binary;
using Vostok.Commons.Collections;
using Vostok.Hercules.Client.Abstractions;
using Vostok.Hercules.Client.Abstractions.Events;
using Vostok.Hercules.Client.Abstractions.Models;
using Vostok.Hercules.Client.Abstractions.Queries;
using Vostok.Hercules.Client.Abstractions.Results;
using Vostok.Hercules.Client.Client;
using Vostok.Hercules.Client.Helpers;
using Vostok.Hercules.Client.Serialization.Readers;
using Vostok.Hercules.Client.Serialization.Writers;
using Vostok.Logging.Abstractions;
using BinaryBufferReader = Vostok.Commons.Binary.BinaryBufferReader;

namespace Vostok.Hercules.Client
{
    /// <inheritdoc />
    [PublicAPI]
    public class HerculesStreamClient<T> : IHerculesStreamClient<T>
    {
        private readonly Func<IBinaryBufferReader, IHerculesEventBuilder<T>> eventBuilderProvider;
        private readonly ResponseAnalyzer responseAnalyzer;
        private readonly BufferPool bufferPool;
        private readonly IClusterClient client;
        private readonly ILog log;

        public HerculesStreamClient([NotNull] HerculesStreamClientSettings<T> settings, [CanBeNull] ILog log)
        {
            this.log = log = (log ?? LogProvider.Get()).ForContext<HerculesStreamClient>();

            bufferPool = new BufferPool(settings.MaxPooledBufferSize, settings.MaxPooledBuffersPerBucket);

            client = ClusterClientFactory.Create(
                settings.Cluster,
                log,
                Constants.ServiceNames.StreamApi,
                config =>
                {
                    config.SetupUniversalTransport(
                        new UniversalTransportSettings
                        {
                            BufferFactory = bufferPool.Rent
                        });
                    config.AddRequestTransform(new ApiKeyRequestTransform(settings.ApiKeyProvider));
                    config.AddResponseTransform(TryDecompress);
                    settings.AdditionalSetup?.Invoke(config);
                });

            responseAnalyzer = new ResponseAnalyzer(ResponseAnalysisContext.Stream);
            eventBuilderProvider = settings.EventBuilderProvider;
        }

        /// <inheritdoc />
        public async Task<ReadStreamResult<T>> ReadAsync(ReadStreamQuery query, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ClusterResult result = null;
            try
            {
                result = await ReadStreamAsync(query, timeout, cancellationToken).ConfigureAwait(false);

                var operationStatus = responseAnalyzer.Analyze(result.Response, out var errorMessage);
                if (operationStatus != HerculesStatus.Success)
                    return new ReadStreamResult<T>(operationStatus, null, errorMessage);

                return new ReadStreamResult<T>(operationStatus, ParseReadResponseBody(result.Response));
            }
            catch (Exception error)
            {
                log.Warn(error);
                return new ReadStreamResult<T>(HerculesStatus.UnknownError, null, error.Message);
            }
            finally
            {
                if (result?.Response.HasContent == true)
                    bufferPool.Return(result.Response.Content.Buffer);
            }
        }

        public async Task<ReadStreamIEnumerableResult<T>> ReadIEnumerableAsync(ReadStreamQuery query, TimeSpan timeout, CancellationToken cancellationToken = new CancellationToken())
        {
            ClusterResult result = null;

            try
            {
                result = await ReadStreamAsync(query, timeout, cancellationToken).ConfigureAwait(false);

                var operationStatus = responseAnalyzer.Analyze(result.Response, out var errorMessage);
                if (operationStatus != HerculesStatus.Success)
                    return new ReadStreamIEnumerableResult<T>(operationStatus, null, errorMessage);

                return new ReadStreamIEnumerableResult<T>(operationStatus, ParseReadIEnumerableResponseBody(result, query.Coordinates));
            }
            catch (Exception error)
            {
                log.Warn(error);

                if (result?.Response.HasContent == true)
                    bufferPool.Return(result.Response.Content.Buffer);

                return new ReadStreamIEnumerableResult<T>(HerculesStatus.UnknownError, null, error.Message);
            }
        }

        public async Task<SeekToEndStreamResult> SeekToEndAsync(SeekToEndStreamQuery query, TimeSpan timeout, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var url = new RequestUrlBuilder("stream/seekToEnd")
                    {
                        {Constants.QueryParameters.Stream, query.Name},
                        {Constants.QueryParameters.ClientShard, query.ClientShard},
                        {Constants.QueryParameters.ClientShardCount, query.ClientShardCount}
                    }
                    .Build();

                var request = Request
                    .Get(url);

                var result = await client
                    .SendAsync(request, timeout, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                try
                {
                    var operationStatus = responseAnalyzer.Analyze(result.Response, out var errorMessage);
                    if (operationStatus != HerculesStatus.Success)
                        return new SeekToEndStreamResult(operationStatus, null, errorMessage);

                    return new SeekToEndStreamResult(operationStatus, ParseSeekToEndResponseBody(result.Response), errorMessage);
                }
                finally
                {
                    if (result.Response.HasContent)
                        bufferPool.Return(result.Response.Content.Buffer);
                }
            }
            catch (Exception error)
            {
                log.Warn(error);
                return new SeekToEndStreamResult(HerculesStatus.UnknownError, null, error.Message);
            }
        }

        private async Task<ClusterResult> ReadStreamAsync(ReadStreamQuery query, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var url = new RequestUrlBuilder("stream/read")
                {
                    {Constants.QueryParameters.Stream, query.Name},
                    {Constants.QueryParameters.Limit, query.Limit},
                    {Constants.QueryParameters.ClientShard, query.ClientShard},
                    {Constants.QueryParameters.ClientShardCount, query.ClientShardCount}
                }
                .Build();

            var body = CreateRequestBody(query.Coordinates ?? StreamCoordinates.Empty);

            var request = Request
                .Post(url)
                .WithContentTypeHeader(Constants.ContentTypes.OctetStream)
                .WithAcceptEncodingHeader(Constants.Compression.Lz4Encoding)
                .WithContent(body);

            var result = await client
                .SendAsync(request, timeout, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return result;
        }

        private static ArraySegment<byte> CreateRequestBody([NotNull] StreamCoordinates coordinates)
        {
            var writer = new BinaryBufferWriter(sizeof(int) + coordinates.Positions.Length * (sizeof(int) + sizeof(long)))
            {
                Endianness = Endianness.Big
            };

            StreamCoordinatesWriter.Write(coordinates, writer);

            return writer.FilledSegment;
        }

        private static SeekToEndStreamPayload ParseSeekToEndResponseBody([NotNull] Response response)
        {
            var reader = new BinaryBufferReader(response.Content.Buffer, response.Content.Offset)
            {
                Endianness = Endianness.Big
            };

            var coordinates = StreamCoordinatesReader.Read(reader);

            return new SeekToEndStreamPayload(coordinates);
        }

        private ReadStreamPayload<T> ParseReadResponseBody([NotNull] Response response)
        {
            var reader = new BinaryBufferReader(response.Content.Buffer, response.Content.Offset)
            {
                Endianness = Endianness.Big
            };

            var coordinates = StreamCoordinatesReader.Read(reader);

            var events = EventsBinaryReader.Read(response.Content.Buffer, reader.Position, eventBuilderProvider, log).ToList();

            return new ReadStreamPayload<T>(events, coordinates);
        }

        private ReadStreamIEnumerablePayload<T> ParseReadIEnumerableResponseBody(ClusterResult result, StreamCoordinates queryCoordinates)
        {
            var response = result.Response;

            var reader = new BinaryBufferReader(response.Content.Buffer, response.Content.Offset)
            {
                Endianness = Endianness.Big
            };

            var next = StreamCoordinatesReader.Read(reader);
            var current = StreamCoordinatesHelper.FixQueryCoordinates(queryCoordinates, next);

            var events = EventsBinaryReader.Read(response.Content.Buffer, reader.Position, eventBuilderProvider, log);

            return new ReadStreamIEnumerablePayload<T>(events, current, next, () =>
                bufferPool.Return(result.Response.Content.Buffer));
        }

        private Response TryDecompress(Response response)
        {
            if (!response.HasContent
                || response.Headers[HeaderNames.ContentEncoding] != Constants.Compression.Lz4Encoding
                || !int.TryParse(response.Headers[Constants.Compression.OriginalContentLengthHeaderName], out var originalContentLength))
                return response;

            return response
                .WithContent(Decompress(response.Content, originalContentLength));
        }

        private Content Decompress(Content content, int originalContentLength)
        {
            var buffer = bufferPool.Rent(originalContentLength);

            var decodedContentLenght = LZ4Codec.Decode(content.Buffer, content.Offset, content.Length, buffer, 0, originalContentLength, true);
            if (decodedContentLenght != originalContentLength)
            {
                bufferPool.Return(buffer);
                throw new Exception($"Failed to decompress {content.Length} bytes: expected exactly {originalContentLength} bytes, but received {decodedContentLenght} bytes.");
            }

            bufferPool.Return(content.Buffer);
            return new Content(buffer, 0, originalContentLength);
        }
    }
}