namespace Server.Services
{
    using System;
    using System.Linq;

    using Data;
    using System.Data.Entity;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using ServerUtils;
    using ServerUtils.Wrappers;

    public class AuthenticationServices
    {
        private static readonly Random Random = new Random();

        private readonly AsynchronousSocketListener server;

        public AuthenticationServices(AsynchronousSocketListener server)
        {
            this.server = server;
        }

        public void Login(Client client)
        {
            if (client.Disposed) return;

            var users = this.server.Users;

            if (users.IsValidOfflineUser(client.User))
            {
                this.server.Responses.SomethingWentWrong(client);
                return;
            }

            // double hash for Users that somehow managed to send raw password (which means they are corrupt/modified but whatever)
            client.User.PasswordHash = Hash.Generate(client.User.PasswordHash);
            if (!users.Exists(client.User))
            {
                this.server.Responses.InvalidCredentials(client);
                return;
            }

            if (users.IsLoggedIn(client.User))
            {
                this.server.Responses.AlreadyLoggedIn(client);
                return;
            }

            users.MarkLogin(client.User);
            client.User = users.GetUser(client.User);
            this.server.ClientsByUsername.Add(client.User.Username, client);

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                int id = users.GetId(client.User);
                context.Database.ExecuteSqlCommand("LoginUser", id);

                PlayerDTO player =
                    context.Players.Include(p => p.ResourceProviders).Include(p => p.Units).Include(p => p.ResourceSet).FirstOrDefault(p => p.Id == id);

                if (player == null)
                {
                    this.server.Responses.InternalError(client);
                    return;
                }

                this.server.Writer.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));
                Console.WriteLine($"Client {client.User.Username} logged in");
            }

            this.server.Responses.LoginSuccess(client);
        }

        public void TryLogout(Client client)
        {
            try
            {
                var users = this.server.Users;

                if (users.IsValidOnlineUser(client.User) || client.Disposed)
                {
                    return;
                }

                users.MarkLogout(client.User);
                this.server.ClientsByUsername.Remove(client.User.Username);

                using (SimpleWarsContext context = new SimpleWarsContext())
                {
                    context.Database.ExecuteSqlCommand("LogoutUser", client.User.Id);
                    Console.WriteLine($"Client {client.User.Username} logged out");
                }
            }
            catch
            {
            }
        }

        public void Logout(Client client)
        {
            if (client.Disposed) return;

            var users = this.server.Users;

            if (users.IsValidOnlineUser(client.User))
            {
                this.server.Responses.MustBeLoggedIn(client);
                return;
            }

            users.MarkLogout(client.User);
            this.server.ClientsByUsername.Remove(client.User.Username);

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                context.Database.ExecuteSqlCommand("LogoutUser", client.User.Id);
                Console.WriteLine($"Client {client.User.Username} logged out");
            }

            this.server.Responses.LogoutSuccess(client);
        }

        public void Register(Client client)
        {
            if (client.Disposed) return;

            if (client.User == null)
            {
                this.server.Responses.SomethingWentWrong(client);
                return;
            }

            var users = this.server.Users;
            client.User.PasswordHash = Hash.Generate(client.User.PasswordHash);

            if (client.User.LoggedIn || users.IsLoggedIn(client.User))
            {
                this.server.Responses.AlreadyLoggedIn(client);
                return;
            }

            client.User.LoggedIn = true;

            if (string.IsNullOrWhiteSpace(client.User.Username))
            {
                client.User.LoggedIn = false;
                this.server.Responses.UsernameEmpty(client);
                return;
            }

            if (string.IsNullOrWhiteSpace(client.User.PasswordHash))
            {
                client.User.LoggedIn = false;
                this.server.Responses.PasswordEmpty(client);
                return;
            }

            if (users.GetAll().Any(u => u.Username == client.User.Username))
            {
                client.User.LoggedIn = false;
                this.server.Responses.UsernameTaken(client);
                return;
            }

            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                int worldSeed = Random.Next(0, 1000000000);

                PlayerDTO player = new PlayerDTO(client.User.Username, client.User.PasswordHash, worldSeed);
                player.LoggedIn = true;
                context.Players.Add(player);
                context.SaveChanges();
                client.User.Id = player.Id;
                users.MarkRegister(client.User);
                this.server.ClientsByUsername.Add(client.User.Username, client);

                this.server.Writer.SendTo(client, new Message<PlayerDTO>(Service.PlayerData, player));
                Console.WriteLine($"Client {client.User.Username} registered");
            }

            this.server.Responses.RegisterSuccess(client);
        }

        public static void LogoutAllUsers()
        {
            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                context.Database.ExecuteSqlCommand("LogoutAllUsers");
                Console.WriteLine("All Users logged out. Server shutting down.");
            }
        }
    }
}