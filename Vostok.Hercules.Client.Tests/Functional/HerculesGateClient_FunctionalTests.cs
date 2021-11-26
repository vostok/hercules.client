using NUnit.Framework;
using Vostok.Hercules.Client.Abstractions;
using Vostok.Hercules.Client.Abstractions.Queries;
using Vostok.Hercules.Client.Tests.Functional.Helpers;

namespace Vostok.Hercules.Client.Tests.Functional
{
    [TestFixture]
    internal class HerculesGateClientFunctionalTests : HerculesSender_FunctionalTests
    {
        public HerculesGateClientFunctionalTests() =>
            PushEvent = (stream, e) => Helpers.Hercules.Gate.Insert(new InsertEventsQuery(stream, new[] {e.ToEvent()}), Timeout);

        [TestCase(10000, 1)]
        [TestCase(10000, 10)]
        [TestCase(50000, 2)]
        public void Should_write_many_events(int count, int threads)
        {
            var events = TestHelpers.GenerateEventBuilders(count).ToEvents();

            using (Helpers.Hercules.Management.CreateTemporaryStream(out var stream))
            {
                var query = new InsertEventsQuery(stream, events);

                Helpers.Hercules.Gate.Insert(query, Timeout);

                var actualEvents = Helpers.Hercules.Stream.ReadEvents(stream, count, count / 4);

                actualEvents.ShouldBeEqual(events);
            }
        }
    }
}