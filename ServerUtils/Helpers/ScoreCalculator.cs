namespace ServerUtils.Helpers
{
    using ModelDTOs;

    public class ScoreCalculator
    {
        // positive -> attacker had more units at the beggining of the battle
        public readonly int InitialUnitDiff;

        public ScoreCalculator(PlayerDTO attacker, PlayerDTO defender)
        {
            this.InitialUnitDiff = attacker.Units.Count - defender.Units.Count;
        }

        // 1 is for attacker -1 for defender 0 for undetermined
        public int CalculateWinner(PlayerDTO attacker, PlayerDTO defender)
        {
            var diff = attacker.Units.Count - defender.Units.Count;

            if (this.InitialUnitDiff > 0)
            {
                if (diff > 0)
                {
                    return 1;
                }

                return -1;
            }

            if (this.InitialUnitDiff < 0)
            {
                if (diff > 0)
                {
                    return 1;
                }

                return -1;
            }

            if (diff > 0) return 1;
            if (diff < 0) return -1;
            return 0;
        }
    }
}