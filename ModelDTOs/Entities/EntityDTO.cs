namespace ModelDTOs.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

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
            PlayerDTO owner)
        {
            this.Id = Guid.NewGuid();
            this.PosX = posX;
            this.PosY = posY;
            this.PosZ = posZ;
            this.RotX = rotX;
            this.RotY = rotY;
            this.RotZ = rotZ;
            this.Scale = scale;
            this.Weight = weight;
            this.Owner = owner;
        }

        [ProtoMember(1)]
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; private set; }

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

        [ProtoMember(10)]
        public Guid OwnerId { get; private set; }

        public virtual PlayerDTO Owner { get; private set; }
    }
}