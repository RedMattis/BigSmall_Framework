using System.Linq;
using Verse;

namespace BigAndSmall.SimpleCustomRaces
{
    public static class ThoughtDefPatcher
    {
        public static void PatchDefs()
        {
            
            foreach (var hediffDef in DefDatabase<HediffDef>.AllDefs)
            {
                foreach (var modExt in hediffDef.ExtensionsOnDef<PawnExtension, HediffDef>().Where(x=>x.nullsThoughts != null))
                {
                    foreach (var thought in modExt.nullsThoughts)
                    {
                        thought.nullifyingHediffs ??= [];
                        thought.nullifyingHediffs.AddDistinct(hediffDef);
                    }
                }
            }

            if (ModsConfig.BiotechActive)
            {
                foreach (var geneDef in DefDatabase<GeneDef>.AllDefs)
                {
                    foreach (var modExt in geneDef.ExtensionsOnDef<PawnExtension, GeneDef>().Where(x => x.nullsThoughts != null))
                    {
                        foreach (var thought in modExt.nullsThoughts)
                        {
                            thought.nullifyingGenes ??= [];
                            thought.nullifyingGenes.AddDistinct(geneDef);
                            //Log.Message($"DEBUG: Added {gene.defName} to {thought.defName}'s nullifying genes.");
                        }
                    }
                }
            }
        }
    }
}
