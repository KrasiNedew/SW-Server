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

    using Serialization;

    using Server.Handlers;
    using Server.Wrappers;

    using ServerUtils;

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

                    client.PacketAssembler.CleanDataBuffer();

                    if (received.GetStringFromRawData().EndsWith("<EOF>"))
                    {
                        string data = client.PacketAssembler.ReceivedStringData;

                        // clean the packet assembler
                        client.PacketAssembler.CleanDataBuffer();
                        client.PacketAssembler.CleanPackets();

                        ServiceRequest request = Deserializer.ExtractServiceRequest(data);
                        this.ParseRequest(client, request, data);

                        // log the data
                        Console.WriteLine(JsonConvert.DeserializeObject(data));
                    }
                }

                this.BeginReceive(client);
            }
            catch (Exception e)
            {
                if (client != null)
                {
                    this.SendToThenDropConnection(client, Messages.InternalErrorDrop);
                }

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
                if (!client.Validated)
                {
                    throw new InvalidOperationException("Cannot send data to non validated clients");
                }

                byte[] dataBytes = Encoding.ASCII.GetBytes(data);

                client.Socket.BeginSend(dataBytes, 0, dataBytes.Length, SocketFlags.None, this.SendToCallback, client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendToThenDropConnection(ConnectedClient client, string data)
        {
            byte[] dataBytes = Encoding.ASCII.GetBytes(data);

            client.Socket.Send(dataBytes);

            client.Close();
            this.clients.Remove(client);
            client = null;
        }

        private void SendToCallback(IAsyncResult result)
        {
            ConnectedClient client = (ConnectedClient)result;

            int bytesSent = client.Socket.EndSend(result);

            Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, client.AuthData.Username);
        }

        private bool ParseRequest(ConnectedClient client, ServiceRequest request, string data)
        {
            int err;
            switch (request)
            {
                case ServiceRequest.Unknown:
                    this.SendToThenDropConnection(client, "Invalid request");
                    return false;
                case ServiceRequest.Login:
                    err = RequestHandler.Login(client, data);

                    switch (err)
                    {
                        case ErrorCodes.InvalidCredentialsError:
                            this.SendTo(client, Messages.InvalidCredentials);
                            break;
                        case ErrorCodes.InternalError:
                            this.SendTo(
                                client, Messages.SomethingWentWrong);
                            break;
                        default:
                            this.SendToThenDropConnection(
                                client, Messages.InternalErrorDrop);
                            return false;
                    }

                    break;

                case ServiceRequest.Logout:
                    // change state to logged out
                    err = RequestHandler.Logout(client);
                    switch (err)
                    {
                        case 0:
                            this.SendToThenDropConnection(client, Messages.LogoutSuccess);
                            break;
                        case ErrorCodes.LogoutError:
                            this.SendTo(client, Messages.DataNotSaved);
                            break;
                        default:
                            this.SendToThenDropConnection(
                                client, Messages.InternalErrorDrop);
                            return false;
                    }

                    break;

                case ServiceRequest.Registration:
                    err = RequestHandler.Register(client, data);

                    switch (err)
                    {
                        case 0:
                            this.SendTo(
                            client,
                            Messages.RegisterSuccessful);
                            break;
                        case ErrorCodes.AlreadyLoggedIn:
                            this.SendTo(
                            client,
                            Messages.AlreadyLoggedIn);
                            break;
                        case ErrorCodes.UsernameEmptyError:
                            this.SendTo(
                            client,
                            Messages.EmptyUsername);
                            break;
                        case ErrorCodes.PasswordEmptyError:
                            this.SendTo(
                            client,
                            Messages.EmptyPassword);
                            break;
                        case ErrorCodes.UsernameTakenError:
                            this.SendTo(
                            client,
                            Messages.UsernameTaken);
                            break;
                        default:
                            this.SendToThenDropConnection(
                            client,
                            Messages.InternalErrorDrop);
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
    }
}