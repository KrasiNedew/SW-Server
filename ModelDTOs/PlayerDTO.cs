namespace ModelDTOs
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    using ModelDTOs.Entities;
    using ModelDTOs.Resources;

    using ProtoBuf;

    [ProtoContract]
    [Table("Players")]
    public class PlayerDTO
    {
        protected PlayerDTO()
        {
            this.ResourceProviders = new HashSet<ResourceProviderDTO>();
            this.Units = new HashSet<UnitDTO>();
        }

        public PlayerDTO(string username, string passwordHash, int worldSeed)
        {
            this.Username = username;
            this.PasswordHash = passwordHash;
            this.WorldSeed = worldSeed;
            this.ResourceSet = new ResourceSetDTO(this);
            this.Units = new HashSet<UnitDTO>();
            this.ResourceProviders = new HashSet<ResourceProviderDTO>();
        }

        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Username { get; private set; }

        public string PasswordHash { get; set; }

        [ProtoMember(4)]
        public int WorldSeed { get; set; }

        [ProtoMember(7)]
        public bool LoggedIn { get; set; }

        [ProtoMember(8)]
        public bool UnderAttack { get; set; }

        [ProtoMember(14)]
        public virtual ResourceSetDTO ResourceSet { get; private set; }

        [ProtoMember(15)]
        public virtual ICollection<ResourceProviderDTO> ResourceProviders { get; private set; }

        [ProtoMember(16)]
        public virtual ICollection<UnitDTO> Units { get; private set; }

        public void ChangeAttackState(bool underAttack)
        {
            this.UnderAttack = underAttack;
        }
    }
}