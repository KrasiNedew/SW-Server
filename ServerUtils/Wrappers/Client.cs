namespace ServerUtils.Wrappers
{
    using System;
    using System.Net.Sockets;

    using ModelDTOs;

    public class Client : IDisposable
    {
        public Socket Socket { get; }

        public UserFull User { get; set; }

        public bool Disposed { get; private set; }

        public int ErrorsAccumulated { get; set; }

        public bool IsConnected(byte[] ping)
        {
            try
            {
                if (this.Disposed) return false;

                int sent = this.Socket.Send(ping);
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