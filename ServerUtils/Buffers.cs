namespace ServerUtils
{
    using System;
    using System.Collections.Concurrent;
    using System.ServiceModel.Channels;
    using System.Threading;
    using System.Threading.Tasks;

    public class Buffers
    {
        private readonly BufferManager buffersManager;

        private readonly BlockingCollection<byte[]> buffersClearance;

        public Buffers(int maxPoolSize, int maxBufferSize)
        {
            this.buffersManager = BufferManager.CreateBufferManager(maxPoolSize, maxBufferSize);
            this.buffersClearance = new BlockingCollection<byte[]>();

            Task.Run(() => { this.Cleaner(); });
        }

        public byte[] Take(int size)
        {
            return this.buffersManager.TakeBuffer(size);
        }

        public void Return(byte[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            this.buffersClearance.Add(buffer);
        }

        private void Cleaner()
        {
            bool available;
            do
            {
                byte[] buffer;
                available = this.buffersClearance.TryTake(out buffer, Timeout.Infinite);
                if (available)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    this.buffersManager.ReturnBuffer(buffer);
                }
            }
            while (available);
        }
    }
}