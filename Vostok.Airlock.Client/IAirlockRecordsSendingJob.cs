﻿using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Airlock.Client
{
    internal interface IAirlockRecordsSendingJob
    {
        int SentRecordsCount { get; }

        IAirlockRecordsSendingJobSchedule Schedule { get; }

        Task RunAsync(CancellationToken cancellationToken = default);
    }
}