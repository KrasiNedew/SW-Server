namespace Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using Server.CommHandlers;
    using Server.CommHandlers.Interfaces;
    using Server.Services;

    using ServerUtils;
    using ServerUtils.Wrappers;

    public class AsynchronousSocketListener : IDisposable
    {
        private const int ConnectionCheckInterval = 10000;

        private const int MaxNumberOfConcurrentConnections = 2000;

        private const int BufferPoolSize = 10000;

        private const int MaxBufferSize = 1048576;

        private const int Backlog = 50;

        // Thread signal.
        private readonly ManualResetEvent connectionHandle;    

        private readonly Socket listener;

        public readonly UsersManager Users;

        public readonly BattlesManager Battles;

        public readonly HashSet<Client> Clients;

        public readonly Dictionary<string, Client> ClientsByUsername;

        public readonly Dictionary<string, DateTime> BlockedIps;

        public readonly AuthenticationServices Auth;

        public readonly GameServices Game;

        public readonly PredefinedResponses Responses;

        public readonly Reader Reader;

        public readonly Writer Writer;

        public readonly Parser Parser;

        public readonly Buffers Buffers;

        public AsynchronousSocketListener()
        {
            this.connectionHandle = new ManualResetEvent(false);
            this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.Users = new UsersManager();
            this.Battles = new BattlesManager();
            this.Clients = new HashSet<Client>();
            this.ClientsByUsername = new Dictionary<string, Client>();
            this.BlockedIps = new Dictionary<string, DateTime>();

            this.Auth = new AuthenticationServices(this);
            this.Game = new GameServices(this);
            this.Reader = new DefaultReader(this);
            this.Writer = new DefaultWriter(this);
            this.Responses = new PredefinedResponses(this);
            this.Parser = new DefaultParser(this);
            this.Buffers = new Buffers(BufferPoolSize, MaxBufferSize);
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
            if (this.BlockedIps.ContainsKey(ip.Address.ToString()))
            {
                this.Responses.Blocked(client, this.BlockedIps[ip.Address.ToString()]);
                return;
            }


            if (this.BlockedIps.Count >= MaxNumberOfConcurrentConnections)
            {
                this.Responses.ServerFull(client);
                return;
            }

            this.Clients.Add(client);

            this.Reader.ReadMessagesContinuously(client);
        }

        private void Heartbeat()
        {
            while (true)
            {
                Thread.Sleep(ConnectionCheckInterval);
                Console.WriteLine($"Connected clients: {this.Clients.Count}");

                if (this.Clients.Count == 0)
                {
                    continue;
                }             

                try
                {
                    ICollection<string> unblocked = 
                        (from ip in this.BlockedIps.Keys
                        let diff = 
                        new TimeSpan(DateTime.Now.Ticks - this.BlockedIps[ip].Ticks)
                        where diff.Minutes > 10 select ip).ToList();

                    foreach (var ip in unblocked)
                    {
                        this.BlockedIps.Remove(ip);
                    }

                    var badClients = this.Clients.Where(client => !client.IsConnected() || client.Disposed || client.ErrorsAccumulated > 10);

                    foreach (var badClient in badClients)
                    {
                        this.Auth.TryLogout(badClient);
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
                            this.Clients.Remove(badClient);

                            if (this.Users.IsValidCleanUser(badClient.User)
                                && this.ClientsByUsername
                                .ContainsKey(badClient.User.Username))
                            {
                                this.ClientsByUsername.Remove(badClient.User.Username);
                            }
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
                if (!this.BlockedIps.ContainsKey(ip))
                {
                    this.BlockedIps.Add(ip, DateTime.Now);
                }

                this.Responses.Blocked(client, this.BlockedIps[ip]);
            }
            finally
            {
                client.Dispose();
                this.Clients.Remove(client);

                if (this.ClientsByUsername.ContainsKey(client.User.Username))
                {
                    this.ClientsByUsername.Remove(client.User.Username);
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
            try
            {
                AuthenticationServices.LogoutAllUsers();
            }
            finally
            {
                foreach (var client in this.Clients)
                {
                    client.Dispose();
                    this.listener.Close();
                    this.listener.Dispose();
                }
            }            
        }
    }
}