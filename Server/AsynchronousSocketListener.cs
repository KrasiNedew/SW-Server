namespace Server
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Data;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Newtonsoft.Json;

    using ProtoBuf;

    using Serialization;

    using Server.Handlers;
    using Server.Wrappers;

    using ServerUtils;

    public class AsynchronousSocketListener : IDisposable
    {
        // Thread signal.
        private readonly ManualResetEvent allDone;

        private HashSet<Client> clients { get; set; }

        private readonly Socket listener;

        public AsynchronousSocketListener()
        {
            this.allDone = new ManualResetEvent(false);
            this.clients = new HashSet<Client>();
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

        private void Broadcast(Message message)
        {
            foreach (var client in this.clients)
            {
                this.SendTo(client, message);
            }
        }

        public void SendTo(Client client, Message message)
        {
            try
            {
                if (!client.Validated)
                {
                    throw new InvalidOperationException("Cannot send data to non validated clients");
                }

                byte[] dataBytes = SerManager.SerializeWithLengthPrefix(message);
                Message check = SerManager.DeserializeWithLengthPrefix<Message>(dataBytes);

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

                client.Dispose();
                this.clients.Remove(client);
                client = null;
            }
            catch (Exception e)
            {
                client?.Dispose();
                this.clients.Remove(client);
                client = null;
                Console.WriteLine(e.ToString());
            }          
        }

        private void SendToCallback(IAsyncResult result)
        {
            Client client = (Client)result.AsyncState;

            int bytesSent = client.Socket.EndSend(result);

            Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, client.AuthData.Username);
        }

        private bool ParseReceived(Client client, Message message)
        {
            int err;
            switch (message.Service)
            {
                case Service.None:
                    this.SendToThenDropConnection(client, new Message<string>(Service.Login, Messages.InternalErrorDrop));
                    return false;
                case Service.Login:
                    AuthDataRaw authDataRaw = ((Message<AuthDataRaw>)message).Data;
                    AuthDataSecure authDataSecure = new AuthDataSecure(authDataRaw.Username, authDataRaw.Password);
                    authDataRaw.Password = string.Empty;

                    err = RequestHandler.Login(client, authDataSecure);

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
                                client, new Message<string>(Service.Login, Messages.SomethingWentWrong));
                            break;
                        default:
                            this.SendToThenDropConnection(
                                client, new Message<string>(Service.None, Messages.InternalErrorDrop));
                            return false;
                    }

                    break;

                case Service.Logout:
                    err = RequestHandler.Logout(client);
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
                            return false;
                    }

                    break;

                case Service.Registration:
                    AuthDataRaw authDataInsecure = ((Message<AuthDataRaw>)message).Data;
                    AuthDataSecure authDataReg = new AuthDataSecure(authDataInsecure.Username, authDataInsecure.Password);
                    authDataInsecure.Password = string.Empty;

                    err = RequestHandler.Register(client, authDataReg);

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
                            return false;
                    }

                    break;
            }

            return true;
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