namespace Server.Services
{
    using System;
    using System.Linq;

    using Data;
    using System.Data.Entity;
    using System.Threading.Tasks;

    using ModelDTOs;
    using ModelDTOs.Entities;
    using ModelDTOs.Enums;

    using Server.Constants;
    using Server.Handlers;
    using Server.Wrappers;

    using ServerUtils;

    public static class AuthenticationServices
    {
        private static readonly UsersManager Users = new UsersManager();

        private static readonly Random Random = new Random();

        public static int Login(Client client)
        {            
            if (client.User == null) return -1;

            // double hash for users that somehow managed to send raw password (which means they are corrupt anyway)
            client.User.PasswordHash = Hash.Generate(client.User.PasswordHash);
            if (!Users.Exists(client.User)) return ErrorCodes.InvalidCredentialsError;

            if (Users.IsLoggedIn(client.User)) return ErrorCodes.AlreadyLoggedIn;

            Users.MarkLogin(client.User);
            client.User = Users.GetUser(client.User);

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                int id = Users.GetId(client.User);

                PlayerDTO player =
                    context.Players.Include(p => p.ResourceProviders).Include(p => p.Units).Include(p => p.ResourceSet).FirstOrDefault(p => p.Id == id);

                if (player == null)
                {
                    return ErrorCodes.InternalError;
                }

                player.LoggedIn = true;
                context.SaveChanges();

                Writer.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));
                Console.WriteLine($"Client {client.User.Username} logged in");
            }

            return 0;
        }

        public static void TryLogout(Client client)
        {
            try
            {
                if (client.User == null || !Users.IsLoggedIn(client.User) || client.User.Id == 0)
                {
                    return;
                }

                Users.MarkLogout(client.User);

                using (SimpleWarsContext context = new SimpleWarsContext())
                {
                    var player = context.Players.Find(client.User.Id);

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
            if (client.User == null || !Users.IsLoggedIn(client.User) || client.User.Id == 0)
            {
                return ErrorCodes.LogoutError;
            }

            Users.MarkLogout(client.User);

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                var player = context.Players.Find(client.User.Id);

                if (player == null)
                {
                    return ErrorCodes.InternalError;
                }

                player.LoggedIn = false;
                context.SaveChanges();
            }

            return 0;
        }

        public static int Register(Client client)
        {
            if (client.User == null) return -1;

            client.User.PasswordHash = Hash.Generate(client.User.PasswordHash);
            if (client.User.LoggedIn || Users.IsLoggedIn(client.User))
            {
                return ErrorCodes.AlreadyLoggedIn;
            }

            client.User.LoggedIn = true;

            if (string.IsNullOrWhiteSpace(client.User.Username))
            {
                client.User.LoggedIn = false;
                return ErrorCodes.UsernameEmptyError;
            }

            if (string.IsNullOrWhiteSpace(client.User.PasswordHash))
            {
                client.User.LoggedIn = false;
                return ErrorCodes.PasswordEmptyError;
            }

            if (Users.Exists(client.User))
            {
                client.User.LoggedIn = false;
                return ErrorCodes.UsernameTakenError;
            }

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                int worldSeed = Random.Next(0, 1000000000);

                PlayerDTO player = new PlayerDTO(client.User.Username, client.User.PasswordHash, worldSeed);
                player.LoggedIn = true;
                context.Players.Add(player);
                context.SaveChanges();
                context.Entry(player).Reload();
                client.User.Id = player.Id;
                Users.MarkRegister(client.User);

                Writer.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));
            }

            return 0;
        }

        public static void LogoutAllUsers()
        {
            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                lock (Users)
                {
                    foreach (var user in Users.GetAll())
                    {
                        context.Players.Find(user.Id).LoggedIn = false;
                    }

                    context.SaveChanges();
                }
            }
        }
    }
}