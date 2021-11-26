using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Hercules.Client.Abstractions;
using Vostok.Hercules.Client.Abstractions.Queries;
using Vostok.Hercules.Client.Abstractions.Results;
using Vostok.Hercules.Client.Tests.Functional.Helpers;

namespace Vostok.Hercules.Client.Tests.Functional
{
    [TestFixture]
    internal class HerculesManagementClient_Tests
    {
        private static readonly TimeSpan Timeout = 30.Seconds();

        private readonly HerculesManagementClient managementClient = Helpers.Hercules.Management;

        [TestCase(1, 10000, null)]
        [TestCase(3, 40000, null)]
        [TestCase(1, 10000, "first", "second")]
        public void Should_create_stream_with_given_parameters(int partitions, int ttlMs, params string[] shardingKey)
        {
            var name = TestHelpers.GenerateStreamName();
            var query = new CreateStreamQuery(name)
            {
                Partitions = partitions,
                TTL = TimeSpan.FromMilliseconds(ttlMs),
                ShardingKey = shardingKey
            };

            try
            {
                managementClient.CreateStream(query, Timeout).EnsureSuccess();

                var info = managementClient.GetStreamDescription(name, Timeout).Payload;

                info.Name.Should().Be(name);
                info.Partitions.Should().Be(partitions);
                info.ShardingKey.Should().BeEquivalentTo(shardingKey);
                info.TTL.TotalMilliseconds.Should().Be(ttlMs);
            }
            finally
            {
                // NOTE: Doesn't work on Windows due to Kafka's guarantees
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    managementClient.DeleteStream(name, Timeout);
            }
        }

        [Platform("Unix", Reason = "Doesn't work on Windows because Kafka topic deletion doesn't work due to Kafka's guarantees")]
        [Test]
        public void Should_create_and_delete_stream()
        {
            string name;

            using (managementClient.CreateTemporaryStream(out name))
                GetStatus().Should().Be(HerculesStatus.Success);

            GetStatus().Should().Be(HerculesStatus.StreamNotFound);

            HerculesStatus GetStatus() => managementClient.GetStreamDescription(name, Timeout).Status;
        }
    }
}