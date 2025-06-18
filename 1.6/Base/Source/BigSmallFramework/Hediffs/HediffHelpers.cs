using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
   public static class HediffHelpers
    {
        public static bool TryAddToAllMatchingParts(this HediffDef hediffDef, Pawn pawn, List<BodyPartDef> targetPart, IEnumerable<BodyPartRecord> partsToConsider)
        {
            var allMatchingParts = partsToConsider
                .Where(x => targetPart.Contains(x.def) && !pawn.health.hediffSet.HasHediff(hediffDef, x));
            foreach (var part in allMatchingParts)
            {
                pawn.health.AddHediff(hediffDef, part);
            }
            return allMatchingParts.Any();
        }

        public static bool TryRemoveAllOfType(this HediffDef hediffDef, Pawn pawn)
        {
            var allMatchingHediffs = pawn.health.hediffSet.hediffs.Where(x => x.def == hediffDef);
            for (int i = allMatchingHediffs.Count() - 1; i >= 0; i--)
            {
                pawn.health.RemoveHediff(allMatchingHediffs.ElementAt(i));
            }
            return allMatchingHediffs.Any();
        }
    }
}
