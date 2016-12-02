namespace Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;

    using ModelDTOs;
    using ModelDTOs.Entities;
    using ModelDTOs.Enums;
    using ModelDTOs.Resources;

    using ServerUtils.Wrappers;

    public class GameServices
    {
        private readonly AsynchronousSocketListener server;

        public GameServices(AsynchronousSocketListener server)
        {
            this.server = server;
        }

        public void StartBattle(Client attacker, Message message)
        {
            if (attacker.Disposed ) return;
            if (!this.server.Users.IsValidOnlineUser(attacker.User))
            {
                this.server.Responses.MustBeLoggedIn(attacker);
                return;
            }

            var defenderUser = ((Message<UserLimited>)message).Data;
            if (defenderUser?.Username == null
                || defenderUser.LoggedIn == false)
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            Client defender = 
                this.server
                .ClientsByUsername[defenderUser.Username];

            if (defender == null 
                || defender.Disposed 
                || !this.server.Users.IsValidOnlineUser(defender.User))
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            var attackerDTO = this.server.Players[attacker.User];
            var defenderDTO = this.server.Players[defender.User];

            BattleInfo battle = new BattleInfo(attacker, defender, attackerDTO, defenderDTO);

            bool started = this.server.Battles.TryAdd(battle);

            if (started)
            {
                this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleStarted, battle.DefenderDTO));
                this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleStarted, battle.AttackerDTO));
            }
            else
            {
                this.server.Responses.SomethingWentWrong(attacker);
            }
        }

        public void EndBattle(Client client)
        {
            var battle = this.server.Battles.GetByUsername(client.User.Username);
            bool ended = this.server.Battles.TryEnd(battle);
            if (!ended) return;

            if (client == battle.Attacker)
            {
                this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleEnd, "You lost"));
                this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleEnd, "You won"));
            }
            else
            {
                this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleEnd, "You won"));
                this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleEnd, "You lost"));
            }
        }

        public void EndBattle(BattleInfo battle)
        {
            bool ended = this.server.Battles.TryEnd(battle);
            if (!ended) return;

            var winner = battle.Score.CalculateWinner(battle.AttackerDTO, battle.DefenderDTO);

            if (winner > 0)
            {
                this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleEnd, "You won"));
                this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleEnd, "You lost"));
            }
            else if (winner < 0)
            {
                this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleEnd, "You won"));
                this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleEnd, "You lost"));
            }
            else
            {
                this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleEnd, "Draw"));
                this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleEnd, "Draw"));
            }
        }

        public void UpdateFull(Client client, Message message)
        {
            if (client.Disposed) return;

            var player = ((Message<PlayerDTO>)message).Data;
            if (!this.server.Users.IsLoggedIn(client.User) 
                || client.User.Id != player.Id
                || this.server.PlayersByUsername[player.Username].PasswordHash != client.User.PasswordHash)
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            this.SwapPlayerData(client.User, player);
        }

        public void UpdateResources(Client client, Message message)
        {
            if (client.Disposed) return;

            var resourceSet = ((Message<ResourceSetDTO>)message).Data;
            if (!this.server.Users.IsLoggedIn(client.User)
                || client.User.Id != resourceSet.Id)
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            this.UpdateResourceSet(client.User, resourceSet);
        }

        public void UpdateUnits(Client client, Message message)
        {
            if (client.Disposed) return;

            var units = ((Message<ICollection<UnitDTO>>)message).Data;
            if (!this.server.Users.IsLoggedIn(client.User)
                || units.Any(u => u.OwnerId != client.User.Id))
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            var battle = this.server.Battles.GetByUsername(client.User.Username);

            if (battle != null)
            {
                battle.LastUpdate = DateTime.Now;
                this.server.Writer.SendTo(
                    battle.Attacker.User.Username == client.User.Username
                    ? battle.Defender : battle.Attacker,
                    message);
            }

            this.UpdateUnits(client.User, units);         
        }

        public void UpdateResourceProviders(Client client, Message message)
        {
            if (client.Disposed) return;

            var resourceProviders = ((Message<ICollection<ResourceProviderDTO>>)message).Data;
            if (!this.server.Users.IsLoggedIn(client.User)
                || resourceProviders.Any(e => e.Id != client.User.Id))
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            this.UpdateResourceProviders(client.User, resourceProviders);
        }

        private void UpdateResourceSet(UserFull user, ResourceSetDTO changedResSet)
        {
            if (!this.ValidateForUpdate(user))
            {
                return;
            }

            var player = this.server.Players[user];

            // ugly as fuck
            player.ResourceSet.Food.Quantity = changedResSet.Food.Quantity;
            player.ResourceSet.Gold.Quantity = changedResSet.Gold.Quantity;
            player.ResourceSet.Wood.Quantity = changedResSet.Wood.Quantity;
            player.ResourceSet.Metal.Quantity = changedResSet.Metal.Quantity;
            player.ResourceSet.Rock.Quantity = changedResSet.Rock.Quantity;
            player.ResourceSet.Population.Quantity = changedResSet.Population.Quantity;
        }

        private void UpdateResourceProviders(UserFull user, ICollection<ResourceProviderDTO> changedResProv)
        {
            if (!this.ValidateForUpdate(user))
            {
                return;
            }

            var player = this.server.Players[user];

            foreach (var changed in changedResProv)
            {
                var resProv = player.ResourceProviders.First(rp => rp.Id == changed.Id);

                resProv.Quantity = changed.Quantity;
            }
        }

        private void UpdateUnits(UserFull user, ICollection<UnitDTO> changedUnits)
        {
            if (!this.ValidateForUpdate(user))
            {
                return;
            }

            var player = this.server.Players[user];

            foreach (var changed in changedUnits)
            {
                var unit = player.Units.First(u => u.Id == changed.Id);

                unit.Health = changed.Health;
                unit.IsAlive = changed.IsAlive;

                unit.PosX = changed.PosX;
                unit.PosY = changed.PosY;
                unit.PosZ = changed.PosZ;

                unit.RotX = changed.RotX;
                unit.RotY = changed.RotY;
                unit.RotZ = changed.RotZ;
            }
        }

        private void SwapPlayerData(UserFull user, PlayerDTO detached)
        {
            if (!this.ValidateForUpdate(user))
            {
                return;
            }

            var attached = this.server.Players[user];

            PlayerDTO removed;
            this.server.Context.Entry(attached).State = EntityState.Detached;
            this.server.Players.TryRemove(user, out removed);
            this.server.PlayersByUsername.TryRemove(user.Username, out removed);

            this.server.Context.Players.Attach(detached);
            this.server.Players.TryAdd(user, detached);
            this.server.PlayersByUsername.TryAdd(user.Username, detached);
        }

        private bool ValidateForUpdate(UserFull user)
        {
            if (this.server.Players.ContainsKey(user))
            {
                return true;
            }

            this.server.Responses.DataNotSaved(this.server.ClientsByUsername[user.Username]);
            return false;
        }
    }
}