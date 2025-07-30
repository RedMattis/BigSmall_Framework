using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class LockedNeed
    {
        public static void UpdateLockedNeeds(Gene gene)
        {
            var geneExt = gene.def.ExtensionsOnDef<PawnExtension, GeneDef>();
            if (geneExt.Any(x => x.lockedNeeds != null && x.lockedNeeds.Any(x => x.need != null)))
            {
                foreach (var lockedNeed in geneExt.Where(x => x.lockedNeeds != null)
                            .SelectMany(x => x.lockedNeeds).Where(x => x.need != null))
                {
                    float value = lockedNeed.value;
                    bool minValue = lockedNeed.minValue;
                    NeedDef needDef = lockedNeed.need;

                    var need = gene.pawn?.needs?.TryGetNeed(needDef);

                    if (need != null)
                    {
                        if (minValue)
                        {
                            if (need.CurLevelPercentage < value)
                            {
                                need.CurLevel = need.MaxLevel * value;
                            }
                        }
                        else
                        {
                            need.CurLevel = need.MaxLevel * value;
                        }
                    }
                }
            }
        }
    }
}
