namespace Server.Wrappers
{
    using System;
    using System.Net.Sockets;

    using ServerUtils;

    public class Client : IDisposable
    {
        public Socket Socket { get; }

        public AuthDataSecure AuthData { get; set; }

        public bool Validated { get; set; }

        public Client(Socket socket)
        {
            this.Socket = socket;
            this.Validated = false;
        }

        public void Dispose()
        {
            this.Socket.Close();
            this.Socket.Dispose();
        }
    }
}