using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{


    // Patch BleedRate so pawns with the gene Gene_NoBlood always have a bleed rate of 0.
    [HarmonyPatch(typeof(Hediff_Injury), nameof(Hediff_Injury.BleedRate), MethodType.Getter)]
    public class BleedRatePatch
    {
        public static void Postfix(ref float __result, ref Pawn_HealthTracker __instance, ref Pawn ___pawn)
        {
            // If the bleed rate is above 0. Check if the pawn has the Gene_NoBlood gene. If so set it to 0.
            __result = SetBleedRate(__result, ___pawn);
        }

        public static float SetBleedRate(float __result, Pawn ___pawn)
        {
            if (__result > 0)
            {
                if (___pawn?.needs != null)
                {
                    var sizeCache = HumanoidPawnScaler.GetCache(___pawn);
                    if (sizeCache != null)
                    {
                        if (sizeCache.bleedRate == BSCache.BleedRateState.NoBleeding)
                        {
                            __result = 0;
                        }
                    }
                }
            }

            return __result;
        }
    }

    // Patch BleedRate so pawns with the gene Gene_NoBlood always have a bleed rate of 0.
    [HarmonyPatch(typeof(Hediff_MissingPart), nameof(Hediff_MissingPart.BleedRate), MethodType.Getter)]
    public class BleedRate_Missing_Patch
    {
        public static void Postfix(ref float __result, ref Pawn_HealthTracker __instance, ref Pawn ___pawn)
        {
            // If the bleed rate is above 0. Check if the pawn has the Gene_NoBlood gene. If so set it to 0.
            __result = BleedRatePatch.SetBleedRate(__result, ___pawn);
        }
    }

    // Patch HediffSet's CalculateBleedRate similar to the above.
    [HarmonyPatch(typeof(HediffSet), "CalculateBleedRate")]
    public class CalculateBleedRatePatch
    {
        public static void Postfix(ref float __result, ref HediffSet __instance)
        {
            if (__result > 0)
            {
                var pawn = __instance.pawn;
                if (pawn?.needs != null)
                {
                    var sizeCache = HumanoidPawnScaler.GetCache(pawn);
                    if (sizeCache != null)
                    {
                        if (sizeCache.bleedRate == BSCache.BleedRateState.NoBleeding)
                        {
                            __result = 0;
                        }
                        else if (sizeCache.bleedRate == BSCache.BleedRateState.SlowBleeding)
                        {
                            __result /= 2;
                        }
                        else if (sizeCache.bleedRate == BSCache.BleedRateState.VerySlowBleeding)
                        {
                            __result /= 3;
                        }

                    }
                }
            }
        }
    }

    // Postfix Xenogerm's PawnIdeoDisallowsImplanting function so pawns that have the "VU_NoXenogerms" Gene return True.
    [HarmonyPatch(typeof(Xenogerm), nameof(Xenogerm.PawnIdeoDisallowsImplanting))]
    public static class PawnIdeoDisallowsImplantingPatch
    {
        public static void Postfix(ref bool __result, Pawn selPawn)
        {
            var pawn = selPawn;
            if (pawn?.needs != null)
            {
                
                var sizeCache = HumanoidPawnScaler.GetCache(pawn);
                if (sizeCache != null)
                {
                    var validGenes = GeneHelpers.GetActiveGenesByName(pawn, "BS_NoXenogerms");
                    if (validGenes.Count() > 0)
                    {
                        __result = true;
                    }
                }
            }
        }
    }

    // Postfix IsBloodfeeder is it also return true if the pawn has the "VU_NoBlood" gene.
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.IsBloodfeeder))]
    public static class IsBloodfeederPatch
    {
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (__result == false && pawn?.needs != null)
            {
                var sizeCache = HumanoidPawnScaler.GetCache(pawn);
                if (sizeCache != null)
                {
                    __result = sizeCache.isBloodFeeder;
                }
            }
        }

        public static bool IsBloodfeeder(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike && (pawn.needs != null || pawn.Dead) && pawn.genes != null)
            {
                var matchingGenes = new List<string>() { "VU_NoBlood", "VU_WhiteRoseBite", "VU_DraculBite", "VU_SuccubusBloodFeeder" };
                var validGenes = GeneHelpers.GetActiveGenesByNames(pawn, matchingGenes);
                if (validGenes.Count() > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetHemogen), "CanFeedOnPrisoner")]
    public static class CanFeedOnPrisoner_HarmonyPatch
    {
        public static void Postfix(Pawn bloodfeeder, Pawn prisoner, ref AcceptanceReport __result)
        {
            if (__result && HumanoidPawnScaler.GetCacheUltraSpeed(prisoner) is BSCache cache)
            {
                if (cache.isBloodFeeder || cache.isUnliving || cache.isMechanical || cache.bleedRate == BSCache.BleedRateState.NoBleeding)
                {
                    __result = AcceptanceReport.WasRejected;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Recipe_ExtractHemogen), nameof(Recipe_ExtractHemogen.AvailableOnNow))]
    public static class Recipe_ExtractHemogenPatch
    {
        public static void Postfix(ref bool __result, Thing thing, BodyPartRecord part)
        {
            if (__result && thing is Pawn pawn && HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache)
            {
                if (cache.isBloodFeeder || cache.isUnliving || cache.isMechanical || cache.bleedRate == BSCache.BleedRateState.NoBleeding)
                {
                    __result = false;
                }
            }
        }
    }

    // Too expensive. Not worth it.
    //[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.CanBleed), MethodType.Getter)]
    //public static class CanBleedPatch
    //{
    //    public static void Postfix(ref bool __result, ref Pawn_HealthTracker __instance, ref Pawn ___pawn)
    //    {
    //        if (__result == true && HumanoidPawnScaler.GetCacheUltraSpeed(___pawn) is BSCache sizeCache)
    //        {
    //            if (sizeCache.bleedRate == BSCache.BleedRateState.NoBleeding)
    //            {
    //                __result = false;
    //            }
    //        }
    //    }
    //}
}
