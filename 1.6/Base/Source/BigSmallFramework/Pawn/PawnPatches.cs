using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System.Linq;
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
                Pawn pawn = __instance;
                float? foodNeed = pawn?.needs?.food?.CurLevelPercentage;
                Pawn_PostMapInit.RefreshPawnGenes(pawn, forceRefresh: true);
                if (foodNeed != null)
                {
                    pawn.needs.food.CurLevelPercentage = foodNeed.Value;
                }

                pawn.def.modExtensions?.OfType<RaceExtension>()?.FirstOrDefault()?.ApplyTrackerIfMissing(pawn);

                var pawnExtensions = ModExtHelper.GetAllPawnExtensions(pawn);
                int? minAge = pawnExtensions.Max(x => x.babyStartAge);
                if (minAge != null && pawn.ageTracker.AgeBiologicalYears < minAge)
                {
                    pawn.ageTracker.AgeBiologicalTicks = (long)(minAge * 3600000) + 1000;

                    // If a scenario/script forced them to a younger age then we will likely want to yeet the old cache completely.
                    HumanoidPawnScaler.Cache.TryRemove(pawn, out _);
                    HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
                }
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
                GenderMethods.UpdatePawnHairAndHeads(__instance);

                //foreach (var gene in GeneHelpers.GetAllActiveGenes(__instance))
                //{
                //    GeneEffectManager.RefreshGeneEffects(gene, active: true);
                //}
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
            // Commented out because it can cause crashes on loading the saves. Probably something (another mod likely) can break pawns
            // in a way that prevents loading saves.

            //if (pawn?.genes != null)
            //{
            //    List<Gene> genes = pawn.genes.GenesListForReading;
            //    foreach (Gene gene in genes.Where(x => x.Active))
            //    {
            //        GeneEffectManager.RefreshGeneEffects(gene, true);
            //    }
            //}
            HumanoidPawnScaler.ShedueleForceRegenerateSafe(pawn, 100);
        }
    }
}
