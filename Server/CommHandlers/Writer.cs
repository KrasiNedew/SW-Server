namespace Server.CommHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.Services;

    using ServerUtils;
    using ServerUtils.Wrappers;

    public static class Writer
    {
        public static void SendTo(this AsynchronousSocketListener server, Client client, Message message)
        {
            if (client.Disposed) return;

            Tuple<byte[], int> data = null;
            try
            {
                if ((client.User == null || !client.User.LoggedIn || client.User.Id == 0) 
                    && !(message is Message<string>))
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
                client.ErrorsAccumulated++;
                Buffers.Return(data?.Item1);
                Console.WriteLine(e.ToString());
            }
        }

        public static void SendToThenDropConnection(this AsynchronousSocketListener server, Client client, Message message)
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

                server.Auth.TryLogout(client);
                client.Dispose();
                Buffers.Return(data.Item1);
            }
            catch (Exception e)
            {
                Buffers.Return(data?.Item1);
                server.Auth.TryLogout(client);
                client.Dispose();
                Console.WriteLine(e.ToString());
            }
        }

        // I don't really need broadcasting 
        // to all clients but its functionality I believe every server should have.
        public static void BroadcastToAll(this AsynchronousSocketListener server, Message message)
        {
            Task.Run(() =>
            {                                    
                try
                {
                    // materializing beforehand to ignore 
                    // other treads manipulating the collection 
                    //(thats a very bad way of handling concurrency but since I written this method just because a server is supposed to have broadcast  and not actually using it who cares)
                    var clients = server.Clients.Where(c => !c.Disposed
                    && c.IsConnected() && c.ErrorsAccumulated <= 10).ToArray();

                        foreach (var client in clients)
                    {
                        try
                        {
                            if(client.Disposed) continue;
                            server.SendTo(client, message);
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

        public static void SendFromTo(this AsynchronousSocketListener server, Client sender, Message message, params Client[] receivers)
        {
            if (sender.Disposed) return;

            Task.Run(() =>
            {
                Message<string> senderUsername = new Message<string>(Service.SenderUsername, sender.User.Username);

                try
                {
                    switch (message.Service)
                    {
                        case Service.PlayerData:
                            var playerDto = (message as Message<PlayerDTO>)?.Data;
                            if (playerDto == null) return;

                            playerDto.PasswordHash = "";

                            foreach (var client in receivers)
                            {
                                try
                                {
                                    if(client.Disposed) continue;
                                    server.SendTo(client, senderUsername);
                                    server.SendTo(client, message);
                                }
                                catch (Exception e)
                                {
                                    client.ErrorsAccumulated++;
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
                Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, state.Item1.User?.Username);
                Buffers.Return(state.Item2);                
            }
            catch (Exception e)
            {
                state.Item1.ErrorsAccumulated++;
                Buffers.Return(state.Item2);
                Console.WriteLine(e.ToString());
            }
        }
    }
}