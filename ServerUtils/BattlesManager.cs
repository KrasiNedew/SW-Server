namespace ServerUtils
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using ServerUtils.Wrappers;

    public class BattlesManager
    {
        private readonly ConcurrentDictionary<string, BattleInfo> battles;

        public BattlesManager()
        {
            this.battles = new ConcurrentDictionary<string, BattleInfo>();
        }

        public bool Exists(BattleInfo battleInfo)
        {
            return battleInfo != null && this.battles.ContainsKey(battleInfo.Attacker.User.Username) && this.battles.ContainsKey(battleInfo.Defender.User.Username);
        }

        public bool TryAdd(BattleInfo battleInfo)
        {
            if (battleInfo == null
                || this.Exists(battleInfo)) return false;

            this.battles.TryAdd(battleInfo.Attacker.User.Username, battleInfo);
            this.battles.TryAdd(battleInfo.Defender.User.Username, battleInfo);
            return true;
        }

        public bool TryEnd(BattleInfo battleInfo)
        {
            if (battleInfo == null 
                || !this.Exists(battleInfo)) return false;

            BattleInfo removed;
            this.battles.TryRemove(battleInfo.Attacker.User.Username, out removed);
            this.battles.TryRemove(battleInfo.Defender.User.Username, out removed);
            return true;
        }

        public bool Any()
        {
            return this.battles.Any();
        }

        public IEnumerable<BattleInfo> GetAll()
        {
            return this.battles.Values;
        }

        public BattleInfo GetByUsername(string username)
        {
            BattleInfo battle;
            bool took = this.battles.TryGetValue(username, out battle);
            if (took)
            {
                return battle;
            }

            return null;
        }
    }
}