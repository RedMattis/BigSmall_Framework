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
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.LearnRateFactor))] //"LearnRateFactor"
    public class SkillRecord_Patch
    {
        public static void Postfix(ref float __result, SkillRecord __instance)
        {
            var sizeCache = HumanoidPawnScaler.GetBSDict(__instance.Pawn);
            if (sizeCache != null && sizeCache.minimumLearning > 0.351)
            {
                if (__instance.passion == Passion.None)
                {
                    // If we have a minimum skill learning speed of 0.35 and a override for 1 this will make the 
                    // final skill learning rate 1.0.
                    float value = sizeCache.minimumLearning / 0.35f;
                    __result *= value;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_AgeTracker), "GrowthPointsPerDayAtLearningLevel")] //"LearnRateFactor"
    public static class GrowthPointPerDayAtLearningLevel_Patch
    {
        public static void Postfix(ref float __result, Pawn ___pawn)
        {
            var sizeCache = HumanoidPawnScaler.GetBSDict(___pawn);
            if (HumanoidPawnScaler.GetBSDict(___pawn) is BSCache cache)
            {
                __result *= cache.growthPointGain;
            }
        }
    }

}
