namespace ModelDTOs.Entities
{
    public abstract class UnitDTO : EntityDTO
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
            : base(posX, posY, posZ, rotX, rotY, rotZ, scale, weight, type, BaseEntityType.Unit, owner)
        {
            this.IsAlive = isAlive;
            this.Health = health;
        }

        public bool IsAlive { get; set; }

        public int Health { get; set; }

    }
}