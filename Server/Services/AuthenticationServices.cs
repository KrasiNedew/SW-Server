namespace Server.Services
{
    using System;
    using System.Linq;

    using Data;
    using System.Data.Entity;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.CommHandlers;

    using ServerUtils;

    public static class AuthenticationServices
    {
        public static readonly UsersManager Users = new UsersManager();

        private static readonly Random Random = new Random();

        public static void Login(Client client)
        {
            if (client.User == null)
            {
                Responses.SomethingWentWrong(client);
                return;
            }

            // double hash for users that somehow managed to send raw password (which means they are corrupt/modified but whatever)
            client.User.PasswordHash = Hash.Generate(client.User.PasswordHash);
            if (!Users.Exists(client.User))
            {
                Responses.InvalidCredentials(client);
                return;
            }

            if (Users.IsLoggedIn(client.User))
            {
                Responses.AlreadyLoggedIn(client);
                return;
            }

            Users.MarkLogin(client.User);
            client.User = Users.GetUser(client.User);

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                int id = Users.GetId(client.User);

                PlayerDTO player =
                    context.Players.Include(p => p.ResourceProviders).Include(p => p.Units).Include(p => p.ResourceSet).FirstOrDefault(p => p.Id == id);

                if (player == null)
                {
                    Responses.InternalError(client);
                    return;
                }

                player.LoggedIn = true;
                context.SaveChanges();

                Writer.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));
                Console.WriteLine($"Client {client.User.Username} logged in");
            }

            Responses.LoginSuccess(client);
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
                    Console.WriteLine($"Client {client.User.Username} logged out");
                }
            }
            catch
            {
            }
        }

        public static void Logout(Client client)
        {
            if (client.User == null || !Users.IsLoggedIn(client.User) || client.User.Id == 0)
            {
                Responses.MustBeLoggedIn(client);
                return;
            }

            Users.MarkLogout(client.User);

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                var player = context.Players.Find(client.User.Id);

                if (player == null)
                {
                    Responses.InternalError(client);
                    return;
                }

                player.LoggedIn = false;
                context.SaveChanges();
                Console.WriteLine($"Client {client.User.Username} logged out");
            }

            Responses.LogoutSuccess(client);
        }

        public static void Register(Client client)
        {
            if (client.User == null)
            {
                Responses.SomethingWentWrong(client);
                return;
            }

            client.User.PasswordHash = Hash.Generate(client.User.PasswordHash);
            if (client.User.LoggedIn || Users.IsLoggedIn(client.User))
            {
                Responses.AlreadyLoggedIn(client);
                return;
            }

            client.User.LoggedIn = true;

            if (string.IsNullOrWhiteSpace(client.User.Username))
            {
                client.User.LoggedIn = false;
                Responses.UsernameEmpty(client);
                return;
            }

            if (string.IsNullOrWhiteSpace(client.User.PasswordHash))
            {
                client.User.LoggedIn = false;
                Responses.PasswordEmpty(client);
                return;
            }

            if (Users.GetAll().Any(u => u.Username == client.User.Username))
            {
                client.User.LoggedIn = false;
                Responses.UsernameTaken(client);
                return;
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
                Console.WriteLine($"Client {client.User.Username} registered");
            }

            Responses.RegisterSuccess(client);
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

                Console.WriteLine("All users logged out. Server shutting down.");
            }
        }
    }
}