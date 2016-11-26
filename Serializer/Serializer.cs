namespace Serializer
{
    using ModelDTOs;

    using Newtonsoft.Json;

    public static class Serializer
    {
        public static AuthData ExtractUsernameAndPassword(string data)
        {
            var relevantData = JsonConvert.DeserializeAnonymousType(
                data,
                new { Username = string.Empty, Password = string.Empty });

            return new AuthData(relevantData.Username, relevantData.Password);
        }
    }
}