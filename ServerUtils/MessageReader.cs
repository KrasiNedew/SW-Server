namespace Server.Wrappers
{
    using System;

    public class MessageReader : IDisposable
    {
        public MessageReader(int bytesToRead)
        {
            this.DataBuffer = Buffers.Take(bytesToRead);
        }

        public bool Disposed { get; private set; }

        public byte[] DataBuffer { get; }

        public void CleanDataBuffer()
        {
            Buffers.Return(this.DataBuffer);
        }

        public void Dispose()
        {
            if (this.Disposed) return;

            this.CleanDataBuffer();
            this.Disposed = true;
        }
    }
}