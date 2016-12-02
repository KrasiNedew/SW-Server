namespace ServerUtils
{
    using System.Collections.Generic;

    using ServerUtils.Wrappers;

    public class BattlesManager
    {
        private readonly Dictionary<string, BattleInfo> battles;

        public BattlesManager()
        {
            this.battles = new Dictionary<string, BattleInfo>();
        }

        public bool Exists(BattleInfo battleInfo)
        {
            return battleInfo != null && this.battles.ContainsKey(battleInfo.Attacker.User.Username) && this.battles.ContainsKey(battleInfo.Defender.User.Username);
        }

        public bool TryAdd(BattleInfo battleInfo)
        {
            if (battleInfo == null
                || this.Exists(battleInfo)) return false;

            this.battles.Add(battleInfo.Attacker.User.Username, battleInfo);
            this.battles.Add(battleInfo.Defender.User.Username, battleInfo);
            return true;
        }

        public bool TryEnd(BattleInfo battleInfo)
        {
            if (battleInfo == null 
                || !this.Exists(battleInfo)) return false;

            this.battles.Remove(battleInfo.Attacker.User.Username);
            this.battles.Remove(battleInfo.Defender.User.Username);
            return true;
        }

        public BattleInfo GetByUsername(string username)
        {
            return !this.battles.ContainsKey(username) ? null : this.battles[username];
        }
    }
}