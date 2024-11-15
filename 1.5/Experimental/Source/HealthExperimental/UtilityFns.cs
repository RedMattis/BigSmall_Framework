using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RedHealth
{
    public static class HealthHelpers
    {
        // Becuse the vanilla method literaslly just checks if ANY part patching the bodyredord DEF has a bionic OR implant.
        // This is a more accurate check.
        public static bool PartIsBionic(Pawn pawn, BodyPartRecord part)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].Part == part && hediffs[i] is Hediff_AddedPart)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
