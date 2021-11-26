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
        private static ILog log;

        [OneTimeSetUp]
        public void StartHercules()
        {
            log = new SynchronousConsoleLog();

            cluster = HerculesCluster.DeployNew(TestContext.CurrentContext.TestDirectory, log.WithMinimumLevel(LogLevel.Warn));

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
                log);

            Sink = new HerculesSink(sinkSettings, log);

            Stream = new HerculesStreamClient(streamSettings, log);

            Gate = new HerculesGateClient(gateSettings, log);
        }

        public static HerculesSink Sink { get; private set; }
        public static HerculesManagementClient Management { get; private set; }
        public static HerculesGateClient Gate { get; private set; }
        public static HerculesStreamClient Stream { get; private set; }

        [OneTimeTearDown]
        public void Dispose()
        {
            log.Info("Rented in BufferPools: {Rented}.", BufferPool.Rented);

            Sink?.Dispose();
            cluster?.Dispose();
        }
    }
}