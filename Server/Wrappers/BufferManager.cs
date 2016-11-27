namespace Server.Wrappers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public static class BufferManager
    {
        public const int NumberOfDataBuffers = 10000;

        public const int NumberOfTransferBuffers = 5000;

        private static BlockingCollection<byte[]> DataBufferClearance;

        private static BlockingCollection<byte[]> TransferBufferClearance;

        // public so I can keep track of them while debugging
        public static Stack<byte[]> DataBuffers;

        public static Stack<byte[]> TransferBuffers; 

        public static void Init()
        {
            DataBuffers = new Stack<byte[]>(NumberOfDataBuffers);
            TransferBuffers = new Stack<byte[]>(NumberOfTransferBuffers);
            DataBufferClearance = new BlockingCollection<byte[]>(NumberOfDataBuffers);
            TransferBufferClearance = new BlockingCollection<byte[]>(NumberOfTransferBuffers);

            for (int i = 0; i < NumberOfDataBuffers; i++)
            {
                DataBuffers.Push(new byte[PacketAssembler.PacketSize]);
            }

            for (int i = 0; i < NumberOfTransferBuffers; i++)
            {
                TransferBuffers.Push(new byte[PacketAssembler.PacketSize * 2]);
            }

            Task.Run(() => { CleanDataBuffers(); });
            Task.Run(() => { CleanTransferBuffers(); });
        }

        public static void TakeDataBufferBack(byte[] buffer)
        {
            DataBufferClearance.Add(buffer);
        }

        public static byte[] GiveDataBuffer()
        {
            return DataBuffers.Pop();
        }

        public static void TakeTransferBufferBack(byte[] buffer)
        {
            TransferBufferClearance.Add(buffer);
        }

        public static byte[] GiveTransferBuffer()
        {
            return TransferBuffers.Pop();
        }

        private static void CleanDataBuffers()
        {
            bool available;
            do
            {
                byte[] buffer;
                available = DataBufferClearance.TryTake(out buffer, Timeout.Infinite);
                if (available)
                {
                    CleanDataBuffer(buffer);
                }
            }
            while (available);
        }

        private static void CleanTransferBuffers()
        {
            bool available;
            do
            {
                byte[] buffer;
                available = TransferBufferClearance.TryTake(out buffer, Timeout.Infinite);
                if (available)
                {
                    CleanTransferBuffer(buffer);
                }
            }
            while (available);
        }

        private static void CleanDataBuffer(byte[] buffer)
        {
            Array.Clear(buffer, 0, PacketAssembler.PacketSize);
            DataBuffers.Push(buffer);
        }

        private static void CleanTransferBuffer(byte[] buffer)
        {
            Array.Clear(buffer, 0, PacketAssembler.PacketSize);
            TransferBuffers.Push(buffer);
        }
    }
}