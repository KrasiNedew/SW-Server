namespace Server.Wrappers
{
    using System;
    public class LengthReceiver : IDisposable
    {
        public const int LengthPrefixBytes = 5;

        public byte[] Buffer { get; private set; }

        public byte[] LengthData { get; private set; }

        public int BytesRead { get; set; }

        public int BytesToRead => LengthPrefixBytes - this.BytesRead;

        public bool Disposed { get; set; }

        public LengthReceiver()
        {
            this.Buffer = Buffers.TakeBuffer(LengthPrefixBytes);
            this.LengthData = Buffers.TakeBuffer(LengthPrefixBytes);
        }

        public void PushReceivedData(int bytesRead)
        {
            if (this.Buffer.Length == LengthPrefixBytes)
            {
                this.LengthData = this.Buffer;
                this.BytesRead = LengthPrefixBytes;
            }
            else
            {
                for (int i = 0, j = this.BytesRead; i < bytesRead; i++, j++)
                {
                    this.LengthData[j] = this.Buffer[i];
                }

                this.BytesRead += bytesRead;
                this.CleanBuffer();
            }
        }

        public void CleanBuffer()
        {
            byte[] temp = this.Buffer;
            this.Buffer = Buffers.TakeBuffer(this.BytesToRead);
            Buffers.ReturnBuffer(temp);
        }

        public void Dispose()
        {
            if (this.Disposed) return;

            Buffers.ReturnBuffer(this.Buffer);
            Buffers.ReturnBuffer(this.LengthData);
            this.Disposed = true;
        }
    }
}