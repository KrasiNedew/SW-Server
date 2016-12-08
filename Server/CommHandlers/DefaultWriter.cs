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
                this.server.Buffers.Return(data?.Item1);
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

                state.Item1.Socket.EndSend(result);
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