namespace ModelDTOs
{
    using System.Collections.Generic;

    using ModelDTOs.Entities;

    using ProtoBuf;

    [ProtoContract]
    public class BattleState
    {
        [ProtoMember(1)]
        public ICollection<UnitDTO> MutatedUnits { get; private set; }

        protected BattleState()
        {
        }

        public BattleState(ICollection<UnitDTO> mutatedUnits)
        {
            this.MutatedUnits = mutatedUnits;
        }

        public static BattleState Create(ICollection<UnitDTO> MutatedUnits)
        {
            return new BattleState(MutatedUnits);
        }
    }
}