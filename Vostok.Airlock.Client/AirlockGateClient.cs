using System;
using System.Collections.Concurrent;
using System.Threading;
using Vostok.Airlock.Client.Abstractions;
using Vostok.Logging.Abstractions;

namespace Vostok.Airlock.Client
{
    public class AirlockGateClient : IAirlockGateClient
    {
        private readonly AirlockConfig config;
        private readonly ILog log;

        private readonly IMemoryManager memoryManager;
        private readonly ConcurrentDictionary<string, Lazy<IBufferPool>> bufferPools;
        private readonly IAirlockRecordWriter recordWriter;

        private int lostRecordsCounter;

        public AirlockGateClient(ILog log, AirlockConfig config)
        {
            this.log = log;
            this.config = config;

            memoryManager = new MemoryManager(this.config.MaximumMemoryConsumption.Bytes);
            bufferPools = new ConcurrentDictionary<string, Lazy<IBufferPool>>();
            recordWriter = new AirlockRecordWriter(this.log, (int) this.config.MaximumRecordSize.Bytes);
        }

        public int LostRecordsCount => lostRecordsCounter;

        public void Put(string stream, Action<IAirlockRecordBuilder> build)
        {
            var bufferPool = ObtainBufferPool(stream);

            if (!bufferPool.TryAcquire(out var buffer))
            {
                Interlocked.Increment(ref lostRecordsCounter);
                return;
            }

            try
            {
                var binaryWriter = buffer.BeginRecord();

                if (recordWriter.TryWrite(binaryWriter, build))
                {
                    buffer.Commit();
                }
                else
                {
                    Interlocked.Increment(ref lostRecordsCounter);
                }
            }
            finally
            {
                bufferPool.Release(buffer);
            }
        }
        
        private IBufferPool ObtainBufferPool(string stream)
        {
            return bufferPools.GetOrAdd(stream, _ => new Lazy<IBufferPool>(CreateBufferPool, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        private IBufferPool CreateBufferPool()
        {
            return new BufferPool(memoryManager, config.InitialPooledBuffersCount, (int) config.InitialPooledBufferSize.Bytes);
        }
    }
}