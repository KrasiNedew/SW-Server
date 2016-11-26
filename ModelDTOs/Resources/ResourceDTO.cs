namespace ModelDTOs.Resources
{
    public class ResourceDTO
    {
        protected ResourceDTO()
        {
        }

        public ResourceDTO(int quantity, ResourceType resourceType)
        {
            this.Quantity = quantity;
            this.ResourceType = resourceType;
        }

        public int Id { get; set; }

        public int Quantity { get; set; }

        public ResourceType ResourceType { get; protected set; } 
    }
}