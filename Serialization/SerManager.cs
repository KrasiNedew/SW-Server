namespace Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using ProtoBuf;

    using ServerUtils;

    public static class SerManager
    {
        /// <summary>   
        /// Serializes any object that has proto contract 
        /// to managed buffer from the provided buffers. 
        /// </summary>
        public static Tuple<byte[], int> SerializeToManagedBuffer<T>(
            T obj, Buffers buffers)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                byte[] buffer = buffers.Take((int)ms.Length);
                Array.Copy(ms.ToArray(), buffer, ms.Length);
                return Tuple.Create(buffer, (int)ms.Length);
            }
        }

        /// <summary>   
        /// Serializes any object that has proto contract
        /// to managed buffer from the provided buffers 
        /// and encodes the length of the buffer in the first 5 bytes. 
        /// </summary>
        public static Tuple<byte[], int> SerializeToManagedBufferPrefixed<T>(
            T obj, Buffers buffers)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Seek(5, SeekOrigin.Begin);
                Serializer.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                Serializer.Serialize(ms, ms.Length);
                byte[] buffer = buffers.Take((int)ms.Length);
                Array.Copy(ms.ToArray(), buffer, ms.Length);
                return Tuple.Create(buffer, (int)ms.Length);
            }
        }

        public static byte[] Serialize<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static byte[] SerializeWithLengthPrefix<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Seek(5, SeekOrigin.Begin);
                Serializer.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                Serializer.Serialize(ms, ms.Length);
                return ms.ToArray();
            }
        }

        public static int GetLengthPrefix(byte[] data)
        {
            if (data.Length < 5)
            {
                return 0;
            }

            if (data[0] != 8)
            {
                // invalid prefix sent
                return -1;
            }

            List<byte> prefixBytes = new List<byte>(5) { 8 };
            for (int i = 1; i < 5; i++)
            {
                if (data[i] != 0)
                {
                    prefixBytes.Add(data[i]);
                }
            }

            if (prefixBytes.Count <= 1)
            {
                return 0;
            }

            using (MemoryStream ms = new MemoryStream(prefixBytes.ToArray()))
            {
                return Serializer.Deserialize<int>(ms);
            }
        }

        public static T DeserializeWithLengthPrefix<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data, 0, FindEnd(data)))
            {
                ms.Seek(5, SeekOrigin.Begin);
                return Serializer.Deserialize<T>(ms);
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data, 0, FindEnd(data)))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }

        private static int FindEnd(byte[] buffer)
        {
            int i = buffer.Length - 1;
            while (buffer[i] == 0)
            {
                i--;
            }

            return i + 1;
        }
    }
}