using System.Linq;
using Verse;

namespace BigAndSmall.SimpleCustomRaces
{
    public static class ThoughtDefPatcher
    {
        public static void PatchDefs()
        {
            
            foreach (var hediff in DefDatabase<HediffDef>.AllDefs)
            {
                foreach (var modExt in hediff.ExtensionsOnDef<PawnExtension, HediffDef>().Where(x=>x.nullsThoughts != null))
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
                    foreach (var modExt in gene.ExtensionsOnDef<PawnExtension, GeneDef>().Where(x => x.nullsThoughts != null))
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
