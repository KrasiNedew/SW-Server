namespace Serialization
{
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
                Serializer.SerializeWithLengthPrefix(ms, obj, PrefixStyle.Base128);
                return ms.ToArray();
            }
        }

        public static T DeserializeWithLengthPrefix<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                return Serializer.DeserializeWithLengthPrefix<T>(ms, PrefixStyle.Base128);
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