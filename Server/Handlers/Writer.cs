﻿namespace Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.Wrappers;

    public static class Writer
    {
        public static void SendTo(Client client, Message message)
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
                client.Socket.BeginSend(dataBytes, 0, dataBytes.Length, SocketFlags.None, SendToCallback, client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SendToThenDropConnection(Client client, Message message)
        {
            try
            {
                if (client.Socket.Connected)
                {
                    byte[] dataBytes = SerManager.SerializeWithLengthPrefix(message);

                    client.Socket.Send(dataBytes);
                }

                ServiceHandler.TryLogout(client);
                client.Dispose();
            }
            catch (Exception e)
            {
                ServiceHandler.TryLogout(client);
                client?.Dispose();
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