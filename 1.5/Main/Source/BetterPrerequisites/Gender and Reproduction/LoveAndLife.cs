using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class GenderAndSexPatches
    {
        [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
        [HarmonyPostfix]
        public static void EveryFertileFix(ref float __result, Pawn pawn)
        {
            if (pawn.needs != null)
            {
                var cache = HumanoidPawnScaler.GetCache(pawn);
                if (cache != null && cache.everFertile)
                {
                    if (__result < 1f)
                    {
                        __result = 1f;
                    }
                }
            }

            // Wtf is this?
            var myHediffNames = new List<string> { "VPECurses_VPECurse_Curse1", "VPECurses_VPECurse_Suffering2", "VPECurses_VPECurse_Misfortune99" };
            var matchingHediffs = pawn.health.hediffSet.hediffs.Where(x => x.def.defName == "VPECurses_VPECurse_Curse1");
            foreach (var hediff in matchingHediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }

        }

        [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.BodyResourceGrowthSpeed))]
        public static void Postfix(ref float __result, Pawn pawn)
        {
            var cache = HumanoidPawnScaler.GetCache(pawn);
            if (cache != null)
            {
                __result *= cache.pregnancySpeed;
            }
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.CompatibilityWith))]
        [HarmonyPostfix]
        public static void CompatibilityWith_Postfix(ref float __result, Pawn_RelationsTracker __instance, Pawn otherPawn, Pawn ___pawn)
        {
            if (!(__result > 0f))
            {
                __result = GetCompatibilityWith(___pawn, otherPawn, __result);
            }

        }

        public static float GetCompatibilityWith(this Pawn pawn, Pawn otherPawn, float defaultValue = 0)
        {
            float ConstantPerPawnsPairCompatibilityOffset(int otherPawnID)
            {
                Rand.PushState();
                Rand.Seed = (pawn.thingIDNumber ^ otherPawnID) * 37;
                float result = Rand.GaussianAsymmetric(0.3f, 1f, 1.4f);
                Rand.PopState();
                return result;
            }
            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && HumanoidPawnScaler.GetCacheUltraSpeed(otherPawn) is BSCache cacheTwo)
            {
                if (pawn.def != otherPawn.def || pawn == otherPawn)
                {
                    return 0f;
                }

                float x = Mathf.Abs(cache.apparentAge - cacheTwo.apparentAge);
                float num = Mathf.Clamp(GenMath.LerpDouble(0f, 20f, 0.45f, -0.45f, x), -0.45f, 0.45f);
                float num2 = ConstantPerPawnsPairCompatibilityOffset(otherPawn.thingIDNumber);
                return num + num2;
            }
            return defaultValue;
        }
    }

}
