namespace Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.Constants;
    using Server.Handlers;
    using Server.Services;
    using Server.Wrappers;

    public class AsynchronousSocketListener : IDisposable
    {
        private const int ConnectionCheckInterval = 10000;

        private const int MaxNumberOfConcurrentConnections = 3000;

        private const int Backlog = 50;

        // Thread signal.
        private readonly ManualResetEvent connectionHandle;

        private readonly HashSet<Client> clients;

        private readonly Dictionary<string, DateTime> blocked;

        private readonly Socket listener;

        public AsynchronousSocketListener()
        {
            this.connectionHandle = new ManualResetEvent(false);
            this.clients = new HashSet<Client>();
            this.blocked = new Dictionary<string, DateTime>();
            this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void StartListening(int port)
        {    
            IPEndPoint endPoint = new IPEndPoint(GetLocalIPAddress(), port);
            this.listener.Bind(endPoint);

            this.listener.Listen(Backlog);

            Console.WriteLine("Server listening on " + endPoint.Address);

            Task.Run(() => { this.Heartbeat(); });

            while (true)
            {
                this.connectionHandle.Reset();

                try
                {
                    this.listener.BeginAccept(this.AcceptCallback, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    this.connectionHandle.Set();
                }

                this.connectionHandle.WaitOne();
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            Socket socket = this.listener.EndAccept(result);

            this.connectionHandle.Set();

            if (socket == null)
            {
                throw new Exception("Socket was null wtf?");
            }

            Client client = new Client(socket);

            IPEndPoint ip = (IPEndPoint)client.Socket.RemoteEndPoint;
            if (this.blocked.ContainsKey(ip.Address.ToString()))
            {
                this.SendBlockedMessage(client, this.blocked[ip.Address.ToString()]);
                return;
            }


            if (this.clients.Count >= MaxNumberOfConcurrentConnections)
            {
                Writer.SendTo(client, new Message<string>(Service.Info, Messages.ConnectionLimitReached));
                return;
            }

            this.clients.Add(client);

            Reader.ReadMessagesContinously(client);
        }

        private void Heartbeat()
        {
            while (true)
            {
                Thread.Sleep(ConnectionCheckInterval);
                Console.WriteLine($"Connected clients: {this.clients.Count}");
                Console.WriteLine($"{Buffers.PrefixBuffers.Count} pb {Buffers.TinyBuffers.Count} tb {Buffers.SmallBuffers.Count} sb {Buffers.MediumBuffers.Count} mb {Buffers.LargeBuffers.Count} lb");

                if (this.clients.Count == 0)
                {
                    continue;
                }             

                try
                {
                    ICollection<string> unblocked = 
                        (from ip in this.blocked.Keys
                        let diff = 
                        new TimeSpan(DateTime.Now.Ticks - this.blocked[ip].Ticks)
                        where diff.Minutes > 10 select ip).ToList();

                    foreach (var ip in unblocked)
                    {
                        this.blocked.Remove(ip);
                    }

                    var badClients = this.clients.Where(client => !client.IsConnected() || client.Disposed || client.ErrorsAccumulated > 10);

                    foreach (var badClient in badClients)
                    {
                        AuthenticationServices.TryLogout(badClient);
                        try
                        {
                            if (badClient.ErrorsAccumulated > 10)
                            {
                                this.BlockClient(badClient);
                            }

                            badClient.Dispose();
                        }
                        finally
                        {
                            this.clients.Remove(badClient);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public void BlockClient(Client client)
        {
            try
            {
                string ip = ((IPEndPoint)client.Socket.RemoteEndPoint).Address.ToString();
                if (!this.blocked.ContainsKey(ip))
                {
                    this.blocked.Add(ip, DateTime.Now);
                }

                this.SendBlockedMessage(client, DateTime.Now);
            }
            finally
            {
                client.Dispose();
                this.clients.Remove(client);
            }
        }

        private void SendBlockedMessage(Client client, DateTime timeOfBlock)
        {
            TimeSpan diff = new TimeSpan(DateTime.Now.Ticks - timeOfBlock.Ticks);
            Message<string> message = 
                new Message<string>(Service.Info, $"You are blocked. Try again in {diff.Minutes} min : {diff.Seconds} sec");

            Tuple<byte[], int> data = SerManager.SerializeToManagedBufferPrefixed(message);
            try
            {
                client.Socket.Send(data.Item1, 0, data.Item2, SocketFlags.None);
            }
            finally
            {
                Buffers.Return(data.Item1);
                client.Dispose();
            }
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


        public void Dispose()
        {
            try
            {
                AuthenticationServices.LogoutAllUsers();
            }
            finally
            {
                foreach (var client in this.clients)
                {
                    client.Dispose();
                    this.listener.Close();
                    this.listener.Dispose();
                }
            }            
        }
    }
}