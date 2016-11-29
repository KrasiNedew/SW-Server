namespace Server.Wrappers
{
    using System;
    public class PrefixReader : IDisposable
    {
        public const int PrefixBytes = 5;

        public byte[] Buffer { get; private set; }

        public byte[] PrefixData { get; private set; }

        public int BytesRead { get; set; }

        public int BytesToRead => PrefixBytes - this.BytesRead;

        public bool Disposed { get; set; }

        public PrefixReader()
        {
            this.Buffer = Buffers.Take(PrefixBytes);
            this.PrefixData = Buffers.Take(PrefixBytes);
        }

        public void PushReceivedData(int bytesRead)
        {
            if (this.Buffer.Length == PrefixBytes)
            {
                this.PrefixData = this.Buffer;
                this.BytesRead = PrefixBytes;
            }
            else
            {
                for (int i = 0, j = this.BytesRead; i < bytesRead; i++, j++)
                {
                    this.PrefixData[j] = this.Buffer[i];
                }

                this.BytesRead += bytesRead;
                this.CleanBuffer();
            }
        }

        public void CleanBuffer()
        {
            byte[] temp = this.Buffer;
            this.Buffer = Buffers.Take(this.BytesToRead);
            Buffers.Return(temp);
        }

        public void Dispose()
        {
            if (this.Disposed) return;

            Buffers.Return(this.Buffer);
            Buffers.Return(this.PrefixData);
            this.Disposed = true;
        }
    }
}