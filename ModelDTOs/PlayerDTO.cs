namespace ModelDTOs
{
    using System.Collections.Generic;

    using ModelDTOs.Entities;
    using ModelDTOs.Resources;

    public class PlayerDTO
    {
        protected PlayerDTO()
        {
            this.ResourceProviders = new HashSet<ResourceProviderDTO>();
            this.Units = new HashSet<UnitDTO>();
        }

        public PlayerDTO(string username, string passwordHash, int worldSeed, int worldX, int worldY)
        {
            this.Username = username;
            this.PasswordHash = passwordHash;
            this.WorldSeed = worldSeed;
            this.WorldX = worldX;
            this.WorldY = worldY;
            this.ResourceSet = new ResourceSetDTO(this);
            this.Units = new HashSet<UnitDTO>();
            this.ResourceProviders = new HashSet<ResourceProviderDTO>();
        }

        public int Id { get; set; }

        public string Username { get; set; }

        public string PasswordHash { get; set; }

        public int WorldSeed { get; set; }

        public float WorldX { get; set; }

        public float WorldY { get; set; }

        public bool LoggedIn { get; set; }

        public virtual ResourceSetDTO ResourceSet { get; private set; }

        public virtual ICollection<ResourceProviderDTO> ResourceProviders { get; private set; }

        public virtual ICollection<UnitDTO> Units { get; private set; }
    }
}