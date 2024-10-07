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
    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.BodyResourceGrowthSpeed))]
    public static class PawnUtility_BodyResourceGrowthSpeed_Patch
    {
        public static void Postfix(ref float __result, Pawn pawn)
        {
            var cache = HumanoidPawnScaler.GetBSDict(pawn);
            if (cache != null)
            {
                __result *= cache.pregnancySpeed;
            }
        }
    }

    [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
    public static class StatPart_FertilityByGenderAge_Patch
    {
        [HarmonyPostfix]
        public static void EveryFertileFix(ref float __result, Pawn pawn)
        {
            if (pawn.needs != null)
            {
                var cache = HumanoidPawnScaler.GetBSDict(pawn);
                if (cache != null && cache.everFertile)
                {
                    if (__result < 1f)
                    {
                        __result = 1f;
                    }
                }
            }

            // Get all hediffs in the game
            var myHediffNames = new List<string> { "VPECurses_VPECurse_Curse1", "VPECurses_VPECurse_Suffering2", "VPECurses_VPECurse_Misfortune99" };
            var matchingHediffs = pawn.health.hediffSet.hediffs.Where(x => x.def.defName == "VPECurses_VPECurse_Curse1");
            foreach (var hediff in matchingHediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }

        }
    }

}
