using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class LovinPatches
    {
        public static IEnumerable<Toil> VEHighmates_Lovin(IEnumerable<Toil> __result, JobDriver __instance)
        {
            try
            {
                var pawn = __instance.pawn;
                var partner = (Pawn)__instance.job.GetTarget(TargetIndex.A);
                if (pawn != null && partner != null)
                {
                    __result = LovinSoulfeed(__result, pawn, partner);
                    if (!partner.IsBloodfeeder())
                        __result = LovinBloodFeed(__result, pawn, partner);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"{e.Message}\n{e.StackTrace}");
            }
            foreach (var toil in __result)
                yield return toil;
        }

        [HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
        [HarmonyPostfix]
        public static IEnumerable<Toil> JobDriver_Lovin(IEnumerable<Toil> __result, JobDriver_Lovin __instance, TargetIndex ___PartnerInd)
        {
            try
            {
                var pawn = __instance.pawn;
                var partner = (Pawn)__instance.job.GetTarget(___PartnerInd);
                if (pawn != null && partner != null)
                {
                    __result = LovinSoulfeed(__result, pawn, partner);
                    if (!partner.IsBloodfeeder())
                        __result = LovinBloodFeed(__result, pawn, partner);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"{e.Message}\n{e.StackTrace}");
            }
            foreach (var toil in __result)
                yield return toil;
        }

        public static void SiphonAction(Pawn initiator, Pawn target)
        {
            if (target == null || initiator == null)
                return;
            var pawnExts = initiator.GetAllPawnExtensions();
            var siphons = pawnExts
                .Select(x => x.siphonSoul)
                .Where(x => x != null && x.type == SiphonType.Lovin);
            if (siphons.Any())
            {
                var fused = siphons.FuseAll(SiphonType.Lovin);
                SoulCollector soulCollector = Soul.GetOrAddSoulCollector(initiator);
                float amount = soulCollector.AddPawnSoul(target, fused, verbose: false);
                Messages.Message(new Message($"BS_LovinSoulFeed".Translate(initiator.NameShortColored, target.NameShortColored, $"{amount*100:f1}%"), MessageTypeDefOf.NeutralEvent));
            }
        }

        public static IEnumerable<Toil> LovinSoulfeed(IEnumerable<Toil> __result, Pawn initiator, Pawn target)
        {
            
            var pawnExts = initiator.GetAllPawnExtensions();
            var siphons = pawnExts
                .Select(x => x.siphonSoul)
                .Where(x => x != null && x.type == SiphonType.Lovin);
            if (siphons.Any())
            {
                var lastToil = __result.Last();
                var newToil = ToilMaker.MakeToil("Post-lovin' soul suckin'");
                newToil.AddFinishAction(delegate
                {
                    SiphonAction(initiator, target);
                });
                __result = __result.AddItem(newToil);
            }

            return __result;
        }

        public static IEnumerable<Toil> LovinBloodFeed(IEnumerable<Toil> __result, Pawn pawn, Pawn partner)
        {
            static void Feedin(Pawn pawn, Pawn partner, float hemogenTriggerLevel, Ability bite)
            {
                if (pawn == null || partner == null)
                    return;
                Gene_Hemogen gene_Hemogen = pawn.genes?.GetFirstGeneOfType<Gene_Hemogen>();
                if (gene_Hemogen.Value < hemogenTriggerLevel)
                {
                    foreach (var c in bite.EffectComps)
                    {
                        c.Apply(partner, pawn);
                        Messages.Message(new Message($"BS_LovinnBloodfeed".Translate(pawn.NameShortColored, partner.NameShortColored), MessageTypeDefOf.NegativeHealthEvent));
                    }
                }
            }
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

                    var newToil = ToilMaker.MakeToil("Post-lovin' feedin'");
                    newToil.FailOn(partner.IsBloodfeeder);
                    newToil.AddFinishAction(delegate
                    {
                        Feedin(pawn, partner, hemogenTriggerLevel, bite);                    });
                    __result = __result.AddItem(newToil);
                }
            }

            return __result;
        }
    }
}
