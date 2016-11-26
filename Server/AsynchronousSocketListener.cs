namespace Server
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using ModelDTOs;

    using Newtonsoft.Json;

    using Serializer;

    using Server.Wrappers;

    public class AsynchronousSocketListener
    {
        // Thread signal.
        private readonly ManualResetEvent allDone;

        private HashSet<ConnectedClient> clients { get; set; }

        private Socket listener;

        public AsynchronousSocketListener()
        {
            this.allDone = new ManualResetEvent(false);
            this.clients = new HashSet<ConnectedClient>();
            this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void StartListening(int port)
        {
            IPEndPoint endPoint = new IPEndPoint(GetLocalIPAddress(), port);
            this.listener.Bind(endPoint);

            this.listener.Listen(0);

            Console.WriteLine("Server listening on " + endPoint.Address);

            while (true)
            {
                this.allDone.Reset();
                //this.clients.RemoveWhere(client => !client.Socket.Connected);

                try
                {
                    this.listener.BeginAccept(this.AcceptCallback, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                this.allDone.WaitOne();
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            Socket socket = this.listener.EndAccept(result);
            this.allDone.Set();

            if (socket == null)
            {
                throw new Exception("Socket was null wtf?");
            }

            ConnectedClient client = new ConnectedClient(socket);

            this.clients.Add(client);

            this.BeginReceive(client);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            ConnectedClient client = result.AsyncState as ConnectedClient;

            try
            {
                int bytesReceived = client.Socket.EndReceive(result);

                if (bytesReceived > 0)
                {
                    Packet received = new Packet(client.PacketAssembler.DataBuffer);
                    client.PacketAssembler.Packets.Add(received);

                    client.PacketAssembler.DataBuffer = new byte[Packet.Size];

                    if (received.GetStringFromRawData().EndsWith("<EOF>"))
                    {
                        string data = client.PacketAssembler.CurrentStringData;

                        // clean the packet assembler
                        client.PacketAssembler.DataBuffer = new byte[Packet.Size];
                        client.PacketAssembler.Packets = new List<Packet>();

                        // get auth info out of the data
                        client.AuthData = Serializer.ExtractUsernameAndPassword(data);

                        // check if client credentials are correct and 
                        // mark if tey are
                        // remeber to set to false after first response sent back
                        client.Validated = true;

                        // parse the data
                        Console.WriteLine(JsonConvert.DeserializeObject(data));
                    }
                }

                this.BeginReceive(client);
            }
            catch (Exception e)
            {
                this.clients.Remove(client);
                Console.WriteLine(e.ToString());
            }         
        }

        private void BeginReceive(ConnectedClient client)
        {
            client.Socket.BeginReceive(
                        client.PacketAssembler.DataBuffer,
                        0,
                        Packet.Size,
                        SocketFlags.None,
                        this.ReceiveCallback,
                        client);
        }

        private void Broadcast(string data)
        {
            foreach (var client in this.clients)
            {
                this.SendTo(client, data);
            }
        }

        private void SendTo(ConnectedClient client, string data)
        {
            try
            {
                //if (!client.Validated)
                //{
                //    throw new InvalidOperationException("Cannot send data to non validated clients");
                //}

                byte[] dataBytes = Encoding.ASCII.GetBytes(data);

                client.Socket.BeginSend(dataBytes, 0, dataBytes.Length, SocketFlags.None, this.SendToCallback, client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendToCallback(IAsyncResult result)
        {
            ConnectedClient client = (ConnectedClient)result;

            int bytesSent = client.Socket.EndSend(result);

            Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, client.AuthData.Username);
        }

        private static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }

            return IPAddress.Parse("127.0.0.1");
        }
    }
}