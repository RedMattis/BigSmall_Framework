using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    // Postfix JobDriver_Lovin to add an additional toil to MakeNewToils()
    // This toil will be used to have vampires bite their lovers sometimes.
    [HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
    public static class JobDriver_Lovin_MakeNewToils
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_Lovin __instance, TargetIndex ___PartnerInd)
        {
            try
            {
                var pawn = __instance.pawn;
                var partner = (Pawn)__instance.job.GetTarget(___PartnerInd);
                if (pawn != null && partner != null && !partner.IsBloodfeeder())
                {
                    // , "VU_WhiteRoseBite", "VU_SuccubusBloodFeeder"
                    var activationGenes = GeneHelpers.GetActiveGenesByNames(pawn, ["VU_VampireLover"]);
                    if (activationGenes.Count > 0)
                    {
                        //var biteAbilities = pawn.abilities.AllAbilitiesForReading.Where(x => x.def.defName == "VU_WhiteRoseBite" || x.def.defName == "Bloodfeed");
                        var biteAbilities = pawn.abilities.AllAbilitiesForReading.Where(x => x.comps != null && x.comps.Where(y => y is CompAbilityEffect_BloodfeederBite).Count() > 0);
                        var succubusGenes = GeneHelpers.GetActiveGenesByName(pawn, "VU_LethalLover");
                        float hemogenTriggerLevel = 0.55f;
                        if (succubusGenes.Count > 0)
                            hemogenTriggerLevel = 0.75f;

                        // Check if the pawn has hemogen
                        if (biteAbilities.Count() > 0)
                        {
                            var lastToil = __result.Last();

                            var bite = biteAbilities.Last();

                            var newToil = ToilMaker.MakeToil("Post-sleep bite");
                            newToil.FailOn(() => partner.IsBloodfeeder()); // Replace this with the vampire not being hungry.
                                                                           //Log.Message("Adding Finish Actions to last toil.");
                            newToil.AddFinishAction(delegate
                            {
                                Gene_Hemogen gene_Hemogen = pawn.genes?.GetFirstGeneOfType<Gene_Hemogen>();
                                if (gene_Hemogen.Value < hemogenTriggerLevel)
                                {
                                    foreach (var c in bite.EffectComps)
                                    {
                                        c.Apply(partner, pawn);
                                        Messages.Message(new Message($"{pawn.Name} fed from their partner {partner.Name}.", MessageTypeDefOf.NegativeHealthEvent));
                                        //Log.Message($"{pawn.Name} bit {partner.Name}.");
                                    }
                                }
                            });
                            __result = __result.AddItem(newToil);

                            //yield return newToil;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"{e.Message}\n{e.StackTrace}");
            }
            foreach (var toil in __result)
            {
                yield return toil;
            }

        }
    }
}
