namespace Server.Wrappers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
 
    public static class Buffers
    {
        private const int NumberOfPrefixBuffers = 5000;

        private const int NumberOfTinyBuffers = 5000;

        private const int NumberOfSmallBuffers = 5000;

        private const int NumberOfMediumBuffers = 2000;

        private const int NumberOfLargeBuffers = 200;

        public const int TinyBufferSize = 256;

        public const int SmallBufferSize = 1024;

        public const int MediumBufferSize = 16384;

        public const int LargeBufferSize = 524288;

        public static Stack<byte[]> PrefixBuffers;

        public static Stack<byte[]> TinyBuffers;

        public static Stack<byte[]> SmallBuffers;

        public static Stack<byte[]> MediumBuffers;

        public static Stack<byte[]> LargeBuffers;

        public static BlockingCollection<byte[]> BuffersClearance; 

        public static void Init()
        {
            PrefixBuffers = new Stack<byte[]>(NumberOfPrefixBuffers);
            TinyBuffers = new Stack<byte[]>(NumberOfTinyBuffers);
            SmallBuffers = new Stack<byte[]>(NumberOfSmallBuffers);
            MediumBuffers = new Stack<byte[]>(NumberOfMediumBuffers);
            LargeBuffers = new Stack<byte[]>(NumberOfLargeBuffers);

            for (int i = 0; i < NumberOfPrefixBuffers; i++)
            {
                PrefixBuffers.Push(new byte[PrefixReader.PrefixBytes]);
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

            Task.Run(() => { Cleaner(); });
        }

        public static byte[] Take(int size)
        {
            if (size <= PrefixReader.PrefixBytes)
            {
                return PrefixBuffers.Pop();
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

        public static void Return(byte[] buffer)
        {
            if (buffer == null || buffer.Length > LargeBufferSize)
            {
                return;
            }

            BuffersClearance.Add(buffer);
        }

        private static void Cleaner()
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
                        case PrefixReader.PrefixBytes:
                            PrefixBuffers.Push(buffer);
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