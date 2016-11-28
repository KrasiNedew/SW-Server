namespace Server.Handlers
{
    using System;
    using System.Linq;
    using Data;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.Wrappers;

    using ServerUtils;

    public static class ServiceHandler
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

                if (player.LoggedIn)
                {
                    return ErrorCodes.AlreadyLoggedIn;
                }

                player.LoggedIn = true;
                context.SaveChanges();
                client.Validated = true;
                client.AuthData = authData;
                Writer.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));
                Console.WriteLine($"Client {client.AuthData.Username} logged in");
            }

            return 0;
        }

        public static void TryLogout(Client client)
        {
            try
            {
                if (client == null || (client != null && client.AuthData == null))
                {
                    return;
                }

                AuthDataSecure authData = client?.AuthData;

                using (SimpleWarsContext context = new SimpleWarsContext())
                {
                    var player = context.Players.FirstOrDefault(
                        p => p.Username == authData.Username && p.PasswordHash == authData.PasswordHash);

                    if (player == null)
                    {
                        return;
                    }

                    player.LoggedIn = false;
                    context.SaveChanges();
                }
            }
            catch
            {
            }
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
                Writer.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));   
            }

            return 0;
        }
    }
}