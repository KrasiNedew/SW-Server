namespace Server.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ModelDTOs.Enums;

    using Server.Handlers;

    public class PacketAssembler
    {
        public const int PacketSize = 512;

        public PacketAssembler()
        {
            this.DataBuffer = new byte[PacketSize];
            this.Data = new byte[PacketSize*2];
        }

        public byte[] DataBuffer { get; private set; }

        public byte[] Data { get; private set; }

        public int BytesToRead { get; set; }

        public int BytesRead { get; set; }

        public void CleanDataBuffer()
        {
            this.DataBuffer = new byte[PacketSize];
        }

        public void PushReceivedData(int bytesReceived)
        {
            if (this.BytesRead + bytesReceived > this.Data.Length)
            {
                throw new InvalidOperationException("Request length header not found");
            }

            for (int i = this.BytesRead, j = 0; j < bytesReceived; i++, j++)
            {
                this.Data[i] = this.DataBuffer[j];
            }

            this.BytesRead += bytesReceived;
            this.CleanDataBuffer();
        }

        public void CleanData()
        {
            this.Data = new byte[PacketSize];
            this.BytesRead = 0;
            this.CleanDataBuffer();
        }

        public void AllocateSpaceForReceiving(int bytes)
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
    }
}