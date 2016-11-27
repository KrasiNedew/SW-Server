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

        private const int MaxNumberOfClients = 3000;

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

            Task.Run(() => { this.CheckForDisconnectedClients(); });

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
            if (this.clients.Count == MaxNumberOfClients)
            {
                this.connectionHandle.Set();
                return;
            }

            Socket socket = this.listener.EndAccept(result);
            this.connectionHandle.Set();

            if (socket == null)
            {
                throw new Exception("Socket was null wtf?");
            }

            Client client = new Client(socket);

            this.clients.Add(client);

            this.BeginReceiveClean(client);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            Tuple<Client, PacketAssembler> state =
                result.AsyncState as Tuple<Client, PacketAssembler>;

            Client client = state?.Item1;
            PacketAssembler packetAssembler = state?.Item2;

            try
            {
                int bytesReceived = client.Socket.EndReceive(result);
                bool pushed = false;

                if (packetAssembler.BytesToRead == 0)
                {
                    packetAssembler.PushReceivedData(bytesReceived);
                    pushed = true;

                    int streamDataLength = SerManager.GetLengthPrefix(packetAssembler.Data);

                    if (streamDataLength > 0)
                    {
                        packetAssembler.BytesToRead = streamDataLength;
                        packetAssembler.AllocateSpace(streamDataLength);
                    }
                } 

                if (bytesReceived > 0 && packetAssembler.BytesToRead > 0)
                {
                    if (!pushed)
                    {
                        packetAssembler.PushReceivedData(bytesReceived);
                        pushed = true;
                    }


                    if (packetAssembler.BytesRead >= packetAssembler.BytesToRead)
                    {
                        // handle the client service
                        List<Message> messages =
                            SerManager.DeserializeWithLengthPrefix<List<Message>>(packetAssembler.Data);

                        packetAssembler.Dispose();

                        this.ParseReceived(client, messages[0]);

                        this.BeginReceiveClean(client);
                        return;
                    }
                }

                this.BeginReceive(state);
            }
            catch (Exception e)
            {
                this.TryLogout(client);

                if (client != null)
                {
                    this.SendToThenDropConnection(client, new Message<string>(Service.None, Messages.InternalErrorDrop));
                }

                Console.WriteLine(e.ToString());
            }         
        }

        private void BeginReceive(Tuple<Client, PacketAssembler> state)
        {
            state.Item1.Socket.BeginReceive(
                        state.Item2.DataBuffer,
                        0,
                        PacketAssembler.PacketSize,
                        SocketFlags.None,
                        this.ReceiveCallback,
                        state);
        }

        private void BeginReceiveClean(Client client)
        {
            PacketAssembler packetAssembler = new PacketAssembler();
 
            client.Socket.BeginReceive(
                packetAssembler.DataBuffer,
                0,
                PacketAssembler.PacketSize,
                SocketFlags.None,
                this.ReceiveCallback,
                Tuple.Create(client, packetAssembler));
        }

        // Performs async broadcast to all clients
        public void BroadcastToAll(Message message)
        {
            Task.Run(() =>
            {
                try
                {
                    foreach (var client in this.clients)
                    {
                        try
                        {
                            this.SendTo(client, message);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Clients collection was probably modified");
                    Console.WriteLine(e.ToString());
                }
            });
        }

        // Performs async broadcast to specified clients.
        public void Send(Client sender, Message message, params Client[] receivers)
        {
            Task.Run(() =>
            {
                Message<string> senderUsername = new Message<string>(Service.SenderUsername, sender.AuthData.Username);

                try
                {
                    switch (message.Service)
                    {
                        case Service.PlayerData:
                            PlayerDTO player = (message as Message<PlayerDTO>)?.Data;
                            player.PasswordHash = "";

                            foreach (var client in receivers)
                            {
                                try
                                {
                                    this.SendTo(client, senderUsername);
                                    this.SendTo(client, message);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.ToString());
                                }
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Outer send exception");
                    Console.WriteLine(e.ToString());
                }
            });          
        }

        public void SendTo(Client client, Message message)
        {
            try
            {
                if (!client.Validated && !(message is Message<string>))
                {
                    throw new InvalidOperationException("Cannot send data to non validated clients");
                }

                byte[] dataBytes = SerManager.SerializeWithLengthPrefix(message);
                Message check = SerManager.DeserializeWithLengthPrefix<Message>(dataBytes);
                Console.WriteLine(check.Service);
                client.Socket.BeginSend(dataBytes, 0, dataBytes.Length, SocketFlags.None, this.SendToCallback, client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendToThenDropConnection(Client client, Message message)
        {
            try
            {
                if (client.Socket.Connected)
                {
                    byte[] dataBytes = SerManager.SerializeWithLengthPrefix(message);

                    client.Socket.Send(dataBytes);
                }

                this.TryLogout(client);
                client.Dispose();
                this.clients.Remove(client);
                client = null;
            }
            catch (Exception e)
            {
                this.TryLogout(client);
                client?.Dispose();
                this.clients.Remove(client);
                client = null;
                Console.WriteLine(e.ToString());
            }          
        }

        private void SendToCallback(IAsyncResult result)
        {
            try
            {
                Client client = (Client)result.AsyncState;

                int bytesSent = client.Socket.EndSend(result);

                Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, client.AuthData?.Username);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void CheckForDisconnectedClients()
        {
            while (true)
            {
                Thread.Sleep(ConnectionCheckInterval);

                if (this.clients.Count == 0)
                {
                    continue;
                }

                try
                {
                    foreach (var client in this.clients)
                    {
                        client.Connected = client.IsConnected();
                    }
                }
                catch (Exception)
                {
                }

                try
                {
                    var disconnectedClients = this.clients.Where(client => !client.Connected);

                    foreach (var discClient in disconnectedClients)
                    {
                        this.TryLogout(discClient);
                        discClient.Dispose();
                        this.clients.Remove(discClient);
                    }

                }
                catch (Exception)
                {
                }
            }
        }

        private void ParseReceived(Client client, Message message)
        {
            int err;
            switch (message.Service)
            {
                case Service.None:
                    this.SendToThenDropConnection(client, new Message<string>(Service.Login, Messages.InternalErrorDrop));
                    return;
                case Service.Login:
                    AuthDataRaw authDataRaw = ((Message<AuthDataRaw>)message).Data;
                    AuthDataSecure authDataSecure = new AuthDataSecure(authDataRaw.Username, authDataRaw.Password);
                    authDataRaw.Password = string.Empty;

                    err = ServiceHandler.Login(client, authDataSecure);
                    switch (err)
                    {
                        case 0:
                            ServerManager.Instance.Listener.SendTo(client, new Message<string>          (Service.Login, Messages.LoginSuccess));
                            break;
                        case ErrorCodes.InvalidCredentialsError:
                            this.SendTo(client, new Message<string>
                                (Service.Login, Messages.InvalidCredentials));
                            break;
                        case ErrorCodes.AlreadyLoggedIn:
                            this.SendTo(
                                client, new Message<string>(Service.Login, Messages.PlayerAlreadyLoggedIn));
                            break;
                        default:
                            this.SendToThenDropConnection(
                                client, new Message<string>(Service.None, Messages.InternalErrorDrop));
                            return;
                    }

                    break;

                case Service.Logout:
                    err = ServiceHandler.Logout(client);
                    switch (err)
                    {
                        case 0:
                            this.SendToThenDropConnection(client, 
                                new Message<string>(Service.Logout, Messages.LogoutSuccess));
                            break;
                        case ErrorCodes.LogoutError:
                            this.SendTo(client, new Message<string>
                                (Service.Logout, Messages.DataNotSaved));
                            break;
                        default:
                            this.SendToThenDropConnection(
                                client, new Message<string>
                                (Service.Logout, Messages.InternalErrorDrop));
                            return;
                    }

                    break;

                case Service.Registration:
                    AuthDataRaw authDataInsecure = ((Message<AuthDataRaw>)message).Data;
                    AuthDataSecure authDataReg = new AuthDataSecure(authDataInsecure.Username, authDataInsecure.Password);
                    authDataInsecure.Password = string.Empty;

                    err = ServiceHandler.Register(client, authDataReg);
                    switch (err)
                    {
                        case 0:
                            this.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.RegisterSuccessful));
                            break;
                        case ErrorCodes.AlreadyLoggedIn:
                            this.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.AlreadyLoggedIn));
                            break;
                        case ErrorCodes.UsernameEmptyError:
                            this.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.EmptyUsername));
                            break;
                        case ErrorCodes.PasswordEmptyError:
                            this.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.EmptyPassword));
                            break;
                        case ErrorCodes.UsernameTakenError:
                            this.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.UsernameTaken));
                            break;
                        default:
                            this.SendToThenDropConnection(
                            client,
                            new Message<string>(Service.Registration, Messages.InternalErrorDrop));
                            return;
                    }

                    break;
            }
        }

        private void TryLogout(Client client)
        {
            if (client?.AuthData == null)
            {
                return;
            }

            AuthDataSecure authData = client.AuthData;

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                var player = context.Players.FirstOrDefault(
                    p => p.Username == authData.Username && p.PasswordHash == authData.PasswordHash);

                if (player == null)
                {
                    return;
                }

                player.LoggedIn = false;
                context.SaveChanges();
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