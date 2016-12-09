namespace Server.CommHandlers
{
    using System;
    using System.Net.Sockets;

    using ModelDTOs;
    using Serialization;

    using Server.CommHandlers.Interfaces;

    using ServerUtils;
    using ServerUtils.Wrappers;

    public class DefaultReader : Reader
    {
        private readonly AsynchronousSocketListener server;

        public DefaultReader(AsynchronousSocketListener server)
        {
            this.server = server;
        }

        public void ReadSingleMessage(Client client)
        {
            if (client.Disposed) return;

            this.ReadLengthPrefix(client, false);
        }

        public void ReadMessagesContinuously(Client client)
        {
            if (client.Disposed) return;

            this.ReadLengthPrefix(client, true);
        }

        private void ReadMessage(Client client, int messageLength, bool continuous)
        {
            if (client.Disposed) return;

            MessageReader packetAssembler = new MessageReader(messageLength, this.server.Buffers);

            try
            {
                client.Socket.BeginReceive(
                    packetAssembler.DataBuffer,
                    0,
                    messageLength,
                    SocketFlags.None, this.MessageReceivedCallback,
                    Tuple.Create(client, packetAssembler, continuous));
            }
            catch
            {
                client.ErrorsAccumulated++;
                client.Dispose();
                packetAssembler.Dispose();
            }
        }

        private void MessageReceivedCallback(IAsyncResult result)
        {
            Tuple<Client, MessageReader, bool> state =
                result.AsyncState as Tuple<Client, MessageReader, bool>;
            if (state == null || state.Item1.Disposed)
            {
                state?.Item2.Dispose();
                return;
            }

            Client client = state.Item1;
            MessageReader packetAssembler = state.Item2;
            bool listenForNextMessage = state.Item3;

            try
            {
                int bytesReceived = client.Socket.EndReceive(result);

                if (bytesReceived > 0)
                {
                    Message message =
                        SerManager.Deserialize<Message>(packetAssembler.DataBuffer);

                    packetAssembler.Dispose();

                    // handle the data
                    this.server.Parser.ParseReceived(client, message);

                    if (state.Item3)
                    {
                        this.ReadLengthPrefix(client, listenForNextMessage);
                    }
                }
            }
            catch (Exception e)
            {
                client.ErrorsAccumulated++;
                packetAssembler.Dispose();
                this.server.Responses.SomethingWentWrong(client);
                client.Dispose();

                Console.WriteLine(e.ToString());
            }
        }

        private void ReadLengthPrefix(Client client, bool continuous)
        {
            if (client.Disposed) return;

            PrefixReader prefixReader = new PrefixReader(this.server.Buffers);

            try
            {
                client.Socket.BeginReceive(
                    prefixReader.Buffer,
                    0,
                    PrefixReader.PrefixBytes,
                    SocketFlags.None, this.LengthPrefixReceivedCallback,
                    Tuple.Create(client, prefixReader, continuous));
            }
            catch
            {
                client.ErrorsAccumulated++;
                prefixReader.Dispose();
                client.Dispose();
            }
        }

        private void LengthPrefixReceivedCallback(IAsyncResult result)
        {
            Tuple<Client, PrefixReader, bool> state = (Tuple<Client, PrefixReader, bool>)result.AsyncState;
            if (state.Item1.Disposed)
            {
                state.Item2.Dispose();
                return;
            }

            try
            {
                int bytesRead = state.Item1.Socket.EndReceive(result);
                state.Item2.PushReceivedData(bytesRead);

                if (state.Item2.BytesToRead == 0)
                {
                    int messageLength = SerManager.GetLengthPrefix(state.Item2.PrefixData) - PrefixReader.PrefixBytes;
                    state.Item2.Dispose();
                    this.ReadMessage(state.Item1, messageLength, state.Item3);
                }
                else
                {
                    this.ContinueReadingLengthPrefix(state);
                }
            }
            catch
            {
                state.Item1.ErrorsAccumulated++;
                state.Item1.Dispose();
                state.Item2.Dispose();
                Console.WriteLine("Error while reading length prefix");
            }
        }

        private void ContinueReadingLengthPrefix(Tuple<Client, PrefixReader, bool> state)
        {
            if (state.Item1.Disposed)
            {
                state.Item2.Dispose();
                return;
            }

            try
            {
                state.Item1.Socket.BeginReceive(
                    state.Item2.Buffer,
                    0,
                    state.Item2.BytesToRead,
                    SocketFlags.None, this.LengthPrefixReceivedCallback,
                    state);
            }
            catch
            {
                state.Item1.ErrorsAccumulated++;
                state.Item1.Dispose();
                state.Item2.Dispose();
            }
        }
    }
}