namespace Server.CommHandlers
{
    using System;
    using System.Net.Sockets;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.Constants;
    using Server.Services;

    using ServerUtils;

    public static class Reader
    {
        public static void ReadSingleMessage(Client client)
        {
            if (client.Disposed) return;

            ReadLengthPrefix(client, false);
        }

        public static void ReadMessagesContinously(Client client)
        {
            if (client.Disposed) return;

            ReadLengthPrefix(client, true);
        }

        private static void ReadMessage(Client client, int messageLength, bool continuous)
        {
            if (client.Disposed) return;

            MessageReader packetAssembler = new MessageReader(messageLength);

            try
            {
                client.Socket.BeginReceive(
                    packetAssembler.DataBuffer,
                    0,
                    messageLength,
                    SocketFlags.None,
                    MessageReceivedCallback,
                    Tuple.Create(client, packetAssembler, continuous));
            }
            catch
            {
                client.ErrorsAccumulated++;
                packetAssembler.Dispose();
            }
        }

        private static void MessageReceivedCallback(IAsyncResult result)
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
                    Parser.ParseReceived(client, message);

                    if (state.Item3)
                    {
                        ReadLengthPrefix(client, listenForNextMessage);
                    }
                }
            }
            catch (Exception e)
            {
                client.ErrorsAccumulated++;
                packetAssembler.Dispose();
                AuthenticationServices.TryLogout(client);

                if (client != null)
                {
                    Writer.SendToThenDropConnection(client, new Message<string>(Service.None, MessageText.InternalErrorDrop));
                }

                Console.WriteLine(e.ToString());
            }
        }

        private static void ReadLengthPrefix(Client client, bool continuous)
        {
            if (client.Disposed) return;

            PrefixReader prefixReader = new PrefixReader();

            try
            {
                client.Socket.BeginReceive(
                    prefixReader.Buffer,
                    0,
                    PrefixReader.PrefixBytes,
                    SocketFlags.None,
                    LengthPrefixReceivedCallback,
                    Tuple.Create(client, prefixReader, continuous));
            }
            catch
            {
                prefixReader.Dispose();
            }
        }

        private static void LengthPrefixReceivedCallback(IAsyncResult result)
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
                    ReadMessage(state.Item1, messageLength, state.Item3);
                }
                else
                {
                    ContinueReadingLengthPrefix(state);
                }
            }
            catch
            {
                state.Item1.ErrorsAccumulated++;
                state.Item2.Dispose();
                Console.WriteLine("Error while reading length prefix");
            }
        }

        private static void ContinueReadingLengthPrefix(Tuple<Client, PrefixReader, bool> state)
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
                    SocketFlags.None,
                    LengthPrefixReceivedCallback,
                    state);
            }
            catch
            {
                state.Item1.ErrorsAccumulated++;
                state.Item2.Dispose();
            }
        }
    }
}