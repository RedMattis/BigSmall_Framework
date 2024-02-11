using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.Noise;
using Verse;
using RimWorld;
using UnityEngine;

namespace BigAndSmall
{
    // Might be causing pawns to constantly regenerate health under some circumstances due to some rounding error or modmismatch.
    // Investigate and restore once fixed.
    [HarmonyPatch(typeof(Pawn), "HealthScale", MethodType.Getter)]
    public static class Pawn_HealthScale
    {
        public static void Postfix(ref float __result, Pawn __instance)
        {

            var sizeCache = HumanoidPawnScaler.GetBSDict(__instance);
            if (sizeCache != null)
            {
                float newScale = __result * CalculateHealthMultiplier(sizeCache.scaleMultiplier);
                if (sizeCache.previousScaleMultiplier != null)
                {
                    float oldscale = __result * CalculateHealthMultiplier(sizeCache.previousScaleMultiplier);
                    if (sizeCache.injuriesRescaled == false && newScale.ApproximatelyEquals(oldscale) == false)
                    {
                        // Check time since loading scene. We don't want to rescale injuries when loading a save.
                        // This is because the injuries are already scaled when loading a save, and rescaling them
                        // again will cause them to get heal small pawns everytime the save is loaded.
                        if (Time.timeSinceLevelLoad < BigSmallMod.settings.cacheUpdateFrequency * 3f)
                        {
                            sizeCache.injuriesRescaled = true;
                            sizeCache.previousScaleMultiplier = sizeCache.scaleMultiplier;
                            //Log.Warning($"Supressed Injury Rescale due to save-file loading {Time.timeSinceLevelLoad}");
                        }
                        else
                        {

                            // Scale all injuries by the difference between the old and new health multipliers.
                            // This is meant to make sure a pawns injuries don't get more severe if they shrink.
                            // It shouldn't really be needed, but shrinking pawns seem to make their injuries get worse,
                            // Which can result in Nisses dying after spawning with scars.
                            float injuryScale = newScale / oldscale;
                            injuryScale = Mathf.Min(1, injuryScale);
                            var injuries = new List<Hediff_Injury>();
                            __instance.health.hediffSet.GetHediffs(ref injuries);
                            foreach (var injury in injuries)
                            {
                                injury.Severity *= injuryScale;
                                if (injury.Severity < 0)
                                {
                                    injury.Severity = 0;
                                }
                            }
                            sizeCache.injuriesRescaled = true;
                        }
                    }
                }
                __result = newScale;
            }

        }

        private static float CalculateHealthMultiplier(BSCache.PercentChange scalMult)
        {
            float quad = scalMult.quadratic;
            float roughylLinear = scalMult.linear;
            if (roughylLinear > 1)
            {
                roughylLinear = (scalMult.linear - 1) * 0.8f + 1; // Nerf scaling a bit, large pawns are tanky enough already.
            }
            if (roughylLinear > quad) { quad = roughylLinear; } // Make sure small creatures don't get absolutely unreasonably low health.
            return Mathf.Lerp(roughylLinear, quad, 0.20f);
        }
    }

    [HarmonyPatch(typeof(Need_Food), nameof(Need_Food.FoodFallPerTickAssumingCategory))]
    public static class Need_Food_FoodFallPerTickAssumingCategory
    {
        public static void Prefix(ref Pawn ___pawn, out float __state)
        {
            __state = ___pawn.def.race.baseHungerRate;
            if (___pawn != null
                && ___pawn.needs != null
                && ___pawn.DevelopmentalStage > DevelopmentalStage.Baby)
            {
                var sizeCache = HumanoidPawnScaler.GetBSDict(___pawn);
                if (sizeCache != null)
                {
                    float hungerRate = __state * Mathf.Max(sizeCache.scaleMultiplier.linear, sizeCache.scaleMultiplier.DoubleMaxLinear);
                    float finalHungerRate = Mathf.Lerp(__state, hungerRate, BigSmallMod.settings.hungerRate);

                    ___pawn.def.race.baseHungerRate = finalHungerRate;
                }
            }
        }

