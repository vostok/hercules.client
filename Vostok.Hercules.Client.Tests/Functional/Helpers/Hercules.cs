using System;
using NUnit.Framework;
using Vostok.Hercules.Local;
using Vostok.Logging.Console;

namespace Vostok.Hercules.Client.Tests.Functional.Helpers
{
    public class Hercules : IDisposable
    {
        private readonly HerculesCluster cluster;

        public Hercules()
        {
            var log = new SynchronousConsoleLog();

            cluster = HerculesCluster.DeployNew(TestContext.CurrentContext.TestDirectory, log);
            
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
                GetApiKey);

            Management = new HerculesManagementClient(
                managementSettings,
                log);

            Sink = new HerculesSink(sinkSettings, log);

            Stream = new HerculesStreamClient(streamSettings, log);

            Gate = new HerculesGateClient(gateSettings, log);
        }
        
        public HerculesSink Sink { get; }
        public HerculesManagementClient Management { get; }
        public HerculesGateClient Gate { get; }
        public HerculesStreamClient Stream { get; }

        public void Dispose()
        {
            Sink?.Dispose();
            cluster?.Dispose();
        }
    }
}