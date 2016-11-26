namespace Serialization
{
    using ModelDTOs;
    using ModelDTOs.Entities;

    using Newtonsoft.Json;

    using ServerUtils;

    public static class Deserializer
    {
        public static AuthData ExtractAuthData(string data)
        {
            var relevantData = JsonConvert.DeserializeAnonymousType(
                data,
                new { Username = string.Empty, Password = string.Empty });

            return new AuthData(relevantData.Username, relevantData.Password);
        }

        public static ServiceRequest ExtractServiceRequest(string data)
        {
            var relevantData = JsonConvert.DeserializeAnonymousType(data, new { ServiceType = ServiceRequest.Unknown });

            return relevantData.ServiceType;
        }

        public static PlayerDTO DeserializePlayer(string data)
        {
            PlayerDTO player = JsonConvert.DeserializeObject<PlayerDTO>(data);
            player.PasswordHash = AuthData.GenerateHash(player.PasswordHash);

            return player;
        }

        public static BaseEntityType ExtractBaseEntityType(string data)
        {
            var relevantData = JsonConvert.DeserializeAnonymousType(data, new { BaseType = BaseEntityType.Unknown });

            return relevantData.BaseType;
        }

        public static UnitDTO DeserializeUnit(string data)
        {
            UnitDTO unit = JsonConvert.DeserializeObject<UnitDTO>(data);

            return unit;
        }

        public static ResourceProviderDTO DeserializeResourceProvider(string data)
        {
            ResourceProviderDTO resourceProvider = JsonConvert.DeserializeObject<ResourceProviderDTO>(data);

            return resourceProvider;
        }
    }
}