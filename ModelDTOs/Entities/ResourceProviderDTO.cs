namespace ModelDTOs.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    using ModelDTOs.Resources;

    using ProtoBuf;

    [ProtoContract]
    [Table("ResourceProviders")]
    public class ResourceProviderDTO : EntityDTO
    {
        public enum ProviderType
        {
            Tree
        }

        protected ResourceProviderDTO()
        {
        }

        public ResourceProviderDTO(
            int quantity,
            ResourceType resourceType,
            float posX,
            float posY,
            float posZ,
            float rotX,
            float rotY,
            float rotZ,
            float scale,
            float weight,
            PlayerDTO owner)
            : base(posX, posY, posZ, rotX, rotY, rotZ, scale, weight, owner)
        {
            this.Quantity = quantity;
            this.ResourceType = resourceType;
        }

        [ProtoMember(13)]
        public int Quantity { get; set; }

        [ProtoMember(14)]
        public ResourceType ResourceType { get; set; }

        public bool Depleted => this.Quantity > 0;

        [ProtoMember(16)]
        public ProviderType Type { get; set; }
    }
}