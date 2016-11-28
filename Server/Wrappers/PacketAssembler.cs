namespace Server.Wrappers
{
    using System;
    using System.Collections.Generic;

    public class PacketAssembler : IDisposable
    {
        public PacketAssembler(int bytesToRead)
        {
            this.DataBuffer = Buffers.TakeBuffer(bytesToRead);
        }

        public byte[] DataBuffer { get; private set; }

        public void CleanDataBuffer()
        {
            Buffers.ReturnBuffer(this.DataBuffer);
        }

        public void Dispose()
        {
            this.CleanDataBuffer();
        }
    }
}