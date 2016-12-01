namespace Server.Services
{
    using System;
    using System.Linq;

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

            UserLimited forShare = UserLimited.Create(client.User.Username, true);
            if (users.IsLoggedIn(client.User) || this.server.Players.ContainsKey(client.User) || this.server.PlayersByUsername.ContainsKey(client.User.Username) || this.server.UsersForShare.Contains(forShare))
            {
                this.server.Responses.AlreadyLoggedIn(client);
                return;
            }

            users.MarkLogin(client.User);
            client.User = users.GetUser(client.User);
            PlayerDTO player = this.server.Context.Players.Find(users.GetId(client.User));

            if (player == null)
            {
                users.MarkLogout(client.User);
                client.User = null;
                this.server.Responses.InternalError(client);
                return;
            }

            player.LoggedIn = true;
            this.server.Players.Add(client.User, player);
            this.server.ClientsByUsername.Add(client.User.Username, client);
            this.server.PlayersByUsername.Add(client.User.Username, player);
            this.server.UsersForShare.Add(forShare);

            this.server.Writer.SendTo(client, 
                Message.Create(Service.PlayerData, player));
            this.server.Responses.LoginSuccess(client);

            Console.WriteLine($"Client {client.User.Username} logged in");
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
                if (this.server.Players.ContainsKey(client.User))
                {
                    this.server.Players[client.User].LoggedIn = false;                 
                }

                this.server.Players.Remove(client.User);
                this.server.PlayersByUsername.Remove(client.User.Username);
                this.server.UsersForShare.Remove(UserLimited.Create(client.User.Username, client.User.LoggedIn));

                Console.WriteLine($"Client {client.User.Username} logged out");
                client.User = null;
            }
            catch
            {
            }
        }

        public void Logout(Client client)
        {
            if (client.Disposed) return;

            var users = this.server.Users;

            if (!users.IsValidOnlineUser(client.User))
            {
                this.server.Responses.MustBeLoggedIn(client);
                return;
            }

            users.MarkLogout(client.User);
            this.server.ClientsByUsername.Remove(client.User.Username);

            if (this.server.Players.ContainsKey(client.User))
            {
                this.server.Players[client.User].LoggedIn = false;            
            }

            this.server.Players.Remove(client.User);
            this.server.PlayersByUsername.Remove(client.User.Username);
            this.server.UsersForShare.Remove(UserLimited.Create(client.User.Username, client.User.LoggedIn));

            Console.WriteLine($"Client {client.User.Username} logged out");
            client.User = null;

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

                int worldSeed = Random.Next(0, 1000000000);

                PlayerDTO player = new PlayerDTO(client.User.Username, client.User.PasswordHash, worldSeed);
                player.LoggedIn = true;
                this.server.Context.Players.Add(player);
                this.server.Context.SaveChanges();
                client.User.Id = player.Id;
                users.MarkRegister(client.User);
                this.server.ClientsByUsername.Add(client.User.Username, client);
                this.server.Players.Add(client.User, player);
                this.server.PlayersByUsername.Add(client.User.Username, player);
                this.server.UsersForShare.Add(UserLimited.Create(client.User.Username, client.User.LoggedIn));

                this.server.Writer.SendTo(client, 
                    Message.Create(Service.PlayerData, player));
                this.server.Responses.RegisterSuccess(client);

            Console.WriteLine($"Client {client.User.Username} registered");
        }

        public void LogoutAllUsers()
        {
            try
            {
                foreach (var player in this.server.Context.Players)
                {
                    player.LoggedIn = false;
                }

                this.server.Context.BulkSaveChanges();

                Console.WriteLine("All Users logged out. Server shutting down.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}