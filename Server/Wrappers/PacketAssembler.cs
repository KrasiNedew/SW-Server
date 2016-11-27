namespace Server.Wrappers
{
    using System.Collections.Generic;
    using ModelDTOs.Enums;

    public class PacketAssembler
    {
        public const int PacketSize = 512;

        public PacketAssembler()
        {
            this.DataBuffer = new byte[PacketSize];
            this.Data = new byte[0];
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
            for (int i = this.BytesRead, j = 0; j < bytesReceived; i++, j++)
            {
                this.Data[i] = this.DataBuffer[j];
            }
        }

        public void CleanData()
        {
            this.Data = new byte[0];
        }

        public void AllocateSpaceForReceiving(int bytes)
        {
            this.Data = new byte[bytes];
        }
    }
}