        public static void Postfix(ref float __result, Pawn ___pawn, float __state)
        {
            ___pawn.def.race.baseHungerRate = __state;

            //if (BigSmall.performScaleCalculations
            //    && ___pawn.needs != null
            //    && BigSmall.humnoidScaler != null
            //    && BigSmall.___pawn.DevelopmentalStage > DevelopmentalStage.Baby)
            //{
            //    var sizeCache = HumanoidPawnScaler.GetPawnBSDict(___pawn);
            //    if (sizeCache != null)
            //        __result *= Mathf.Max(sizeCache.scaleMultiplier.quadratic, sizeCache.scaleMultiplier.linear);
            //}
            //else
            //{
            //    Log.Warning($"No hunger calculations could be done for Pawn.");
            //}
        }
    }

    //[HarmonyPatch(typeof(RaceProperties), nameof(RaceProperties.NutritionEatenPerDayExplanation))]
    //public static class RaceProperties_NutritionEatenPerDayExplanation
    //{
    //    public static void Prefix(ref Pawn p, out float __state)
    //    {
    //        __state = p.def.race.baseHungerRate;
    //        if (
    //            BigSmall.performScaleCalculations
    //            && p.needs != null
    //            && BigSmall.humnoidScaler != null
    //            && p.DevelopmentalStage > DevelopmentalStage.Baby)
    //        {
    //            var sizeCache = HumanoidPawnScaler.GetPawnBSDict(p);
    //            if (sizeCache != null)
    //                p.def.race.baseHungerRate = __state * Mathf.Max(sizeCache.scaleMultiplier.linear, 0.2f);
    //            else
    //            {
    //                Log.Warning("Failed to set NutritionEatenPerDayExplanation");
    //            }
    //        }
    //    }

    //    public static void Postfix(Pawn p, float __state)
    //    {
    //        p.def.race.baseHungerRate = __state;
    //    }
    //}


    [HarmonyPatch(typeof(Verb_MeleeAttack), "GetDodgeChance")]
    public static class VerbMeleeAttack_GetDodgeChance
    {
        public static void Postfix(ref float __result, LocalTargetInfo target)
        {
            if (target.Thing is Pawn pawn && __result < 0.99f && HumanoidPawnScaler.GetBSDict(pawn) is BSCache sizeCache)
            {
                if (sizeCache != null)
                    __result /= sizeCache.scaleMultiplier.linear;
                if (__result >= 0.96)
                    __result = 0.96f;
            }
        }
    }




    //// AdjustedArmorPenetration

    //[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedArmorPenetration), new Type[]
    //    {
    //    typeof(Tool),
    //    typeof(Pawn),
    //    typeof(HediffComp_VerbGiver)
    //    })
    //]
    //public static class VerbProperties_AdjustedArmorPenetration_Patch
    //{
    //    public static void Postfix(ref float __result, Pawn attacker, VerbProperties __instance)
    //    {
    //        if (BigSmall.performScaleCalculations &&
    //            __instance.IsMeleeAttack && attacker != null
    //            && BigSmall.humnoidScaler != null)
    //        {
    //            float armorPenAdjustment = BigSmall.humnoidScaler.GetSizeChangeMultiplier(HumanoidPawnScaler.SizeChangeType.Linear, attacker) - 1;
    //            float extraArmorPen = 20 * armorPenAdjustment;

    //            if (extraArmorPen > 0)
    //            {
    //                // Make giants a bit less prone to instant-killing.
    //                armorPenAdjustment = Mathf.Pow(armorPenAdjustment, 0.50f);
    //                Log.Message($"DEBUG: Armor Pen is {armorPenAdjustment}");
    //            }
    //            else
    //            {
    //                armorPenAdjustment = 0;
    //            };

    //            // Mostly for balance reasons, too much instant-death otherwise.
    //            __result += armorPenAdjustment;
    //        }
    //    }
    //}

}
