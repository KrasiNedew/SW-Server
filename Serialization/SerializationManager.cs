namespace Serialization
{
    using System.Collections.Generic;
    using System.IO;

    using ProtoBuf;

    public static class SerializationManager
    {
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

        public static int GetLength(byte[] data)
        {
            if (data.Length < 5)
            {
                return 0;
            }

            List<byte> length = new List<byte>(5) { 8 };
            bool gotAny = false;

            for (int i = 1; i < 5; i++)
            {
                if (data[i] != 0)
                {
                    gotAny = true;
                    length.Add(data[i]);
                }
            }

            if (gotAny)
            {
                using (MemoryStream ms = new MemoryStream(length.ToArray()))
                {
                    return Serializer.Deserialize<int>(ms);
                }
            }

            return 0;
        }

        public static T DeserializeWithLengthPrefix<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                ms.Seek(5, SeekOrigin.Begin);
                return Serializer.Deserialize<T>(ms);
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }
    }
}