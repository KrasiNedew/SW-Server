namespace Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.Services;
    using Server.Wrappers;

    public static class Writer
    {
        public static void SendTo(Client client, Message message)
        {
            if (client.Disposed) return;

            try
            {
                if (!client.Validated && !(message is Message<string>))
                {
                    throw new InvalidOperationException("Cannot send data to non validated clients");
                }

                byte[] dataBytes = SerManager.SerializeWithLengthPrefix(message);
                Message check = SerManager.DeserializeWithLengthPrefix<Message>(dataBytes);
                Console.WriteLine(check.Service);

                if (client.Disposed) return;
                client.Socket.BeginSend(dataBytes, 0, dataBytes.Length, SocketFlags.None, SendToCallback, client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SendToThenDropConnection(Client client, Message message)
        {
            if (client.Disposed) return;

            try
            {
                byte[] dataBytes = SerManager.SerializeWithLengthPrefix(message);

                client.Socket.Send(dataBytes);

                AuthenticationServices.TryLogout(client);
                client.Dispose();
            }
            catch (Exception e)
            {
                AuthenticationServices.TryLogout(client);
                client.Dispose();
                Console.WriteLine(e.ToString());
            }
        }

        public static void BroadcastToAll(Message message, ISet<Client> clients)
        {
            Task.Run(() =>
            {
                try
                {
                    foreach (var client in clients)
                    {
                        try
                        {
                            if(client.Disposed) continue;
                            SendTo(client, message);
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

        public static void Send(Client sender, Message message, params Client[] receivers)
        {
            if (sender.Disposed) return;

            Task.Run(() =>
            {
                Message<string> senderUsername = new Message<string>(Service.SenderUsername, sender.AuthData.Username);

                try
                {
                    switch (message.Service)
                    {
                        case Service.PlayerData:
                            var playerDto = (message as Message<PlayerDTO>)?.Data;
                            if (playerDto == null)
                            {
                                return;
                            }

                            playerDto.PasswordHash = "";

                            foreach (var client in receivers)
                            {
                                try
                                {
                                    if(client.Disposed) continue;
                                    SendTo(client, senderUsername);
                                    SendTo(client, message);
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

        private static void SendToCallback(IAsyncResult result)
        {
            try
            {
                Client client = (Client)result.AsyncState;
                if (client.Disposed) return;

                int bytesSent = client.Socket.EndSend(result);

                Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, client.AuthData?.Username);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}