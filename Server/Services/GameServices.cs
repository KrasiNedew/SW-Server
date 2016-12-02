namespace Server.Services
{
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

        public void StartBattle(Client attacker, UserLimited defenderOff)
        {
            if (attacker.Disposed 
                || !this.server.Users.IsValidOnlineUser(attacker.User)
                || defenderOff?.Username == null
                || defenderOff.LoggedIn == false)
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            Client defender = 
                this.server
                .ClientsByUsername[defenderOff.Username];

            if (defender == null 
                || defender.Disposed 
                || !this.server.Users.IsValidOnlineUser(defender.User))
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            var attackerDTO = this.server.Players[attacker.User];
            var defenderDTO = this.server.Players[defender.User];

            BattleInfo battleInfo = BattleInfo.Create(attacker, defender, attackerDTO, defenderDTO);

            bool started = this.server.Battles.TryAdd(battleInfo);

            if (started)
            {
                var battle = this.server.Battles.GetByIdentifier(battleInfo.Identifier);
                this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleStarted, battle.DefenderDTO));
                this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleStarted, battle.AttackerDTO));
            }
            else
            {
                this.server.Responses.SomethingWentWrong(attacker);
            }
        }

        public void UpdateBattleState(Client sender, UserLimited other, BattleState senderState)
        {
            if (sender.Disposed 
                || !this.server.Users.IsValidOnlineUser(sender.User)
                || other.Username == null)
            {
                sender.ErrorsAccumulated++;
                this.server.Responses.DataNotSaved(sender);
                return;
            }
            
            BattleIdentifier identifier = BattleIdentifier.Create(sender.User.Username, other.Username);

            var battle = this.server.Battles.GetByIdentifier(identifier);

            if (battle == null)
            {
                this.server.Responses.DataNotSaved(sender);
                return;
            }

            if (sender.User.Username == battle.AttackerDTO.Username
                && sender.User.PasswordHash == battle.AttackerDTO.PasswordHash)
            {    
                    this.server.Writer.SendTo(battle.Defender, 
                        Message.Create(Service.BattleState, senderState));
            }
            else if (sender.User.Username == battle.DefenderDTO.Username
                     && sender.User.PasswordHash == battle.DefenderDTO.PasswordHash)
            {
                this.server.Writer.SendTo(battle.Attacker, 
                    Message.Create(Service.BattleState, senderState));
            }
        }

        public void UpdateFull(Client client, PlayerDTO player)
        {
            if (client.Disposed 
                || !this.server.Users.IsLoggedIn(client.User) 
                || client.User.Id != player.Id
                || client.User.Username != player.Username)
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            this.SwapPlayerData(client.User, player);
        }

        public void UpdateResources(Client client, ResourceSetDTO resourceSet)
        {
            if (client.Disposed 
                || !this.server.Users.IsLoggedIn(client.User)
                || client.User.Id != resourceSet.Id)
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            this.UpdateResourceSet(client.User, resourceSet);
        }

        public void UpdateUnits(
            Client client, ICollection<UnitDTO> units)
        {
            if (client.Disposed 
                || !this.server.Users.IsLoggedIn(client.User)
                || units.Any(u => u.OwnerId != client.User.Id))
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            this.UpdateUnits(client.User, units);
        }

        public void UpdateResourceProviders(
            Client client, ICollection<ResourceProviderDTO> resourceProviders)
        {
            if (client.Disposed 
                || !this.server.Users.IsLoggedIn(client.User)
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

            this.server.Context.Entry(attached).State = EntityState.Detached;
            this.server.Players.Remove(user);
            this.server.PlayersByUsername.Remove(user.Username);

            this.server.Context.Players.Attach(detached);
            this.server.Players.Add(user, detached);
            this.server.PlayersByUsername.Add(user.Username, detached);
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