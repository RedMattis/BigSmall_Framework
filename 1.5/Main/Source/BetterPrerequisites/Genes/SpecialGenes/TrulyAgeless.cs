using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    internal class TrulyAgeless : Gene
    {
        public override void Tick()
        {
            const int ticksPerYear = 3600000;
            if (pawn.IsHashIntervalTick(500) && pawn.ageTracker.AgeBiologicalYears > 25)
            {
                pawn.ageTracker.AgeBiologicalTicks = 25 * ticksPerYear;
            }
        }
    }
}
