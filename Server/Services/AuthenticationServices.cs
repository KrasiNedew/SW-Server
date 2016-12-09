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

            var loginData = ((Message<AuthDTO>)message).Data;
            loginData.PasswordHash = Hash.Generate(loginData.PasswordHash);
            if (this.server.Players.ContainsKey(client.Id))
            {
                this.server.Responses.AlreadyLoggedIn(client);
                return;
            }

            PlayerDTO player = this.server.Context.Players.Include(p => p.Units).Include(p => p.ResourceProviders).FirstOrDefault(p => p.Username == loginData.Username && p.PasswordHash == loginData.PasswordHash && !p.LoggedIn);

            if (player == null)
            {
                this.server.Responses.InvalidCredentials(client);
                return;
            }

            player.MapEntites();
            player.LoggedIn = true;
            this.server.Players.TryAdd(client.Id, player);

            this.server.Writer.SendTo(client, 
                Message.Create(Service.OwnPlayerData, player));

            Console.WriteLine($"User {loginData.Username} logged in");
        }

        public void TryLogout(Client client)
        {
            try
            {
                if (client.Disposed)
                {
                    return;
                }

                PlayerDTO player;
                this.server.Players.TryRemove(client.Id, out player);
                player.LoggedIn = false;       
                this.server.Context.BulkSaveChanges();
                this.server.Context.Entry(player).State = EntityState.Detached;

                if (client.BattleId != Guid.Empty)
                {
                    this.server.Game.EndBattle(client);
                }

                Console.WriteLine($"User {player.Username} logged out");
            }
            catch
            {
            }
        }

        public void Logout(Client client)
        {
            if (client.Disposed) return;

            if (!this.server.Players.ContainsKey(client.Id))
            {
                this.server.Responses.MustBeLoggedIn(client);
                return;
            }

            PlayerDTO player;
            this.server.Players.TryRemove(client.Id, out player);
            player.LoggedIn = false;
            this.server.Context.BulkSaveChanges();
            this.server.Context.Entry(player).State = EntityState.Detached;

            if (client.BattleId != Guid.Empty)
            {
                this.server.Game.EndBattle(client);
            }

            Console.WriteLine($"User {player.Username} logged out");

            this.server.Responses.LogoutSuccess(client);
        }

        public void Register(Client client, Message message)
        {
            if (client.Disposed) return;

            if (this.server.Players.ContainsKey(client.Id))
            {
                this.server.Responses.AlreadyLoggedIn(client);
                return;
            }

            var regData = ((Message<AuthDTO>)message).Data;
            regData.PasswordHash = Hash.Generate(regData.PasswordHash);
     
            if (!this.ValidateForRegister(client, regData)) return;

            int worldSeed = Random.Next(0, 1000000000);

            PlayerDTO player = 
            new PlayerDTO(regData.Username, regData.PasswordHash, worldSeed)
            {
                LoggedIn = true
            };

            this.server.Context.Players.Add(player);
            this.server.Context.BulkSaveChanges();

            this.server.Players.TryAdd(client.Id, player);

            this.server.Writer.SendTo(client, 
                Message.Create(Service.OwnPlayerData, player));

            Console.WriteLine($"User {regData.Username} registered");
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

        private bool ValidateForRegister(Client client, AuthDTO regData)
        {
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

            if (this.server.Context.Players.Any(p => p.Username == regData.Username))
            {
                this.server.Responses.UsernameTaken(client);
                return false;
            }

            return true;
        }
    }
}