namespace Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;

    using ModelDTOs;
    using ModelDTOs.Entities;
    using ModelDTOs.Enums;
    using ModelDTOs.Resources;

    using Server.CommHandlers;
    using ServerUtils;
    using ServerUtils.Wrappers;

    using Z.EntityFramework.Plus;

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
                || this.server.Users.IsValidOnlineUser(attacker.User)
                || defenderOff.Username == null)
            {
                this.server.Responses.SomethingWentWrong(attacker);
            }

            Client defender = 
                this.server
                .Clients
                .FirstOrDefault(c => c.User?.Username == defenderOff.Username);

            if (defender == null 
                || defender.Disposed 
                || !this.server.Users.IsValidOnlineUser(defender.User))
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            BattleIdentifier identifier = BattleIdentifier.Create(attacker.User.Username, defender.User.Username);

            bool started = this.server.Battles.TryStart(identifier, attacker, defender);

            if (started)
            {
                var battle = this.server.Battles.GetByIdentifier(identifier);
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
                || !server.Users.IsLoggedIn(client.User) 
                || client.User.Id != player.Id
                || client.User.Username != player.Username)
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            try
            {
                using (SimpleWarsContext context = new SimpleWarsContext())
                {
                    context
                        .Players
                        .Where(p => p.Id == client.User.Id)
                        .Update(p => player);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.server.Responses.DataNotSaved(client);
            }
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

            try
            {
                using (SimpleWarsContext context = new SimpleWarsContext())
                {
                    context.ResourceSets
                        .Where(rs => rs.Id == client.User.Id)
                        .Update(rs => resourceSet);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.server.Responses.DataNotSaved(client);
            }
        }

        public void UpdateUnits(
            Client client, ICollection<UnitDTO> units)
        {
            if (client.Disposed 
                || !this.server.Users.IsLoggedIn(client.User)
                || units.Any(e => e.Id != client.User.Id))
            {
                this.server.Responses.DataNotSaved(client);
                return;
            }

            var map = units.ToDictionary(u => u.Id, u => u);

            try
            {
                using (SimpleWarsContext context = new SimpleWarsContext())
                {
                    var filtered = context.Units
                        .Where(u => map.ContainsKey(u.Id))
                        .ToDictionary(u => u.Id, u => u);
                       
                    foreach (var id in filtered.Keys)
                    {
                        filtered[id] = map[id];
                    }

                    context.BulkSaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.server.Responses.DataNotSaved(client);
            }
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

            var map = resourceProviders.ToDictionary(e => e.Id, e => e);

            try
            {
                using (SimpleWarsContext context = new SimpleWarsContext())
                {
                    var filtered =
                        context.ResourceProviders
                        .Where(rp => map.ContainsKey(rp.Id))
                        .ToDictionary(rp => rp.Id, rp => rp);

                    foreach (var id in filtered.Keys)
                    {
                        filtered[id] = map[id];
                    }

                    context.BulkSaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.server.Responses.DataNotSaved(client);
            }
        }
    }
}