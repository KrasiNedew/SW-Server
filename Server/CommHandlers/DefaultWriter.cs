namespace Server.CommHandlers
{
    using System;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.CommHandlers.Interfaces;

    using ServerUtils.Wrappers;

    public class DefaultWriter : Writer
    {
        private readonly AsynchronousSocketListener server;

        public DefaultWriter(AsynchronousSocketListener server)
        {
            this.server = server;
        }

        public void SendTo(Client client, Message message)
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

                data = SerManager.SerializeToManagedBufferPrefixed(message, this.server.Buffers);

                if (client.Disposed)
                {
                    this.server.Buffers.Return(data.Item1);
                    return;
                }

                client.Socket.BeginSend(data.Item1, 0, data.Item2, SocketFlags.None, this.SendToCallback, Tuple.Create(client, data.Item1));
            }
            catch (Exception e)
            {
                client.ErrorsAccumulated++;
                server.Buffers.Return(data?.Item1);
                Console.WriteLine(e.ToString());
            }
        }

        public void SendToThenDropConnection(Client client, Message message)
        {
            if (client.Disposed) return;

            Tuple<byte[], int> data = null;
            try
            {
                data = SerManager.SerializeToManagedBufferPrefixed(message, this.server.Buffers);

                if (client.Disposed)
                {
                    this.server.Buffers.Return(data.Item1);
                    return;
                }

                client.Socket.Send(data.Item1, 0, data.Item2, SocketFlags.None);

                this.server.Auth.TryLogout(client);
                client.Dispose();
                this.server.Buffers.Return(data.Item1);
            }
            catch (Exception e)
            {
                this.server.Buffers.Return(data?.Item1);
                this.server.Auth.TryLogout(client);
                client.Dispose();
                Console.WriteLine(e.ToString());
            }
        }

        // I don't really need broadcasting 
        // to all clients but its functionality I believe every server should have.
        public void BroadcastToAll(Message message)
        {
            Task.Run(() =>
            {                                    
                try
                {
                    var clients = this.server.Clients.Where(c => !c.Disposed
                    && c.IsConnected() && c.ErrorsAccumulated <= 10).ToArray();

                    foreach (var client in clients)
                    {
                        try
                        {
                            if(client.Disposed) continue;
                            this.server.Writer.SendTo(client, message);
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

        public void SendFromTo(Client sender, Message message, params Client[] receivers)
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
                                    this.server.Writer.SendTo(client, senderUsername);
                                    this.server.Writer.SendTo(client, message);
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

        private void SendToCallback(IAsyncResult result)
        {
            Tuple<Client, byte[]> state = (Tuple<Client, byte[]>)result.AsyncState;
            
            try
            {
                if (state.Item1.Disposed)
                {
                    this.server.Buffers.Return(state.Item2);
                    return;
                }

                int bytesSent = state.Item1.Socket.EndSend(result);
                Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, state.Item1.User?.Username);
                this.server.Buffers.Return(state.Item2);                
            }
            catch (Exception e)
            {
                state.Item1.ErrorsAccumulated++;
                this.server.Buffers.Return(state.Item2);
                Console.WriteLine(e.ToString());
            }
        }
    }
}