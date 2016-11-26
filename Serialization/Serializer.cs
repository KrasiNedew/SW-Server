namespace Serialization
{
    using System.Text;

    using ModelDTOs;

    using Newtonsoft.Json;

    public static class Serializer
    {
        public static byte[] SerializePlayer(PlayerDTO player)
        {
            string data = JsonConvert.SerializeObject(player);

            return SerializeString(data);
        }

        public static byte[] SerializeString(string data)
        {
            return Encoding.ASCII.GetBytes(data);
        }
    }
}