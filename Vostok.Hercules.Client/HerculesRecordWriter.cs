﻿using System;
using Vostok.Commons.Primitives;
using Vostok.Hercules.Client.Abstractions.Events;
using Vostok.Hercules.Client.Binary;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Client
{
    internal class HerculesRecordWriter : IHerculesRecordWriter
    {
        private readonly ILog log;
        private readonly Func<DateTimeOffset> timeProvider;
        private readonly byte recordVersion;
        private readonly int maxRecordSize;

        public HerculesRecordWriter(ILog log, Func<DateTimeOffset> timeProvider, byte recordVersion, int maxRecordSize)
        {
            this.log = log;
            this.timeProvider = timeProvider;
            this.recordVersion = recordVersion;
            this.maxRecordSize = maxRecordSize;
        }

        public bool TryWrite(IHerculesBinaryWriter binaryWriter, Action<IHerculesEventBuilder> build, out int recordSize)
        {
            var startingPosition = binaryWriter.Position;

            try
            {
                binaryWriter.IsOverflowed = false;
                binaryWriter.Write(recordVersion);

                using (var builder = new HerculesEventBuilder(binaryWriter, timeProvider))
                    build.Invoke(builder);

                if (binaryWriter.IsOverflowed)
                {
                    binaryWriter.Position = startingPosition;
                    recordSize = 0;
                    return false;
                }
            }
            catch (Exception exception)
            {
                binaryWriter.Position = startingPosition;
                recordSize = 0;
                log.Error(exception);
                return false;
            }

            recordSize = (int)(binaryWriter.Position - startingPosition);

            if (recordSize <= maxRecordSize)
                return true;

            log.Warn("Discarded record with size {RecordSize} larger than maximum allowed size {MaximumRecordSize}", DataSize.FromBytes(recordSize), DataSize.FromBytes(maxRecordSize));
            binaryWriter.Position = startingPosition;
            return false;
        }
    }
}