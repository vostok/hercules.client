﻿using System;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Commons.Time;

namespace Vostok.Hercules.Client
{
    public class HerculesSinkConfig
    {        
        public byte RecordVersion => 1;
        public long MaximumRecordSizeBytes { get; set; } = 1 * DataSizeConstants.Megabyte;
        public long MaximumMemoryConsumptionBytes { get; set; } = 128 * DataSizeConstants.Megabyte;
        public long InitialPooledBufferSizeBytes { get; set; } = 16 * DataSizeConstants.Kilobyte;
        public int InitialPooledBuffersCount { get; set; } = 32;
        public long MaximumRequestContentSizeBytes { get; set; } = 4 * DataSizeConstants.Megabyte;
        public TimeSpan RequestSendPeriod { get; set; } = 2.Seconds();
        public TimeSpan RequestSendPeriodCap { get; set; } = 1.Minutes();
        public TimeSpan RequestTimeout { get; set; } = 30.Seconds();
        
        public HerculesGate Gate { get; set; } = new HerculesGate();
    }

    public class HerculesGate
    {
        public string Name { get; set; } = "HerculesGateway";
        
        public IClusterProvider Cluster { get; set; }
        
        public Func<string> ApiKey { get; set; }
    }
}