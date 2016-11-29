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

            Tuple<byte[], int> data = null;
            try
            {
                if (!client.Validated && !(message is Message<string>))
                {
                    throw new InvalidOperationException("Cannot send data to non validated clients");
                }

                data = SerManager.SerializeToManagedBufferPrefixed(message);

                if (client.Disposed)
                {
                    Buffers.Return(data.Item1);
                    return;
                }

                client.Socket.BeginSend(data.Item1, 0, data.Item2, SocketFlags.None, SendToCallback, Tuple.Create(client, data.Item1));
            }
            catch (Exception e)
            {
                Buffers.Return(data?.Item1);
                Console.WriteLine(e.ToString());
            }
        }

        public static void SendToThenDropConnection(Client client, Message message)
        {
            if (client.Disposed) return;

            Tuple<byte[], int> data = null;
            try
            {
                data = SerManager.SerializeToManagedBufferPrefixed(message);
                if (client.Disposed)
                {
                    Buffers.Return(data.Item1);
                    return;
                }

                client.Socket.Send(data.Item1, 0, data.Item2, SocketFlags.None);

                AuthenticationServices.TryLogout(client);
                client.Dispose();
                Buffers.Return(data.Item1);
            }
            catch (Exception e)
            {
                Buffers.Return(data?.Item1);
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
            Tuple<Client, byte[]> state = (Tuple<Client, byte[]>)result.AsyncState;
            
            try
            {
                if (state.Item1.Disposed)
                {
                    Buffers.Return(state.Item2);
                    return;
                }

                int bytesSent = state.Item1.Socket.EndSend(result);
                Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, state.Item1.AuthData?.Username);
                Buffers.Return(state.Item2);                
            }
            catch (Exception e)
            {
                Buffers.Return(state.Item2);
                Console.WriteLine(e.ToString());
            }
        }
    }
}