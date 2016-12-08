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

        public void SendOtherPlayers(Client client)
        {
            if(client.Disposed) return;
            if (!this.server.Players.ContainsKey(client.Id))
            {
                this.server.Responses.MustBeLoggedIn(client);
                return;
            }

            Message<string[]> message = Message.Create(
                Service.OtherPlayers,
                this.server.Players.Values.Select(p => p.Username).ToArray());

            this.server.Writer.SendTo(client, message);
        }

        public void StartBattle(Client attacker, Message message)
        {
            if (attacker.Disposed ) return;
            if (!this.server.Players.ContainsKey(attacker.Id))
            {
                this.server.Responses.MustBeLoggedIn(attacker);
                return;
            }

            if (attacker.BattleId != Guid.Empty)
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            var defenderUsername = ((Message<string>)message).Data;
            if (defenderUsername == null)
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            Guid id;
            try
            {
                id = this.server.Players
                    .FirstOrDefault(e => e.Value.Username == defenderUsername).Key;
            }
            catch
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            Client defender;
            bool gotIt = this.server.Clients.TryGetValue(id, out defender);

            if (!gotIt 
                || defender.Disposed
                || defender.BattleId != Guid.Empty)
            {
                this.server.Responses.SomethingWentWrong(attacker);
                return;
            }

            BattleInfo battle = new BattleInfo(attacker, defender);

            bool started = this.server.Battles.TryAdd(battle.Id, battle);

            if (started)
            {
                attacker.BattleId = battle.Id;
                defender.BattleId = battle.Id;
                this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleStarted, this.server.Players[battle.Defender.Id]));
                this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleStarted, this.server.Players[battle.Attacker.Id]));
            }
            else
            {
                this.server.Responses.SomethingWentWrong(attacker);
            }
        }

        public void EndBattle(Client client)
        {
            BattleInfo battle;
            bool removed = this.server.Battles.TryRemove(client.BattleId, out battle);

            if (!removed) return;

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

            battle.Attacker.BattleId = Guid.Empty;
            battle.Defender.BattleId = Guid.Empty;
        }

        public void EndBattle(BattleInfo battle)
        {
            bool ended = this.server.Battles.TryRemove(battle.Id, out battle);
            if (!ended) return;

            this.server.Writer.SendTo(battle.Attacker, Message.Create(Service.BattleEnd, "Battle terminated"));
            this.server.Writer.SendTo(battle.Defender, Message.Create(Service.BattleEnd, "Battle terminated"));
            battle.Attacker.BattleId = Guid.Empty;
            battle.Defender.BattleId = Guid.Empty;
        }

        public void UpdateResources(Client client, Message message)
        {
            if (client.Disposed) return;

            if (!this.server.Players.ContainsKey(client.Id))
            {
                this.server.Responses.MustBeLoggedIn(client);
                return;
            }

            var resourceSet = ((Message<ResourceSetDTO>)message).Data;
            this.UpdateResourceSet(client, resourceSet);
        }

        public void UpdateEntities(Client client, Message message)
        {
            if (client.Disposed || client.BattleId == Guid.Empty) return;

            var entities = ((Message<ICollection<EntityDTO>>)message).Data;
            BattleInfo battle;
            bool gotIt = this.server.Battles.TryGetValue(client.BattleId, out battle);

            if (gotIt)
            {
                battle.LastUpdate = DateTime.Now;
                this.server.Writer.SendTo(
                    battle.Attacker.Id == client.Id
                    ? battle.Defender : battle.Attacker,
                    message);
            }

            foreach (var entity in entities)
            {
                if (entity is UnitDTO)
                {
                    this.UpdateUnit(client, this.server.Players[client.Id].UnitsMap[entity.Id], (UnitDTO)entity);
                }
                else if(entity is ResourceProviderDTO)
                {
                    this.UpdateResourceProvider(client, this.server.Players[client.Id].ResProvMap[entity.Id], (ResourceProviderDTO)entity);
                }
            }
        }

        public void AddEntity(Client client, Message message)
        {
            if (client.Disposed) return;

            if(!this.server.Players.ContainsKey(client.Id))
            {
                this.server.Responses.MustBeLoggedIn(client);
                return;
            }

            var entity = ((Message<EntityDTO>)message).Data;
            var player = this.server.Players[client.Id];
            if (entity is UnitDTO)
            {
                var unit = (UnitDTO)entity;
                player.Units.Add(unit);
                player.UnitsMap.Add(unit.Id, unit);
            }
            else if (entity is ResourceProviderDTO)
            {
                var resProv = (ResourceProviderDTO)entity;
                player.ResourceProviders.Add(resProv);
                player.ResProvMap.Add(resProv.Id, resProv);
            }
        }

        private void UpdateResourceSet(Client client, ResourceSetDTO changedResSet)
        {
            var player = this.server.Players[client.Id];

            // ugly as fuck
            player.ResourceSet.Food.Quantity = changedResSet.Food.Quantity;
            player.ResourceSet.Gold.Quantity = changedResSet.Gold.Quantity;
            player.ResourceSet.Wood.Quantity = changedResSet.Wood.Quantity;
            player.ResourceSet.Metal.Quantity = changedResSet.Metal.Quantity;
            player.ResourceSet.Rock.Quantity = changedResSet.Rock.Quantity;
            player.ResourceSet.Population.Quantity = changedResSet.Population.Quantity;
        }

        private void UpdateResourceProvider(Client client, ResourceProviderDTO resProv, ResourceProviderDTO changedResProv)
        {
            try
            {
                resProv.Quantity = changedResProv.Quantity;
                if (resProv.Depleted)
                {
                    this.server.Players[client.Id].ResourceProviders.Remove(resProv);
                    this.server.Players[client.Id].ResProvMap.Remove(resProv.Id);
                    return;
                }

                resProv.PosX = changedResProv.PosX;
                resProv.PosY = changedResProv.PosY;
                resProv.PosZ = changedResProv.PosZ;

                resProv.RotX = changedResProv.RotX;
                resProv.RotY = changedResProv.RotY;
                resProv.RotZ = changedResProv.RotZ;
            }
            catch
            {
            }
        }

        private void UpdateUnit(Client client, UnitDTO unit, UnitDTO changedUnit)
        {
            try
            {              
                unit.Health = changedUnit.Health;

                if (!unit.IsAlive)
                {
                    this.server.Players[client.Id].Units.Remove(unit);
                    this.server.Players[client.Id].UnitsMap.Remove(unit.Id);
                    return;
                }

                unit.PosX = changedUnit.PosX;
                unit.PosY = changedUnit.PosY;
                unit.PosZ = changedUnit.PosZ;

                unit.RotX = changedUnit.RotX;
                unit.RotY = changedUnit.RotY;
                unit.RotZ = changedUnit.RotZ;
            }
            catch
            {
            }
        }
    }
}