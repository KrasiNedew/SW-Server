namespace Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using Data;

    using ModelDTOs;

    using Serialization;

    using Server.CommHandlers;
    using Server.CommHandlers.Interfaces;
    using Server.Constants;
    using Server.Services;

    using ServerUtils;
    using ServerUtils.Wrappers;

    public class AsynchronousSocketListener : IDisposable
    {
        // every 10 sec
        private const int ConnectionCheckInterval = 10000;

        private const int BattlesCheckInterval = 5000;

        private const int MaxUpdateInterval = 3000;

        // once a minute
        private const int DbPersistInterval = 60000;

        private const int MaxNumberOfConcurrentConnections = 2000;

        private const int BufferPoolSize = 10000;

        // ~1 MB
        private const int MaxBufferSize = 1048576;

        private const int Backlog = 50;

        // Thread signal.
        private readonly ManualResetEvent connectionHandle;    

        private readonly Socket listener;

        public readonly byte[] PingByte;

        public readonly UsersManager Users;

        public readonly BattlesManager Battles;

        public readonly ConcurrentDictionary<Client, Client> Clients;

        public readonly ConcurrentDictionary<UserLimited, UserLimited> UsersForShare;

        public readonly SimpleWarsContext Context;

        public readonly ConcurrentDictionary<UserFull, PlayerDTO> Players;

        public readonly ConcurrentDictionary<string, PlayerDTO> PlayersByUsername; 

        public readonly ConcurrentDictionary<string, Client> ClientsByUsername;

        public readonly ConcurrentDictionary<string, DateTime> BlockedIps;

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
            this.Clients = new ConcurrentDictionary<Client, Client>();
            this.ClientsByUsername = new ConcurrentDictionary<string, Client>();
            this.BlockedIps = new ConcurrentDictionary<string, DateTime>();
            this.Context = new SimpleWarsContext();
            this.Players = new ConcurrentDictionary<UserFull, PlayerDTO>();
            this.PlayersByUsername = new ConcurrentDictionary<string, PlayerDTO>();
            this.UsersForShare = new ConcurrentDictionary<UserLimited, UserLimited>();

            this.Auth = new AuthenticationServices(this);
            this.Game = new GameServices(this);
            this.Reader = new DefaultReader(this);
            this.Writer = new DefaultWriter(this);
            this.Responses = new PredefinedResponses(this);
            this.Parser = new DefaultParser(this);
            this.Buffers = new Buffers(BufferPoolSize, MaxBufferSize);
            this.PingByte = SerManager.SerializeWithLengthPrefix(Messages.Ping);
        }

        public void StartListening(int port)
        {    
            IPEndPoint endPoint = new IPEndPoint(GetLocalIPAddress(), port);
            this.listener.Bind(endPoint);

            this.listener.Listen(Backlog);

            Console.WriteLine("Server listening on " + endPoint.Address);

            Task.Run(() => { this.Heartbeat(); });
            Task.Run(() => { this.Persist(); });

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
            try
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

                this.Clients.TryAdd(client, client);

                this.Reader.ReadMessagesContinuously(client);
            }
            catch
            {
            }
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

                    DateTime timeRemoved;
                    foreach (var ip in unblocked)
                    {
                        this.BlockedIps.TryRemove(ip, out timeRemoved);
                    }

                    var badClients = this.Clients.Keys.Where(client => !client.IsConnected(this.PingByte) || client.Disposed || client.ErrorsAccumulated > 10);

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
                            Client removed;
                            this.Clients.TryRemove(badClient, out removed);

                            if (this.Users.IsValidCleanUser(badClient.User)
                                && this.ClientsByUsername
                                .ContainsKey(badClient.User.Username))
                            {
                                this.ClientsByUsername
                                    .TryRemove(badClient.User.Username, out removed);
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private void CleanBattles()
        {
            while (true)
            {
                Thread.Sleep(BattlesCheckInterval);

                if (!this.Battles.Any())
                {
                    continue;
                }

                try
                {
                    var now = DateTime.Now;
                    foreach (var battle in this.Battles.GetAll())
                    {
                        if (now.Ticks - battle.LastUpdate.Ticks > MaxUpdateInterval)
                        {
                            this.Game.EndBattle(battle);
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
                    this.BlockedIps.TryAdd(ip, DateTime.Now);
                }

                this.Responses.Blocked(client, this.BlockedIps[ip]);
            }
            finally
            {
                client.Dispose();
                Client removed;
                this.Clients.TryRemove(client, out removed);

                if (this.ClientsByUsername.ContainsKey(client.User.Username))
                {
                    this.ClientsByUsername.TryRemove(client.User.Username, out removed);
                }
            }
        }

        private void Persist()
        {
            while (true)
            {
                Thread.Sleep(DbPersistInterval);

                try
                {
                    this.Context.BulkSaveChanges();
                }
                catch
                {
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
                this.Auth.LogoutAllUsers();
            }
            finally
            {
                foreach (var client in this.Clients.Keys)
                {
                    client.Dispose();
                    this.listener.Close();
                    this.listener.Dispose();
                }
            }            
        }
    }
}