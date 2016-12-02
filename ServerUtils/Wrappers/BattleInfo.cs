namespace ServerUtils.Wrappers
{
    using System;
    using System.Linq;
    using Data;

    using ModelDTOs;

    public class BattleInfo
    {
        public BattleInfo(Client attacker, Client defender, PlayerDTO attackerDTO, PlayerDTO defenderDTO)
        {
            this.Attacker = attacker;
            this.Defender = defender;
            this.AttackerDTO = attackerDTO;
            this.DefenderDTO = defenderDTO;
        }

        public readonly Client Attacker;

        public readonly Client Defender;

        public PlayerDTO AttackerDTO { get; set; }

        public PlayerDTO DefenderDTO { get; set; }

        public static BattleInfo Create(Client attacker, Client defender, PlayerDTO attackerDTO, PlayerDTO defenderDTO)
        {
            return new BattleInfo(attacker, defender, attackerDTO, defenderDTO);
        }
    }
}