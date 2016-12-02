namespace ServerUtils
{
    using System.Collections.Generic;

    using ServerUtils.Wrappers;

    public class BattlesManager
    {
        private readonly Dictionary<BattleIdentifier, BattleInfo> battles;

        public BattlesManager()
        {
            this.battles = new Dictionary<BattleIdentifier, BattleInfo>();
        }

        public bool Exists(BattleInfo battleInfo)
        {
            return battleInfo?.Identifier != null && this.battles.ContainsKey(battleInfo.Identifier);
        }

        public bool TryAdd(BattleInfo battleInfo)
        {
            if (battleInfo?.Identifier == null
                || this.Exists(battleInfo)) return false;

            this.battles.Add(battleInfo.Identifier, battleInfo);
            return true;
        }

        public bool TryEnd(BattleInfo battleInfo)
        {
            if (battleInfo?.Identifier == null 
                || !this.Exists(battleInfo)) return false;

            this.battles.Remove(battleInfo.Identifier);
            return true;
        }

        public BattleInfo GetByIdentifier(BattleIdentifier identifier)
        {
            return !this.battles.ContainsKey(identifier) ? null : this.battles[identifier];
        }
    }
}