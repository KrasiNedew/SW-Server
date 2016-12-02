namespace Server.Services
{
    using System;
    using System.Data.Entity;
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

        public void Login(Client client, Message message)
        {
            if (client.Disposed) return;

            var loginData = ((Message<UserFull>)message).Data;
            var users = this.server.Users;

            bool valid = this.ValidateForLogin(client, users, loginData);

            if (!valid) return;

            UserLimited forShare = UserLimited.Create(client.User.Username, true);

            users.MarkLogin(client.User);
            PlayerDTO player = this.server.Context.Players.Find(client.User.Id);

            if (player == null)
            {
                users.MarkLogout(client.User);
                client.User = null;
                this.server.Responses.InternalError(client);
                return;
            }

            player.LoggedIn = true;
            this.server.Players.TryAdd(client.User, player);
            this.server.ClientsByUsername.TryAdd(client.User.Username, client);
            this.server.PlayersByUsername.TryAdd(client.User.Username, player);
            this.server.UsersForShare.TryAdd(forShare, forShare);

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

                if (client.Disposed 
                    || users.IsValidOnlineUser(client.User) 
                    || !this.server.Players.ContainsKey(client.User))
                {
                    return;
                }

                
                var player = this.server.Players[client.User];
                player.LoggedIn = false;       
                this.server.Context.BulkSaveChanges();
                this.server.Context.Entry(player).State = EntityState.Detached;;

                users.MarkLogout(client.User);
                Client removed;
                PlayerDTO pRemoved;
                this.server.ClientsByUsername.TryRemove(client.User.Username, out removed);
                this.server.Players.TryRemove(client.User, out pRemoved);
                this.server.PlayersByUsername.TryRemove(client.User.Username, out pRemoved);

                UserLimited userRemoved = UserLimited.Create(client.User.Username, client.User.LoggedIn);
                this.server.UsersForShare.TryRemove(userRemoved, out userRemoved);

                var battle = this.server.Battles.GetByUsername(removed.User.Username);
                if (battle != null)
                {
                    this.server.Game.EndBattle(removed);
                }

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

            if (!users.IsValidOnlineUser(client.User) || !this.server.Players.ContainsKey(client.User))
            {
                this.server.Responses.MustBeLoggedIn(client);
                return;
            }


            var player = this.server.Players[client.User];
            player.LoggedIn = false;
            this.server.Context.BulkSaveChanges();
            this.server.Context.Entry(player).State = EntityState.Detached;

            users.MarkLogout(client.User);
            Client removed;
            PlayerDTO pRemoved;
            this.server.ClientsByUsername.TryRemove(client.User.Username, out removed);
            this.server.Players.TryRemove(client.User, out pRemoved);
            this.server.PlayersByUsername.TryRemove(client.User.Username, out pRemoved);

            UserLimited userRemoved = UserLimited.Create(client.User.Username, client.User.LoggedIn);
            this.server.UsersForShare.TryRemove(userRemoved, out userRemoved);

            var battle = this.server.Battles.GetByUsername(removed.User.Username);
            if (battle != null)
            {
                this.server.Game.EndBattle(removed);
            }

            Console.WriteLine($"Client {client.User.Username} logged out");
            client.User = null;

            this.server.Responses.LogoutSuccess(client);
        }

        public void Register(Client client, Message message)
        {
            if (client.Disposed) return;

            var regData = ((Message<UserFull>)message).Data;
            var users = this.server.Users;

            bool valid = this.ValidateForRegister(client, users, regData);

            if (!valid) return;

            int worldSeed = Random.Next(0, 1000000000);

            PlayerDTO player = 
            new PlayerDTO(client.User.Username, client.User.PasswordHash, worldSeed)
            {
                LoggedIn = true
            };

            this.server.Context.Players.Add(player);
            this.server.Context.BulkSaveChanges();

            client.User.Id = player.Id;
            users.MarkRegister(client.User);

            this.server.ClientsByUsername.TryAdd(client.User.Username, client);
            this.server.Players.TryAdd(client.User, player);
            this.server.PlayersByUsername.TryAdd(client.User.Username, player);
            UserLimited forShare = UserLimited.Create(client.User.Username, client.User.LoggedIn);
            this.server.UsersForShare.TryAdd(forShare, forShare);

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

        private bool ValidateForLogin(Client client, UsersManager users, UserFull loginData)
        {
            if (client.User != null) return false;

            loginData.PasswordHash = Hash.Generate(loginData.PasswordHash);
            var user = users.GetUser(loginData);

            if (!users.Exists(user) || !users.IsValidOfflineUser(user))
            {
                this.server.Responses.InvalidCredentials(client);
                return false;
            }

            if (users.IsValidOnlineUser(user))
            {
                this.server.Responses.AlreadyLoggedIn(client);
                return false;
            }

            client.User = user;
            return true;
        }

        private bool ValidateForRegister(Client client, UsersManager users, UserFull regData)
        {
            if (client.User != null)
            {
                this.server.Responses.AlreadyLoggedIn(client);
                return false;
            }

            if (string.IsNullOrWhiteSpace(regData.Username))
            {
                this.server.Responses.UsernameEmpty(client);
                return false;
            }

            if (string.IsNullOrWhiteSpace(regData.PasswordHash))
            {
                this.server.Responses.PasswordEmpty(client);
                return false;
            }

            if (users.GetAll().Any(u => u.Username == regData.Username))
            {
                this.server.Responses.UsernameTaken(client);
                return false;
            }

            regData.PasswordHash = Hash.Generate(regData.PasswordHash);
            var user = users.GetUser(regData);

            if (users.IsValidOnlineUser(user))
            {
                this.server.Responses.AlreadyLoggedIn(client);
                return false;
            }

            if (user == null)
            {
                this.server.Responses.SomethingWentWrong(client);
                return false;
            }

            client.User = user;
            return true;
        }
    }
}