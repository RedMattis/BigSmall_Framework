using BetterPrerequisites;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Pawn_SpawnSetup
    {
        public static void Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            if (!respawningAfterLoad)
            {
                float? foodNeed = __instance?.needs?.food?.CurLevelPercentage;
                Pawn_PostMapInit.RefreshPawnGenes(__instance, forceRefresh: true);
                if (foodNeed != null)
                {
                    __instance.needs.food.CurLevelPercentage = foodNeed.Value;
                }

                __instance.def.modExtensions?.OfType<RaceExtension>()?.FirstOrDefault()?.ApplyTrackerIfMissing(__instance);
            }
        }
    }

    // When the game is loaded, go through all hedifs in the pawns health tab and try to add supressors
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostMapInit))]
    public static class Pawn_PostMapInit
    {
        public static void Postfix(Pawn __instance)
        {
            RefreshPawnGenes(__instance, forceRefresh: true);
        }

        public static void RefreshPawnGenes(Pawn __instance, bool forceRefresh = true)
        {
            if (__instance != null)
            {
                foreach (var hediff in __instance.health.hediffSet.hediffs)
                {
                    try
                    {
                        PGene.supressPostfix = true;
                        GeneSuppressorManager.TryAddSuppressor(hediff, __instance);
                    }
                    finally
                    {
                        PGene.supressPostfix = false;
                    }
                }
                GenderMethods.UpdatePawnHairAndHeads(__instance);

                foreach (var gene in GeneHelpers.GetAllActiveGenes(__instance))
                {
                    GeneEffectManager.RefreshGeneEffects(gene, activate: true);
                }
                if (forceRefresh)
                {
                    HumanoidPawnScaler.GetCache(__instance, forceRefresh: true);
                }
            }
            else
            {
                Log.Error("BetterPrerequisites: Someone just called PostMapInit called with null pawn. Probably someone did a whoopsie!");
            }
        }

    }

    //[HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    //public static class SetFaction
    //{
    //    public static void Postfix(Pawn __instance, Faction newFaction)
    //    {
    //        if(__instance?.Drawer != null && HumanoidPawnScaler.GetCacheUltraSpeed(__instance) is BSCache cache)
    //        {
    //            cache.ReevaluateGraphics();
    //        }
    //    }
    //}

    [HarmonyPatch]
    public static class BSVanillaPatches
    {
        [HarmonyPatch(typeof(LifeStageWorker), nameof(LifeStageWorker.Notify_LifeStageStarted))]
        [HarmonyPostfix]
        public static void Post_Notify_LifeStageStarted(Pawn pawn)
        {
            if (pawn.genes != null)
            {
                List<Gene> genes = pawn.genes.GenesListForReading;
                foreach (Gene gene in genes.Where(x => x.Active))
                {
                    GeneEffectManager.RefreshGeneEffects(gene, true);
                }
            }
            FastAcccess.GetCache(pawn, force: true);
        }
    }
}
