namespace Server.Wrappers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public static class BufferManager
    {
        public const int NumberOfBuffers = 10000;

        private static BlockingCollection<byte[]> Clearance;

        public static Stack<byte[]> Buffers;

        public static void Init()
        {
            Buffers = new Stack<byte[]>(NumberOfBuffers);
            Clearance = new BlockingCollection<byte[]>(NumberOfBuffers);

            for (int i = 0; i < NumberOfBuffers; i++)
            {
                Buffers.Push(new byte[PacketAssembler.PacketSize]);
            }

            Task.Run(() => { CleanPending(); });
        }

        public static void TakeBack(byte[] buffer)
        {
            Clearance.Add(buffer);
        }

        public static byte[] Give()
        {
            return Buffers.Pop();
        }

        private static void CleanPending()
        {
            bool available;
            do
            {
                byte[] buffer;
                available = Clearance.TryTake(out buffer, Timeout.Infinite);
                if (available)
                {
                    CleanBuffer(buffer);
                }
            }
            while (available);
        }

        private static void CleanBuffer(byte[] buffer)
        {
            Array.Clear(buffer, 0, PacketAssembler.PacketSize);
            Buffers.Push(buffer);
        }
    }
}