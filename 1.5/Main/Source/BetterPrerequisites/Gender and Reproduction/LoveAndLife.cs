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

        
    }

}
