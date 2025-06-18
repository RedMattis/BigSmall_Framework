using Verse;

namespace BigAndSmall
{
    internal class TrulyAgeless : TickdownGene
    {
        const int ticksPerYear = 3600000;
        public override void ResetCountdown()
        {
            tickDown = 500;
        }

        public override void TickEvent()
        {
            if (pawn?.ageTracker?.AgeBiologicalYears != null && pawn.IsHashIntervalTick(500) && pawn.ageTracker.AgeBiologicalYears > 25)
            {
                pawn.ageTracker.AgeBiologicalTicks = 25 * ticksPerYear;
            }
        }
    }
}
