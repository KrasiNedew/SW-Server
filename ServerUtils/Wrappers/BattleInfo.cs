namespace ServerUtils.Wrappers
{
    using System;
    using ModelDTOs;
    using ServerUtils.Helpers;

    public class BattleInfo
    {
        public BattleInfo(Client attacker, Client defender, PlayerDTO attackerDTO, PlayerDTO defenderDTO)
        {
            this.Attacker = attacker;
            this.Defender = defender;
            this.AttackerDTO = attackerDTO;
            this.DefenderDTO = defenderDTO;

            this.Score = new ScoreCalculator(attackerDTO, defenderDTO);
            this.LastUpdate = DateTime.Now;
        }

        public readonly Client Attacker;

        public readonly Client Defender;

        public readonly PlayerDTO AttackerDTO;

        public readonly PlayerDTO DefenderDTO;

        public readonly ScoreCalculator Score;

        public DateTime LastUpdate { get; set; }
    }
}