﻿using System.Runtime.CompilerServices;
using System.Text;
using Vostok.Hercules.Client.Abstractions.Events;

namespace Vostok.Hercules.Client.Serialization.Readers
{
    internal class BinaryBufferReader : Commons.Binary.BinaryBufferReader, IBinaryEventsReader
    {
        public BinaryBufferReader(byte[] buffer, long position)
            : base(buffer, position)
        {
        }

        public bool SkipMode { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ReadString(Encoding encoding)
        {
            if (SkipMode)
            {
                var size = ReadInt32();
                Position += size;
                return string.Empty;
            }

            return base.ReadString(encoding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ReadShortString(Encoding encoding)
        {
            if (SkipMode)
            {
                var size = ReadByte();
                Position += size;
                return string.Empty;
            }

            return base.ReadShortString(encoding);
        }

        public void ReadContainer(IHerculesTagsBuilder builder) =>
            EventsBinaryReader.ReadContainer(this, builder);
    }
}