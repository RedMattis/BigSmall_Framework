using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
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



    //[HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.CalculatePermanentlyDisabled))]
    //public static class Pawn_GetDisabledWorkTypes
    //{
    //    public static void Postfix(SkillRecord __instance, ref bool __result)
    //    {
    //        if (HumanoidPawnScaler.GetCache(__instance.pawn) is BSCache cache && cache.disabledWorkTypes.Any())
    //        {
    //            if (cache.disabledWorkTypes.Contains(__instance.def))
    //            {
    //                __result = true;
    //            }
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Notify_SkillDisablesChanged))]
    public static class SkillRecord_Notify_SkillDisablesChanged
    {
        public static void Postfix(SkillRecord __instance)
        {
            if (__instance?.pawn == null)
            {
                return;
            }
            if (__instance.pawn.GetCachePrepatched() is BSCache cache && cache.skillsDisabledByExtensions.Any())
            {
                if (cache.skillsDisabledByExtensions.Contains(__instance.def))
                {
                    __instance.cachedPermanentlyDisabled = BoolUnknown.True;
                    __instance.cachedTotallyDisabled = BoolUnknown.True;
                }

            }
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.Notify_DisabledWorkTypesChanged))]
    public static class Pawn_WorkSettings_Notify_DisabledWorkTypesChanged
    {
        public static void Postfix(Pawn_WorkSettings __instance)
        {
            if (__instance.priorities == null || __instance.pawn == null)
            {
                return;
            }
            
            if (__instance.pawn.GetCachePrepatched() is BSCache cache && cache.disabledWorkTypes.Any())
            {
                foreach (var workType in cache.disabledWorkTypes)
                {
                    __instance.Disable(workType);
                    __instance.pawn.cachedDisabledWorkTypes?.AddDistinct(workType);
                    __instance.pawn.cachedDisabledWorkTypesPermanent?.AddDistinct(workType);
                }

            }
        }
    }

    //[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetDisabledWorkTypes))]
    //public static class Pawn_GetDisabledWorkTypes
    //{
    //    public static void Postfix(Pawn __instance, ref List<WorkTypeDef> __result, bool permanentOnly)
    //    {
    //        if (permanentOnly)
    //        {
    //            if (__instance.cachedDisabledWorkTypesPermanent != null)
    //            {
    //                return;
    //            }
    //        }
    //        else
    //        {
    //            if (__instance.cachedDisabledWorkTypes != null)
    //            {
    //                return;
    //            }
    //        }
    //        if (HumanoidPawnScaler.GetCache(__instance) is BSCache cache && cache.disabledWorkTypes.Any())
    //        {
    //            __result.AddDistinctRange(cache.disabledWorkTypes);
    //            if (permanentOnly)
    //            {
    //                __instance.cachedDisabledWorkTypesPermanent.AddDistinctRange(cache.disabledWorkTypes);
    //            }
    //            else
    //            {
    //                __instance.cachedDisabledWorkTypes.AddDistinctRange(cache.disabledWorkTypes);
    //            }

    //        }
    //    }
    //}

    // When the game is loaded, go through all hedifs in the pawns health tab and try to add supressors
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostMapInit))]
    public static class Pawn_PostMapInit
    {
        public static void Postfix(Pawn __instance)
        {
            RefreshPawnGenes(__instance, forceRefresh: true);


            if (HumanoidPawnScaler.GetCache(__instance) is BSCache cache)
            {
                var pawnExts = ModExtHelper.GetAllPawnExtensions(__instance);
                cache.HandleSkillsAndAptitudes(pawnExts);
            }
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
