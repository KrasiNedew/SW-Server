namespace Server.Wrappers
{
    using System;

    public class PacketAssembler : IDisposable
    {
        public const int PacketSize = 512;

        public PacketAssembler()
        {
            this.DataBuffer = BufferManager.Give();
            this.Data = new byte[PacketSize * 2];
        }

        public byte[] DataBuffer { get; private set; }

        public byte[] Data { get; private set; }

        public int BytesToRead { get; set; }

        public int BytesRead { get; set; }

        public void CleanDataBuffer()
        {
            // Storing the ref to the buffer.
            byte[] temp = this.DataBuffer;

            // Pushes the buffer on the blocking queue to be cleaned on background thread.
            // No time overhead.
            BufferManager.TakeBack(temp);

            // Take clean buffer immediately
            this.DataBuffer = BufferManager.Give();
        }

        public void PushReceivedData(int bytesReceived)
        {
            if (this.BytesRead + bytesReceived > this.Data.Length)
            {
                throw new InvalidOperationException("Service length header not found");
            }

            for (int i = this.BytesRead, j = 0; j < bytesReceived; i++, j++)
            {
                this.Data[i] = this.DataBuffer[j];
            }

            this.BytesRead += bytesReceived;
            this.CleanDataBuffer();
        }

        public void AllocateSpace(int bytes)
        {
            byte[] temp = new byte[this.Data.Length];
            this.Data.CopyTo(temp, 0);

            this.Data = new byte[bytes];

            int transfer = Math.Min(temp.Length, this.Data.Length);
            for (int i = 0; i < transfer; i++)
            {
                this.Data[i] = temp[i];
            }
        }

        public void Dispose()
        {
            this.Data = null;
            BufferManager.TakeBack(this.DataBuffer);
        }
    }
}