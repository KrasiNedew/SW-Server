namespace Server
{
    using System.Net.Sockets;
    using System.Text;

    public class StateObject
    {
        // Size of receive Buffer.
        public const int BufferSize = 1024;

        // Socket  socket.
        public Socket WorkSocket = null;
        // Receive Buffer.
        public byte[] Buffer = new byte[BufferSize];
        // Packets data string.
        public StringBuilder Sb = new StringBuilder();
    }
}