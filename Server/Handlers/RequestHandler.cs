namespace Server.Handlers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Data;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Serialization;

    using Server.Wrappers;

    using ServerUtils;

    public static class RequestHandler
    {
        public static int Login(Client client, AuthDataSecure authData)
        {
            if (client.Validated)
            {
                return ErrorCodes.AlreadyLoggedIn;
            }

 
            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                PlayerDTO player =
              context.Players.FirstOrDefault(
                  p => p.Username == authData.Username && p.PasswordHash == authData.PasswordHash);

                if (player == null)
                {
                    return ErrorCodes.InvalidCredentialsError;
                }

                client.Validated = true;
                client.AuthData = authData;
                ServerManager.Instance.Listener.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));
            }

            return 0;
        }

        public static int Logout(Client client)
        {
            if (!client.Validated)
            {
                return ErrorCodes.LogoutError;
            }

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                var player = context.Players.FirstOrDefault(
                    p => p.Username == client.AuthData.Username && p.PasswordHash == client.AuthData.PasswordHash);

                if (player == null)
                {
                    return ErrorCodes.InternalError;
                }

                player.LoggedIn = false;
                context.SaveChanges();
                client.Validated = false;
                client.AuthData = null;
            }

            return 0;
        }

        public static int Register(Client client, AuthDataSecure authData)
        {
            if (client.Validated)
            {
                return ErrorCodes.AlreadyLoggedIn;
            }

            if (string.IsNullOrWhiteSpace(authData.Username))
            {
                return ErrorCodes.UsernameEmptyError;
            }

            if (string.IsNullOrWhiteSpace(authData.PasswordHash))
            {
                return ErrorCodes.PasswordEmptyError;
            }

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                if (context.Players.Any(p => p.Username == authData.Username))
                {
                    return ErrorCodes.UsernameTakenError;
                }

                PlayerDTO player = new PlayerDTO(authData.Username, authData.PasswordHash, 11293941, 100, 100);
                context.Players.Add(player);
                context.SaveChanges();
                client.AuthData = authData;
                client.Validated = true;
                ServerManager.Instance.Listener.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));   
            }

            return 0;
        }
    }
}