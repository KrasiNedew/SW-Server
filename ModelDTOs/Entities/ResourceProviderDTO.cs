namespace ModelDTOs.Entities
{
    using ModelDTOs.Resources;

    public class ResourceProviderDTO : EntityDTO
    {
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
            string type,
            PlayerDTO owner)
            : base(posX, posY, posZ, rotX, rotY, rotZ, scale, weight, type, BaseEntityType.ResourceProvider, owner)
        {
            this.Quantity = quantity;
            this.ResourceType = resourceType;
        }

        public int Quantity { get; set; }

        public ResourceType ResourceType { get; set; }
    }
}