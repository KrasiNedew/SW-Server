namespace ModelDTOs.Entities
{
    using System;
    using System.Runtime.Serialization;

    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(100, typeof(ResourceProviderDTO))]
    [ProtoInclude(101, typeof(UnitDTO))]
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
            string type,
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
            this.Type = type;
            this.Owner = owner;
        }

        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public float PosX { get; set; }

        [ProtoMember(3)]
        public float PosY { get; set; }

        [ProtoMember(4)]
        public float PosZ { get; set; }

        [ProtoMember(5)]
        public float RotX { get; set; }

        [ProtoMember(6)]
        public float RotY { get; set; }

        [ProtoMember(7)]
        public float RotZ { get; set; }

        [ProtoMember(8)]
        public float Scale { get; set; }

        [ProtoMember(9)]
        public float Weight { get; set; }

        public string Type { get; set; }

        [ProtoMember(10)]
        public int OwnerId { get; private set; }

        public virtual PlayerDTO Owner { get; private set; }
    }
}