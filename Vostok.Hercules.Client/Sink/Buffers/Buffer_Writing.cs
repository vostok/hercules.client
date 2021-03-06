using System;
using System.Text;
using Vostok.Commons.Binary;

namespace Vostok.Hercules.Client.Sink.Buffers
{
    internal partial class Buffer
    {
        public bool IsOverflowed { get; set; }

        public int Capacity => writer.Buffer.Length;

        public ArraySegment<byte> CommittedSegment => new ArraySegment<byte>(writer.Buffer, 0, Committed.Length);

        public long Position
        {
            get => writer.Position;
            set => writer.Position = value;
        }

        public Endianness Endianness
        {
            get => Endianness.Big;
            set => throw new NotSupportedException();
        }

        public void Write(short value)
        {
            if (!EnsureAvailableBytes(sizeof(short)))
                return;

            writer.Write(value);
        }

        public void Write(int value)
        {
            if (!EnsureAvailableBytes(sizeof(int)))
                return;

            writer.Write(value);
        }

        public void Write(long value)
        {
            if (!EnsureAvailableBytes(sizeof(long)))
                return;

            writer.Write(value);
        }

        public void Write(double value)
        {
            if (!EnsureAvailableBytes(sizeof(double)))
                return;

            writer.Write(value);
        }

        public void Write(float value)
        {
            if (!EnsureAvailableBytes(sizeof(float)))
                return;

            writer.Write(value);
        }

        public void Write(byte value)
        {
            if (!EnsureAvailableBytes(sizeof(byte)))
                return;

            writer.Write(value);
        }

        public void Write(bool value)
        {
            if (!EnsureAvailableBytes(sizeof(byte)))
                return;

            writer.Write(value);
        }

        public void Write(ushort value)
        {
            if (!EnsureAvailableBytes(sizeof(ushort)))
                return;

            writer.Write(value);
        }

        public void Write(uint value)
        {
            if (!EnsureAvailableBytes(sizeof(uint)))
                return;

            writer.Write(value);
        }

        public void Write(ulong value)
        {
            if (!EnsureAvailableBytes(sizeof(ulong)))
                return;

            writer.Write(value);
        }

        public void Write(Guid value)
        {
            const int size = 16;

            if (!EnsureAvailableBytes(size))
                return;

            writer.Write(value);
        }

        public int WriteVarlen(uint value)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!EnsureAvailableBytes(sizeof(uint) + 1))
                return 0;

            return writer.WriteVarlen(value);
        }

        public int WriteVarlen(ulong value)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!EnsureAvailableBytes(sizeof(ulong) + 1))
                return 0;

            return writer.WriteVarlen(value);
        }

        public void WriteWithLength(byte[] value)
        {
            if (!EnsureAvailableBytes(sizeof(int) + value.Length))
                return;

            writer.WriteWithLength(value);
        }

        public void WriteWithoutLength(byte[] value)
        {
            if (!EnsureAvailableBytes(value.Length))
                return;

            writer.WriteWithoutLength(value);
        }

        public void WriteWithLength(byte[] value, int offset, int length)
        {
            if (!EnsureAvailableBytes(sizeof(int) + length))
                return;

            writer.WriteWithLength(value, offset, length);
        }

        public void WriteWithoutLength(byte[] value, int offset, int length)
        {
            if (!EnsureAvailableBytes(length))
                return;

            writer.WriteWithoutLength(value, offset, length);
        }

        public void WriteWithLength(string value, Encoding encoding)
        {
            if (!EnsureAvailableBytes(sizeof(int) + encoding.GetMaxByteCount(value.Length)))
                return;

            writer.WriteWithLength(value, encoding);
        }

        public void WriteWithoutLength(string value, Encoding encoding)
        {
            if (!EnsureAvailableBytes(encoding.GetMaxByteCount(value.Length)))
                return;

            writer.WriteWithoutLength(value, encoding);
        }

        public void WriteWithoutLength(string value)
        {
            if (TryEnsureAvailableBytes(Encoding.UTF8.GetMaxByteCount(value.Length)))
                writer.WriteWithoutLength(value);

            if (!EnsureAvailableBytes(value.Length))
                return;

            WriteWithoutLength(Encoding.UTF8.GetBytes(value));
        }

        public void WriteWithLength(string value)
        {
            if (TryEnsureAvailableBytes(sizeof(int) + Encoding.UTF8.GetMaxByteCount(value.Length)))
                writer.WriteWithLength(value);

            if (!EnsureAvailableBytes(sizeof(int) + value.Length))
                return;

            WriteWithLength(Encoding.UTF8.GetBytes(value));
        }

        private bool EnsureAvailableBytes(int amount)
        {
            if (TryEnsureAvailableBytes(amount))
                return true;

            IsOverflowed = true;
            return false;
        }

        private bool TryEnsureAvailableBytes(int amount)
        {
            if (IsOverflowed)
                return false;

            var currentCapacity = writer.Buffer.Length;
            var maxPositionAfterWrite = writer.Position + amount;

            if (currentCapacity >= maxPositionAfterWrite)
                return true;

            if (maxPositionAfterWrite > maxSize)
                return false;

            if (currentCapacity + currentCapacity > maxSize)
                return TryResize(maxSize, currentCapacity);

            var reserveAmount = Math.Max(currentCapacity, (int)(maxPositionAfterWrite - currentCapacity));

            if (!memoryManager.TryReserveBytes(reserveAmount))
                return false;

            writer.Resize(currentCapacity + reserveAmount);

            return true;
        }

        private bool TryResize(int newCapacity, int currentCapacity)
        {
            if (!memoryManager.TryReserveBytes(newCapacity - currentCapacity))
                return false;

            writer.Resize(newCapacity);
            return true;
        }
    }
}