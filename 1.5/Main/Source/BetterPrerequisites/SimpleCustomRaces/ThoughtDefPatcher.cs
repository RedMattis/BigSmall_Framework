using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall.SimpleCustomRaces
{
    public static class ThoughtDefPatcher
    {
        public static void PatchDefs()
        {
            
            foreach (var hediff in DefDatabase<HediffDef>.AllDefs)
            {
                foreach (var modExt in ModExtHelper.ExtensionsOnDef<PawnExtension>(hediff).Where(x=>x.nullsThoughts != null))
                {
                    foreach (var thought in modExt.nullsThoughts)
                    {
                        thought.nullifyingHediffs ??= [];
                        thought.nullifyingHediffs.AddDistinct(hediff);
                        //Log.Message($"DEBUG: Added {hediff.defName} to {thought.defName}'s nullifying hediffs.");
                    }
                }
            }

            if (ModsConfig.BiotechActive)
            {
                foreach (var gene in DefDatabase<GeneDef>.AllDefs)
                {
                    foreach (var modExt in ModExtHelper.ExtensionsOnDef<PawnExtension>(gene).Where(x => x.nullsThoughts != null))
                    {
                        foreach (var thought in modExt.nullsThoughts)
                        {
                            thought.nullifyingGenes ??= [];
                            thought.nullifyingGenes.AddDistinct(gene);
                            //Log.Message($"DEBUG: Added {gene.defName} to {thought.defName}'s nullifying genes.");
                        }
                    }
                }
            }
        }
    }
}
