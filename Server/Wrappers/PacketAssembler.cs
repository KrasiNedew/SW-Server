namespace Server.Wrappers
{
    using System.Collections.Generic;
    using System.Linq;

    public class PacketAssembler
    {
        public PacketAssembler()
        {
            this.Packets = new List<Packet>();
            this.DataBuffer = new byte[Packet.Size];
        }

        public byte[] DataBuffer { get; private set; }

        public List<Packet> Packets { get; private set; }

        public byte[] ReceivedRawData
        {
            get
            {
                return this.Packets.SelectMany(r => r.DataRaw).ToArray();
            }
        }

        public string ReceivedStringData
        {
            get
            {
                string val = string.Join(string.Empty, this.Packets.Select(p => p.GetStringFromRawData()));

                return val.EndsWith("<EOF>") ? val.Remove(val.Length - 5) : val;
            }
        }

        public void CleanDataBuffer()
        {
            this.DataBuffer = new byte[Packet.Size];
        }

        public void CleanPackets()
        {
            this.Packets = new List<Packet>();
        }
    }
}