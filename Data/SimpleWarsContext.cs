namespace Data
{
    using System.Data.Entity;

    using Data.Migrations;

    using ModelDTOs;
    using ModelDTOs.Entities;
    using ModelDTOs.Resources;

    public class SimpleWarsContext : DbContext
    {
        public SimpleWarsContext()
            : base("name=SimpleWarsContext")
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<SimpleWarsContext, Configuration>());
        }

        public virtual DbSet<PlayerDTO> Players { get; set; }

        public virtual DbSet<ResourceProviderDTO> ResourceProviders { get; set; }

        public virtual DbSet<UnitDTO> Units { get; set; }

        public virtual DbSet<ResourceSetDTO> ResourceSets { get; set; }

        public virtual DbSet<ResourceDTO> Resources { get; set; }
    }
}