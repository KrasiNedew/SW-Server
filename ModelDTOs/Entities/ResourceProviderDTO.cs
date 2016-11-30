namespace ModelDTOs.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    using ModelDTOs.Resources;

    using ProtoBuf;

    [ProtoContract]
    [Table("ResourceProviders")]
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
            : base(posX, posY, posZ, rotX, rotY, rotZ, scale, weight, type, owner)
        {
            this.Quantity = quantity;
            this.ResourceType = resourceType;
        }

        [ProtoMember(13)]
        public int Quantity { get; set; }

        [ProtoMember(14)]
        public ResourceType ResourceType { get; set; }
    }
}