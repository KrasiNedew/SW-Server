namespace ModelDTOs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    using Entities;
    using Resources;

    using ProtoBuf;

    [ProtoContract]
    [Table("Players")]
    public class PlayerDTO
    {
        protected PlayerDTO()
        {
            this.ResourceProviders = new HashSet<ResourceProviderDTO>();
            this.Units = new HashSet<UnitDTO>();
            this.UnitsMap = new Dictionary<Guid, UnitDTO>();
            this.ResProvMap = new Dictionary<Guid, ResourceProviderDTO>();
        }

        public PlayerDTO(string username, string passwordHash, int worldSeed)
        {
            this.Id = Guid.NewGuid();
            this.Username = username;
            this.PasswordHash = passwordHash;
            this.WorldSeed = worldSeed;
            this.ResourceSet = new ResourceSetDTO(this);
            this.Units = new HashSet<UnitDTO>();
            this.ResourceProviders = new HashSet<ResourceProviderDTO>();
            this.UnitsMap = new Dictionary<Guid, UnitDTO>();
            this.ResProvMap = new Dictionary<Guid, ResourceProviderDTO>();
        }

        [ProtoMember(1)]
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public string Username { get; private set; }

        public string PasswordHash { get; set; }

        [ProtoMember(4)]
        public int WorldSeed { get; set; }

        [ProtoMember(7)]
        public bool LoggedIn { get; set; }

        [ProtoMember(14)]
        public virtual ResourceSetDTO ResourceSet { get; private set; }

        [ProtoMember(15)]
        public virtual ICollection<ResourceProviderDTO> ResourceProviders { get; private set; }

        [ProtoMember(16)]
        public virtual ICollection<UnitDTO> Units { get; private set; }

        [NotMapped]
        public Dictionary<Guid, UnitDTO> UnitsMap { get; private set; }

        [NotMapped]
        public Dictionary<Guid, ResourceProviderDTO> ResProvMap { get; private set; }

        public void MapEntites()
        {
            this.UnitsMap = this.Units.ToDictionary(u => u.Id, u => u);
            this.ResProvMap = this.ResourceProviders.ToDictionary(rp => rp.Id, rp => rp);
        }
    }
}