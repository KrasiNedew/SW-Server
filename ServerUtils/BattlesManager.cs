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

        private bool Exists(BattleIdentifier identifier)
        {
            return this.battles.ContainsKey(identifier);
        }

        private bool TryAdd(BattleIdentifier identifier, BattleInfo battleInfo)
        {
            if (this.Exists(identifier)) return false;

            this.battles.Add(identifier, battleInfo);
            return true;
        }

        public bool TryEnd(BattleIdentifier identifier, BattleInfo battleInfo)
        {
            if (this.Exists(identifier)) return false;

            this.battles[identifier].Dispose();
            this.battles.Remove(identifier);
            return true;
        }

        public bool TryStart(BattleIdentifier identifier, Client attacker, Client defender)
        {
            if (attacker?.User == null 
                || !attacker.User.LoggedIn 
                || attacker.User.Id == 0)
                return false;

            if (defender?.User != null 
                && defender.User.LoggedIn 
                && defender.User.Id != 0)
            {
                var battleInfo = new BattleInfo(attacker, defender);
                bool started = this.TryAdd(identifier, battleInfo);
                return started;
            }           

            return false;
        }

        public BattleInfo GetByIdentifier(BattleIdentifier identifier)
        {
            if (!this.battles.ContainsKey(identifier)) return null;

            var info = this.battles[identifier];
            if (info.Identifier.Equals(identifier))
            {
                return info;
            }

            return null;
        }
    }
}