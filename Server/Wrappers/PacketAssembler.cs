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

        public byte[] DataBuffer { get; set; }

        public List<Packet> Packets { get; set; }

        public byte[] CurrentRawData
        {
            get
            {
                return this.Packets.SelectMany(r => r.DataRaw).ToArray();
            }
        }

        public string CurrentStringData
        {
            get
            {
                string val = string.Join(string.Empty, this.Packets.Select(p => p.GetStringFromRawData()));

                return val.EndsWith("<EOF>") ? val.Remove(val.Length - 5) : val;
            }
        }
    }
}