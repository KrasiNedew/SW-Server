namespace Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;

    using ModelDTOs;
    using ModelDTOs.Entities;
    using ModelDTOs.Resources;

    using ServerUtils;

    using Z.EntityFramework.Plus;

    public static class GameServices
    {
        public static void UpdateFull(Client client, PlayerDTO player)
        {
            if (client.Disposed 
                || !AuthenticationServices.Users.IsLoggedIn(client.User) 
                || client.User.Id != player.Id
                || client.User.Username != player.Username)
            {
                Responses.DataNotSaved(client);
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
                Responses.DataNotSaved(client);
            }
        }

        public static void UpdateResources(Client client, ResourceSetDTO resourceSet)
        {
            if (client.Disposed 
                || !AuthenticationServices.Users.IsLoggedIn(client.User)
                || client.User.Id != resourceSet.Id)
            {
                Responses.DataNotSaved(client);
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
                Responses.DataNotSaved(client);
            }
        }

        public static void UpdateEntities(
            Client client, ICollection<UnitDTO> entities)
        {
            if (client.Disposed 
                || !AuthenticationServices.Users.IsLoggedIn(client.User)
                || entities.Any(e => e.Id != client.User.Id))
            {
                Responses.DataNotSaved(client);
                return;
            }

            var map = entities.ToDictionary(e => e.Id, e => e);

            try
            {
                using (SimpleWarsContext context = new SimpleWarsContext())
                {
                    var filtered = context.Units.Where(u => map.ContainsKey(u.Id)).ToDictionary(u => u.Id, u => u);

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
                Responses.DataNotSaved(client);
            }
        }

        public static void UpdateEntities(
            Client client, ICollection<ResourceProviderDTO> entities)
        {
            if (client.Disposed 
                || !AuthenticationServices.Users.IsLoggedIn(client.User)
                || entities.Any(e => e.Id != client.User.Id))
            {
                Responses.DataNotSaved(client);
                return;
            }

            var map = entities.ToDictionary(e => e.Id, e => e);

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
                Responses.DataNotSaved(client);
            }
        }
    }
}