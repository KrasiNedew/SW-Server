namespace ServerUtils.Wrappers
{
    public class BattleIdentifier
    {
        public readonly string AttackerUsername;

        public readonly string DefenderUsername;

        public readonly string Identifier;

        public BattleIdentifier(string attackerUsername, string defenderUsername)
        {
            this.AttackerUsername = attackerUsername;
            this.DefenderUsername = defenderUsername;
            this.Identifier = string.Concat(this.AttackerUsername, this.DefenderUsername);
        }

        public override bool Equals(object obj)
        {
            var other = obj as BattleIdentifier;

            if (other == null) return false;

            return this.Identifier == other.Identifier;
        }

        public override int GetHashCode()
        {
            return this.Identifier.GetHashCode();
        }

        public static BattleIdentifier Create(string username1, string username2)
        {
            return new BattleIdentifier(username1, username2);
        }
    }
}