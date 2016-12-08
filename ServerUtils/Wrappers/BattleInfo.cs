namespace ServerUtils.Wrappers
{
    using System;

    public class BattleInfo
    {
        public BattleInfo(Client attacker, Client defender)
        {
            this.Attacker = attacker;
            this.Defender = defender;
            this.Id = Guid.NewGuid();

            this.LastUpdate = DateTime.Now;
        }

        public readonly Client Attacker;

        public readonly Client Defender;

        public readonly Guid Id;

        public DateTime LastUpdate { get; set; }
    }
}