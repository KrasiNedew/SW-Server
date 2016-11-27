namespace ModelDTOs.Resources
{
    using Newtonsoft.Json;

    using ProtoBuf;

    [ProtoContract]
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

            this.Player = associatedPlayer;
        }

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

        [ProtoMember(7)]
        public int PlayerId { get; private set; }

        public virtual PlayerDTO Player { get; private set; }
    }
}