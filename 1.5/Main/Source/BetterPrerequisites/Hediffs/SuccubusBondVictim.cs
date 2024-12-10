using RimWorld;
using Verse;

namespace BigAndSmall
{
    public class SuccubusBondVictim : Hediff
    {
        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(1000))
            {
                // Check if psychic sensitivity is at or above 150%
                var sensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity, true);

                // Check if the pawn has a Psylink level above 5
                int psylinkLevel = pawn.GetPsylinkLevel();

                // If both are true set the severity to 2. Otherwise set it to 1.
                if (sensitivity >= 1.5f && psylinkLevel >= 5)
                {
                    Severity = 2f;
                }
                else
                {
                    Severity = 1f;
                }
            }
        }
    }
}
