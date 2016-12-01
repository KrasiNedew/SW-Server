namespace ServerUtils.Wrappers
{
    using System;
    using System.Linq;
    using Data;

    using ModelDTOs;

    public class BattleInfo : IDisposable
    {
        public BattleInfo(Client attacker, Client defender)
        {
            this.Attacker = attacker;
            this.Defender = defender;

            this.Identifier = new BattleIdentifier(this.Attacker.User.Username, this.Defender.User.Username);

            this.Context = new SimpleWarsContext();
            this.AttackerDTO = this.Context.Players.Find(this.Attacker.User.Id);
            this.DefenderDTO = this.Context.Players.Find(this.Defender.User.Id);
        }

        public readonly Client Attacker;

        public readonly Client Defender;

        public readonly SimpleWarsContext Context;

        public readonly BattleIdentifier Identifier;

        public readonly PlayerDTO AttackerDTO;

        public readonly PlayerDTO DefenderDTO;

        public void Dispose()
        {
            try
            {
                this.Context.BulkSaveChanges();
            }
            finally
            {
                this.Context.Dispose();
            }
        }
    }
}