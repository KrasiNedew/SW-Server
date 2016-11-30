namespace ModelDTOs.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    using ProtoBuf;

    [ProtoContract]
    [Table("Units")]
    public class UnitDTO : EntityDTO
    {
        protected UnitDTO()
        {
        }

        public UnitDTO(
            bool isAlive,
            int health,
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
            this.IsAlive = isAlive;
            this.Health = health;
        }

        [ProtoMember(13)]
        public bool IsAlive { get; set; }

        [ProtoMember(14)]
        public int Health { get; set; }

    }
}