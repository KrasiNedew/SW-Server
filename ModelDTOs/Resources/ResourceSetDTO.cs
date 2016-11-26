namespace ModelDTOs.Resources
{
    using Newtonsoft.Json;

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

        public virtual ResourceDTO Gold { get; private set; }

        public virtual ResourceDTO Wood { get; private set; }

        public virtual ResourceDTO Food { get; private set; }

        public virtual ResourceDTO Rock { get; private set; }

        public virtual ResourceDTO Metal { get; private set; }

        public virtual ResourceDTO Population { get; private set; }

        public int PlayerId { get; private set; }

        [JsonIgnore]
        public virtual PlayerDTO Player { get; private set; }
    }
}