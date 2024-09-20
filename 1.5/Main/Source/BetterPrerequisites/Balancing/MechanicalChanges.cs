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
using System.Reflection.Emit;
using System.Reflection;

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
                float newScale = __result * sizeCache.healthMultiplier;
                if (sizeCache.previousScaleMultiplier != null)
                {
                    float oldscale = __result * sizeCache.healthMultiplier_previous;
                    if (sizeCache.injuriesRescaled == false && newScale.ApproximatelyEquals(oldscale) == false)
                    {
                        // Check time since loading scene. We don't want to rescale injuries when loading a save.
                        // This is because the injuries are already scaled when loading a save, and rescaling them
                        // again will cause them to get heal small pawns everytime the save is loaded.
                        if (Time.timeSinceLevelLoad < 2250)
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

        /// <summary>
        /// Calculates the health based on some fudged math. Technically it should probably be the cubic change, but that makes large creatures tank antigrain warheads.
        /// </summary>
        /// <param name="scalMult"></param>
        /// <returns></returns>
        
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
        }
    }



    [HarmonyPatch(typeof(Verb_MeleeAttack), "GetDodgeChance")]
    public static class VerbMeleeAttack_GetDodgeChance
    {
        public static void Postfix(ref float __result, LocalTargetInfo target)
        {
            if (target.Thing is Pawn pawn && __result < 0.99f && HumanoidPawnScaler.GetBSDict(pawn) is BSCache sizeCache)
            {
                __result /= sizeCache.scaleMultiplier.linear;
                if (__result >= 0.96)
                    __result = 0.96f;
            }
        }
    }

}
