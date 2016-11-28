namespace Server.Wrappers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
 
    public static class Buffers
    {
        private const int NumberOfTinyBuffers = 5000;

        private const int NumberOfSmallBuffers = 5000;

        private const int NumberOfMediumBuffers = 2000;

        private const int NumberOfLargeBuffers = 200;

        private const int NumberOfLengthBuffers = 5000;

        private const int TinyBufferSize = 256;

        private const int SmallBufferSize = 1024;

        private const int MediumBufferSize = 16384;

        private const int LargeBufferSize = 524288;

        private static Stack<byte[]> TinyBuffers;

        private static Stack<byte[]> SmallBuffers;

        private static Stack<byte[]> MediumBuffers; 

        private static Stack<byte[]> LargeBuffers;

        private static Stack<byte[]> LengthBuffers;

        private static BlockingCollection<byte[]> BuffersClearance; 

        public static void Init()
        {
            LengthBuffers = new Stack<byte[]>(NumberOfLengthBuffers);
            TinyBuffers = new Stack<byte[]>(NumberOfTinyBuffers);
            SmallBuffers = new Stack<byte[]>(NumberOfSmallBuffers);
            MediumBuffers = new Stack<byte[]>(NumberOfMediumBuffers);
            LargeBuffers = new Stack<byte[]>(NumberOfLargeBuffers);

            for (int i = 0; i < NumberOfLengthBuffers; i++)
            {
                LengthBuffers.Push(new byte[LengthReceiver.LengthPrefixBytes]);
            }

            for (int i = 0; i < NumberOfTinyBuffers; i++)
            {
                TinyBuffers.Push(new byte[TinyBufferSize]);
            }

            for (int i = 0; i < NumberOfSmallBuffers; i++)
            {
                SmallBuffers.Push(new byte[SmallBufferSize]);
            }

            for (int i = 0; i < NumberOfMediumBuffers; i++)
            {
                MediumBuffers.Push(new byte[MediumBufferSize]);
            }

            for (int i = 0; i < NumberOfLargeBuffers; i++)
            {
                LargeBuffers.Push(new byte[LargeBufferSize]);
            }

            BuffersClearance = new BlockingCollection<byte[]>();

            Task.Run(() => { BuffersCleaner(); });
        }

        public static byte[] TakeBuffer(int size)
        {
            if (size <= LengthReceiver.LengthPrefixBytes)
            {
                return LengthBuffers.Pop();
            }

            if (size <= TinyBufferSize)
            {
                return TinyBuffers.Pop();
            }

            if (size <= SmallBufferSize)
            {
                return SmallBuffers.Pop();
            }

            if (size <= MediumBufferSize)
            {
                return MediumBuffers.Pop();
            }

            if (size <= LargeBufferSize)
            {
                return LargeBuffers.Pop();
            }

            throw new InsufficientMemoryException("The requested buffer is too large");
        }

        public static void ReturnBuffer(byte[] buffer)
        {
            BuffersClearance.Add(buffer);
        }

        private static void BuffersCleaner()
        {
            byte[] buffer;
            bool available;
            do
            {
                available = BuffersClearance.TryTake(out buffer, Timeout.Infinite);
                if (available)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    switch (buffer.Length)
                    {
                        case LengthReceiver.LengthPrefixBytes:
                            LengthBuffers.Push(buffer);
                            break;
                        case TinyBufferSize:
                            TinyBuffers.Push(buffer);
                            break;
                        case SmallBufferSize:
                            SmallBuffers.Push(buffer);
                            break;
                        case MediumBufferSize:
                            MediumBuffers.Push(buffer);
                            break;
                        case LargeBufferSize:
                            LargeBuffers.Push(buffer);
                            break;
                    }
                }
            }
            while (available);
        }
    }
}