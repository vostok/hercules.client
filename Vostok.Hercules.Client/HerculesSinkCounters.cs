using JetBrains.Annotations;

namespace Vostok.Hercules.Client
{
    /// <summary>
    /// <para>Provides diagnostics information about <see cref="HerculesSink"/> overall or about one of the streams in it.</para>
    /// </summary>
    [PublicAPI]
    public class HerculesSinkCounters
    {
        /// <summary>
        /// Statistics about records that already have been sent.
        /// </summary>
        public (long Count, long Size) SentRecords { get; internal set; }

        /// <summary>
        /// Statistics about records that lost due to non-retriable sending errors.
        /// </summary>
        public (long Count, long Size) LostRecords { get; internal set; }

        /// <summary>
        /// Statistics about records that stored inside internal buffers and waiting to be sent.
        /// </summary>
        public (long Count, long Size) StoredRecords { get; internal set; }

        /// <summary>
        /// How many records are lost due to exception in record building delegate.
        /// </summary>
        public long WriteFailuresCount { get; internal set; }

        /// <summary>
        /// How many records are lost because they are large than <see cref="HerculesSinkSettings.MaximumRecordSize"/>.
        /// </summary>
        public long TooLargeRecordsCount { get; internal set; }

        /// <summary>
        /// How many records are lost due to memory limit violation.
        /// </summary>
        public long OverflowsCount { get; internal set; }

        /// <summary>
        /// Sums two <see cref="HerculesSinkCounters"/> field-by-field.
        /// </summary>
        public static HerculesSinkCounters operator+(HerculesSinkCounters a, HerculesSinkCounters b)
        {
            return new HerculesSinkCounters
            {

                LostRecords = Sum(a.LostRecords, b.LostRecords),
                SentRecords = Sum(a.SentRecords, b.SentRecords),
                StoredRecords = Sum(a.SentRecords, b.StoredRecords),
                OverflowsCount = a.OverflowsCount + b.OverflowsCount,
                WriteFailuresCount = a.WriteFailuresCount + b.WriteFailuresCount,
                TooLargeRecordsCount = a.TooLargeRecordsCount + b.TooLargeRecordsCount
            };
            
            (long, long) Sum((long, long) x, (long, long) y) => (x.Item1 + y.Item1, x.Item2 + y.Item2);
        }
    }
}