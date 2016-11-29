namespace Server.Wrappers
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.Sockets;

    using ServerUtils;

    public class Client : IDisposable
    {
        private static readonly byte[] PingByte = new byte[] { 1 };

        public Socket Socket { get; }

        public AuthDataSecure AuthData { get; set; }

        public bool Validated { get; set; }

        public bool Disposed { get; set; }

        public bool IsConnected()
        {
            try
            {
                if (this.Disposed) return false;

                int sent = this.Socket.Send(PingByte);
                return sent > 0;
            }
            catch
            {
                return false;
            }
        }

        public Client(Socket socket)
        {
            this.Socket = socket;
            this.Validated = false;
        }

        public void Dispose()
        {
            if (this.Disposed) return;

            this.Socket.Close();
            this.Socket.Dispose();
            this.Disposed = true;
        }
    }
}