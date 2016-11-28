namespace Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using Data;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.Handlers;
    using Server.Wrappers;

    using ServerUtils;

    public class AsynchronousSocketListener : IDisposable
    {
        private const int ConnectionCheckInterval = 6000;

        private const int MaxNumberOfConcurrentConnections = 3000;

        private const int Backlog = 50;

        // Thread signal.
        private readonly ManualResetEvent connectionHandle;

        private readonly HashSet<Client> clients;

        private readonly Socket listener;

        public AsynchronousSocketListener()
        {
            this.connectionHandle = new ManualResetEvent(false);
            this.clients = new HashSet<Client>();
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

            if (this.clients.Count >= MaxNumberOfConcurrentConnections)
            {
                Writer.SendTo(client, new Message<string>(Service.Info, Messages.ConnectionLimitReached));
                return;
            }

            this.clients.Add(client);

            Reader.BeginReceiveContinuous(client);
        }

        private void Heartbeat()
        {
            while (true)
            {
                Thread.Sleep(ConnectionCheckInterval);
                int discClients = 0;
                if (this.clients.Count == 0)
                {
                    continue;
                }

                try
                {
                    foreach (var client in this.clients)
                    {
                        if (!client.Disposed)
                        {
                            client.Connected = client.IsConnected();
                        }
                    }
                }
                catch
                {
                }

                try
                {
                    var disconnectedClients = this.clients.Where(client => !client.Connected || client.Disposed);

                    foreach (var discClient in disconnectedClients)
                    {
                        try
                        {
                            ServiceHandler.TryLogout(discClient);
                            if (!discClient.Disposed)
                            {
                                discClient.Dispose();
                            }

                            this.clients.Remove(discClient);
                            discClients += 1;
                        }
                        catch
                        {
                            ServiceHandler.TryLogout(discClient);
                            discClients += 1;
                            this.clients.Remove(discClient);
                        }
                    }
                }
                catch
                {
                }

                if (discClients > 0)
                {
                    Console.WriteLine($"{discClients} clients disconnected");
                }
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
            foreach (var client in this.clients)
            {
                client.Dispose();
                this.listener.Close();
                this.listener.Dispose();
            }
        }
    }
}