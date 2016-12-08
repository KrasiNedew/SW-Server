namespace ModelDTOs.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    using ProtoBuf;

    [ProtoContract]
    [Table("Units")]
    public class UnitDTO : EntityDTO
    {
        public enum UnitType
        {
            Swordsman
        }

        protected UnitDTO()
        {
        }

        public UnitDTO(
            int health,
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
            this.Health = health;
        }

        public bool IsAlive => this.Health > 0;

        [ProtoMember(14)]
        public int Health { get; set; }

        [ProtoMember(15)]
        public UnitType Type { get; set; }
    }
}