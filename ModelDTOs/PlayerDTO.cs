namespace ModelDTOs
{
    using System;
    using System.Collections.Generic;

    using ModelDTOs.Entities;
    using ModelDTOs.Resources;

    using ProtoBuf;

    [ProtoContract]
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

        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Username { get; set; }

        [ProtoMember(3)]
        public string PasswordHash { get; set; }

        [ProtoMember(4)]
        public int WorldSeed { get; set; }

        [ProtoMember(5)]
        public float WorldX { get; set; }

        [ProtoMember(6)]
        public float WorldY { get; set; }

        [ProtoMember(7)]
        public bool LoggedIn { get; set; }

        [ProtoMember(14)]
        public virtual ResourceSetDTO ResourceSet { get; private set; }

        [ProtoMember(15)]
        public virtual ICollection<ResourceProviderDTO> ResourceProviders { get; private set; }

        [ProtoMember(16)]
        public virtual ICollection<UnitDTO> Units { get; private set; }
    }
}