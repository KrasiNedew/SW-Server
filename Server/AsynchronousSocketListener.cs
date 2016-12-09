namespace Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
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

        public readonly SimpleWarsContext Context;

        public readonly ConcurrentDictionary<Guid, PlayerDTO> Players;

        public readonly ConcurrentDictionary<Guid, Client> Clients;

        public readonly ConcurrentDictionary<Guid, BattleInfo> Battles; 

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

            this.Clients = new ConcurrentDictionary<Guid, Client>();
            this.Players = new ConcurrentDictionary<Guid, PlayerDTO>();
            this.Battles = new ConcurrentDictionary<Guid, BattleInfo>();
            this.BlockedIps = new ConcurrentDictionary<string, DateTime>();
            this.Context = new SimpleWarsContext();

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


                if (this.Clients.Count >= MaxNumberOfConcurrentConnections)
                {
                    this.Responses.ServerFull(client);
                    return;
                }

                Console.WriteLine($"Client {client.Id} connected");
                this.Clients.TryAdd(client.Id, client);

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

                    var badClients = this.Clients.Values.Where(client => !client.IsConnected(this.PingByte) || client.Disposed || client.ErrorsAccumulated > 10);

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
                            PlayerDTO playerFreed;
                            this.Clients.TryRemove(badClient.Id, out removed);
                            this.Players.TryRemove(badClient.Id, out playerFreed);

                            if (removed.BattleId != Guid.Empty 
                                && this.Battles.ContainsKey(removed.BattleId))
                            {
                                this.Game.EndBattle(removed);
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
                    foreach (var battle in this.Battles.Values)
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
                PlayerDTO playerFreed;
                this.Clients.TryRemove(client.Id, out removed);
                this.Players.TryRemove(client.Id, out playerFreed);
            }
        }

        private void Persist()
        {
            while (true)
            {
                Thread.Sleep(DbPersistInterval);

                try
                {
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    this.Context.BulkSaveChanges();
                    timer.Stop();
                    Console.WriteLine("save time: " + timer.ElapsedMilliseconds);
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
                foreach (var client in this.Clients.Values)
                {
                    client.Dispose();
                    this.listener.Close();
                    this.listener.Dispose();
                }
            }            
        }
    }
}