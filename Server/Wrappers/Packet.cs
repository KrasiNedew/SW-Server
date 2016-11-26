namespace Server.Wrappers
{
    using System;
    using System.Text;

    public class Packet
    {
        public const int Size = 512;

        private byte[] dataRaw;

        public Packet(byte[] data)
        {
            this.DataRaw = data;
        }

        public byte[] DataRaw
        {
            get
            {
                return this.dataRaw;
            }

            set
            {
                if (value.Length > Size)
                {
                    Array.Resize(ref value, Size);
                }

                this.dataRaw = value;
            }
        }

        public string GetStringFromRawData()
        {
            return Encoding
                .ASCII
                .GetString(this.DataRaw, 0, this.DataRaw.Length)
                .TrimEnd('\0');
        }
    }
}