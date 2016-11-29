namespace Server.Wrappers
{
    using System;

    public class PacketAssembler : IDisposable
    {
        public PacketAssembler(int bytesToRead)
        {
            this.DataBuffer = Buffers.TakeBuffer(bytesToRead);
        }

        public bool Disposed { get; private set; }

        public byte[] DataBuffer { get; }

        public void CleanDataBuffer()
        {
            Buffers.ReturnBuffer(this.DataBuffer);
        }

        public void Dispose()
        {
            if (this.Disposed) return;

            this.CleanDataBuffer();
            this.Disposed = true;
        }
    }
}