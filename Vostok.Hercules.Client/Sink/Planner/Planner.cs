using System;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Commons.Threading;
using Vostok.Hercules.Client.Sink.Sending;

namespace Vostok.Hercules.Client.Sink.Planner
{
    internal class Planner : IPlanner
    {
        private readonly AsyncManualResetEvent sendImmediately;
        private readonly TimeSpan sendPeriod;
        private readonly TimeSpan sendPeriodCap;

        private int unsuccessfulAttempts;

        public Planner(AsyncManualResetEvent sendImmediately, TimeSpan sendPeriod, TimeSpan sendPeriodCap)
        {
            this.sendImmediately = sendImmediately;
            this.sendPeriod = sendPeriod;
            this.sendPeriodCap = sendPeriodCap;
        }

        public Task WaitForNextSendAsync(StreamSendResult result, CancellationToken cancellationToken)
        {
            switch (result)
            {
                case StreamSendResult.Success:
                case StreamSendResult.NothingToSend:
                    unsuccessfulAttempts = 0;
                    break;
                case StreamSendResult.Failure:
                    unsuccessfulAttempts++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }

            return sendImmediately
                .WaitAsync(cancellationToken)
                .WaitAsync(GetDelayToNextOccurence());
        }

        private TimeSpan GetDelayToNextOccurence() =>
            Delays.ExponentialWithJitter(sendPeriodCap, sendPeriod, unsuccessfulAttempts);
    }
}