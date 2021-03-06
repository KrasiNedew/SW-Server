﻿namespace ModelDTOs.Resources
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using ProtoBuf;

    [ProtoContract]
    [Table("ResourceSets")]
    public class ResourceSetDTO
    {
        protected ResourceSetDTO()
        {
        }

        public ResourceSetDTO(PlayerDTO associatedPlayer)
        {
            this.Gold = new ResourceDTO(0, ResourceType.Gold);
            this.Wood = new ResourceDTO(0, ResourceType.Wood);
            this.Food = new ResourceDTO(0, ResourceType.Food);
            this.Rock = new ResourceDTO(0, ResourceType.Rock);
            this.Metal = new ResourceDTO(0, ResourceType.Metal);
            this.Population = new ResourceDTO(0, ResourceType.Population);

            this.Id = associatedPlayer.Id;
            this.Player = associatedPlayer;            
        }

        [ProtoMember(7)]
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None), ForeignKey("Player")]
        public Guid Id { get; set; }

        [ProtoMember(1)]
        public virtual ResourceDTO Gold { get; private set; }

        [ProtoMember(2)]
        public virtual ResourceDTO Wood { get; private set; }

        [ProtoMember(3)]
        public virtual ResourceDTO Food { get; private set; }

        [ProtoMember(4)]
        public virtual ResourceDTO Rock { get; private set; }

        [ProtoMember(5)]
        public virtual ResourceDTO Metal { get; private set; }

        [ProtoMember(6)]
        public virtual ResourceDTO Population { get; private set; }

        public virtual PlayerDTO Player { get; private set; }
    }
}