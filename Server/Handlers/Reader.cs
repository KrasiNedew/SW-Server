namespace Server.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.Wrappers;

    public static class Reader
    {
        public static void BeginReceiveSingle(Client client)
        {
            PacketAssembler packetAssembler = new PacketAssembler();

            client.Socket.BeginReceive(
                packetAssembler.DataBuffer,
                0,
                PacketAssembler.PacketSize,
                SocketFlags.None,
                ReceiveSingleCallback,
                Tuple.Create(client, packetAssembler));
        }

        public static void BeginReceiveContinuous(Client client)
        {
            PacketAssembler packetAssembler = new PacketAssembler();

            client.Socket.BeginReceive(
                packetAssembler.DataBuffer,
                0,
                PacketAssembler.PacketSize,
                SocketFlags.None,
                ReceiveContinuousCallback,
                Tuple.Create(client, packetAssembler));
        }

        private static void ReceiveContinuousCallback(IAsyncResult result)
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

                        Parser.ParseReceived(client, messages[0]);

                        BeginReceiveContinuous(client);
                        return;
                    }
                }

                ContinueReceive(state);
            }
            catch (Exception e)
            {
                ServiceHandler.TryLogout(client);

                if (client != null)
                {
                    Writer.SendToThenDropConnection(client, new Message<string>(Service.None, Messages.InternalErrorDrop));
                }

                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveSingleCallback(IAsyncResult result)
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

                        Parser.ParseReceived(client, messages[0]);
                        return;
                    }
                }

                ContinueReceiveSingle(state);
            }
            catch (Exception e)
            {
                ServiceHandler.TryLogout(client);

                if (client != null)
                {
                    Writer.SendToThenDropConnection(client, new Message<string>(Service.None, Messages.InternalErrorDrop));
                }

                Console.WriteLine(e.ToString());
            }
        }

        private static void ContinueReceive(Tuple<Client, PacketAssembler> state)
        {
            state.Item1.Socket.BeginReceive(
                        state.Item2.DataBuffer,
                        0,
                        PacketAssembler.PacketSize,
                        SocketFlags.None,
                        ReceiveContinuousCallback,
                        state);
        }

        private static void ContinueReceiveSingle(Tuple<Client, PacketAssembler> state)
        {
            state.Item1.Socket.BeginReceive(
                        state.Item2.DataBuffer,
                        0,
                        PacketAssembler.PacketSize,
                        SocketFlags.None,
                        ReceiveSingleCallback,
                        state);
        }
    }
}