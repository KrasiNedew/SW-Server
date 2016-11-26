namespace Server.Wrappers
{
    using System.Net.Sockets;
    using System.Text;

    using ModelDTOs;

    public class ConnectedClient
    {
        public Socket Socket { get; }

        public AuthData AuthData { get; set; }

        public PacketAssembler PacketAssembler { get; set; }

        public bool Validated { get; set; }

        public ConnectedClient(Socket socket)
        {
            this.Socket = socket;
            this.PacketAssembler = new PacketAssembler();
            this.Validated = false;
        }

        public void Close()
        {
            this.Socket.Close();
        }
    }
}