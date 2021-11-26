using System;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Collections;
using Vostok.Hercules.Local;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.Hercules.Client.Tests.Functional
{
    [SetUpFixture]
    public class Hercules : IDisposable
    {
        private static HerculesCluster cluster;
        private static readonly ILog Log = new SynchronousConsoleLog();

        [OneTimeSetUp]
        public void StartHercules()
        {
            const int retriesCount = 3;

            for (var i = 0; i < retriesCount; i++)
            {
                try
                {
                    cluster = HerculesCluster.DeployNew(TestContext.CurrentContext.TestDirectory, Log.WithMinimumLevel(LogLevel.Warn));
                }
                catch (Exception e)
                {
                    if (i + 1 == retriesCount)
                        throw new Exception($"Unable to start Hercules cluster after {retriesCount} tries.", e);
                    Log.Warn($"Retrying Hercules.Local deployment... Attempt #{i + 1}...");
                    cluster?.Dispose();
                    continue;
                }
                break;
            }

            string GetApiKey() => cluster.ApiKey;

            var managementSettings = new HerculesManagementClientSettings(
                cluster.HerculesManagementApiTopology,
                GetApiKey);

            var streamSettings = new HerculesStreamClientSettings(
                cluster.HerculesStreamApiTopology,
                GetApiKey);

            var gateSettings = new HerculesGateClientSettings(
                cluster.HerculesGateTopology,
                GetApiKey);

            var sinkSettings = new HerculesSinkSettings(
                cluster.HerculesGateTopology,
                GetApiKey)
            {
                SendPeriod = 1.Seconds()
            };

            Management = new HerculesManagementClient(
                managementSettings,
                Log);

            Sink = new HerculesSink(sinkSettings, Log);

            Stream = new HerculesStreamClient(streamSettings, Log);

            Gate = new HerculesGateClient(gateSettings, Log);
        }

        public static HerculesSink Sink { get; private set; }
        public static HerculesManagementClient Management { get; private set; }
        public static HerculesGateClient Gate { get; private set; }
        public static HerculesStreamClient Stream { get; private set; }

        [OneTimeTearDown]
        public void Dispose()
        {
            Log.Info("Rented in BufferPools: {Rented}.", BufferPool.Rented);

            Sink?.Dispose();
            cluster?.Dispose();
        }
    }
}