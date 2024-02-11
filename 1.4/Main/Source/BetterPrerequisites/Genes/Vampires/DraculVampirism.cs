using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static System.Net.Mime.MediaTypeNames;

namespace BigAndSmall
{
    public class DraculStageExtension : DefModExtension
    {
        public int draculStage;
        public int durationDays;
        
        public static (int stage, Gene draculGene) TryGetDraculStage(Pawn pawn)
        {
            var draculGene = Helpers.GetAllActiveGenes(pawn).Where(x => x.def.HasModExtension<DraculStageExtension>());
            if (draculGene.Count() == 1)
            {
                try
                {
                    int stage = draculGene.First().def.GetModExtension<DraculStageExtension>().draculStage;
                    return (stage, draculGene.First());
                }
                catch
                {
                    return (3, null);
                }
            }
            else
            {
                //Log.Warning($"Pawn {pawn.Name} either has none or has more than one Dracul Gene. Defaulting to Stage 3");
                return (3, null);
            }
        }
    }
    
    public class DraculStage : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            // Get Comps and check level.
            int draculStage = def.GetModExtension<DraculStageExtension>().draculStage;
            int days = def.GetModExtension<DraculStageExtension>().durationDays;
            if (draculStage > 3)
            {
                return;
            }

            // Create DraculStageProgression Hediff is one does not already exist.
            if (pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_DraculAge) == null)
            {
                var draculStageProgression = (DraculStageProgression)HediffMaker.MakeHediff(BSDefs.VU_DraculAge, pawn);
                int ticksPerDay = 60000;
                int durationTicks = days * ticksPerDay;
                //Log.Message($"Setting progression duration based on Dracul Stage {draculStage}. Result is {durationTicks}");

                //// DEBUG! REMOVE THIS!
                //durationTicks = 10000;

                // Get the comps in the hediff and set the HediffCompProperties_Disappears disappars to the duration
                foreach (var comp in draculStageProgression.comps)
                {
                    if (comp is HediffComp_Disappears disappears)
                    {
                        disappears.ticksToDisappear = durationTicks;
                    }
                }
                
                pawn.health.AddHediff(draculStageProgression);
            }
        }
    }

    

}
