namespace ModelDTOs.Entities
{
    using Newtonsoft.Json;

    public abstract class EntityDTO
    {
        protected EntityDTO()
        {
        }

        public EntityDTO(
            float posX,
            float posY,
            float posZ,
            float rotX,
            float rotY,
            float rotZ,
            float scale,
            float weight,
            string concreteType,
            BaseEntityType baseType,
            PlayerDTO owner)
        {
            this.PosX = posX;
            this.PosY = posY;
            this.PosZ = posZ;
            this.RotX = rotX;
            this.RotY = rotY;
            this.RotZ = rotZ;
            this.Scale = scale;
            this.Weight = weight;
            this.ConcreteType = concreteType;
            this.BaseType = baseType;
            this.Owner = owner;
            this.OwnerId = owner.Id;
        }

        public int Id { get; set; }

        public float PosX { get; set; }

        public float PosY { get; set; }

        public float PosZ { get; set; }

        public float RotX { get; set; }

        public float RotY { get; set; }

        public float RotZ { get; set; }

        public float Scale { get; set; }

        public float Weight { get; set; }

        public string ConcreteType { get; set; }

        public BaseEntityType BaseType { get; set; }

        public int OwnerId { get; private set; }

        [JsonIgnore]
        public virtual PlayerDTO Owner { get; private set; }
    }
}