using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    internal class GeneEater : Gene
    {
        public static Thing lastEatenThing = null;
        public static int lastEatenThingTicks = 0;
        public override void Notify_IngestedThing(Thing thing, int numTaken)
        {
            base.Notify_IngestedThing(thing, numTaken);

            // So muching on the same thing doesn't trigger the effect a bunch of times.
            if (lastEatenThing == thing && Find.TickManager.TicksGame - lastEatenThingTicks < 6000)
            {
                return;
            }

            var tPawn = thing as Pawn;
            var cPawn = (thing as Corpse)?.InnerPawn;

            if ( tPawn != null || cPawn != null)
            {
                lastEatenThing = thing;
                Pawn ingestedPawn = tPawn == null ? cPawn : tPawn;

                int numGenes;
                if (Rand.Chance(0.75f))
                {
                    numGenes = Rand.Range(1,2);
                }
                else if (Rand.Chance(0.50f))
                {
                    numGenes = Rand.Range(3, 5);
                }
                else if (Rand.Chance(0.50f))
                {
                    numGenes = Rand.Range(6, 12);
                }
                else
                {
                    numGenes = 99;
                }

                CompProperties_IncorporateEffect.IncorporateGenes(pawn, ingestedPawn, genePickCount: numGenes*2, stealTraits: false, userPicks: false, randomPickCount: numGenes, excludeBodySwap:true);

                // Remove the Herbivore Gene if it exists
                var herbivoreGenes = pawn.genes.GenesListForReading.Where(x => x.def.defName.Contains("BS_Diet_Herbivore"));
                foreach (var gene in herbivoreGenes)
                {
                    pawn.genes.RemoveGene(gene);
                }
            }
        }
    }
}

//GeneEater