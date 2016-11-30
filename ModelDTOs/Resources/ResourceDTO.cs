namespace ModelDTOs.Resources
{
    using System.ComponentModel.DataAnnotations.Schema;

    using ProtoBuf;

    [ProtoContract]
    [Table("Resources")]
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

        [ProtoMember(1)]
        public int Id { get; protected set; }

        [ProtoMember(2)]
        public int Quantity { get; set; }

        [ProtoMember(3)]
        public ResourceType ResourceType { get; protected set; } 
    }
}