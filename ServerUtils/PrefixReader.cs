namespace ServerUtils
{
    using System;

    public class PrefixReader : IDisposable
    {
        public const int PrefixBytes = 5;

        private readonly Buffers buffers;

        public PrefixReader(Buffers buffers)
        {
            this.buffers = buffers;
            this.Buffer = this.buffers.Take(PrefixBytes);
            this.PrefixData = this.buffers.Take(PrefixBytes);
        }

        public byte[] Buffer { get; private set; }

        public byte[] PrefixData { get; private set; }

        public int BytesRead { get; set; }

        public int BytesToRead => PrefixBytes - this.BytesRead;

        private bool Disposed { get; set; }

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
            this.Buffer = this.buffers.Take(this.BytesToRead);
            this.buffers.Return(temp);
        }

        public void Dispose()
        {
            if (this.Disposed) return;

            this.buffers.Return(this.Buffer);
            this.buffers.Return(this.PrefixData);
            this.Disposed = true;
        }
    }
}