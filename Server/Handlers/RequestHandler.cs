namespace Server.Handlers
{
    using Serialization;

    using Server.Wrappers;

    using ServerUtils;

    public static class RequestHandler
    {
        public static int Login(ConnectedClient client)
        {
            // logic for login
            client.Validated = true;

            return 0;
        }

        public static int Logout(ConnectedClient client)
        {
            // logout client

            return 0;
        }

        public static int Register(ConnectedClient client)
        {
            if (client.Validated)
            {
                return ErrorCodes.AlreadyLoggedIn;
            }

            //AuthData credentials = Deserializer
            //        .ExtractAuthData(data);
            //client.AuthData = credentials;
            return 0;
        }
    }
}