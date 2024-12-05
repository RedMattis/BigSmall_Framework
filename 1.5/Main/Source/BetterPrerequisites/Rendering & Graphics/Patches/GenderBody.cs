using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static partial class HarmonyPatches
    {
        [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.ToBodyType))]
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPrefix]
        public static bool ToBodyTypePatch(ref BodyTypeDef __result, GeneticBodyType bodyType, Pawn pawn)
        {
            if (pawn != null && HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                if (bodyType == GeneticBodyType.Standard && pawn.IsAdult())
                {
                    Gender apparentGender = cache.GetApparentGender();
                    if (GenderMethods.TryBodyGenderBodyUpdate(pawn.story.bodyType, apparentGender, cache, out BodyTypeDef newBody))
                    {
                        __result = newBody;
                        return false;
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GetBodyTypeFor))]
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPostfix]
        public static void PawnGenerator_GetBodyTypeFor(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn != null && HumanoidPawnScaler.GetCache(pawn) is BSCache cache && pawn.IsAdult())
            {
                Gender apparentGender = cache.GetApparentGender();
                if (GenderMethods.TryBodyGenderBodyUpdate(pawn.story.bodyType, apparentGender, cache, out BodyTypeDef newBody))
                {
                    __result = newBody;
                }
            }
        }

        [HarmonyPatch(typeof(PawnGenerator), "GenerateBodyType")]
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPostfix]
        public static void PawnGenerator_GenerateBodyType(Pawn pawn)
        {
            if (pawn != null && HumanoidPawnScaler.GetCache(pawn) is BSCache cache && pawn.IsAdult())
            {
                Gender apparentGender = cache.GetApparentGender();
                if (GenderMethods.TryBodyGenderBodyUpdate(pawn.story.bodyType, apparentGender, cache, out BodyTypeDef newBody))
                {
                    pawn.story.bodyType = newBody;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_StoryTracker), nameof(Pawn_StoryTracker.TryGetRandomHeadFromSet))]
        public static class TryGetRandomHeadFromSet_Patch
        {
            public static bool swapBackToMale = false;
            public static bool swapBackToFemale = false;
            [HarmonyPrefix]
            public static void Prefix(Pawn_StoryTracker __instance, IEnumerable<HeadTypeDef> options)
            {
                var pawn = GetPawnFromStoryTracker(__instance);
                if (pawn != null && HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
                {
                    Gender apparentGender = cache.GetApparentGender();
                    if (apparentGender == Gender.Female && pawn.gender == Gender.Male)
                    {
                        swapBackToMale = true;
                        pawn.gender = Gender.Female;
                    }
                    else if (apparentGender == Gender.Male && pawn.gender == Gender.Female)
                    {
                        swapBackToFemale = true;
                        pawn.gender = Gender.Male;
                    }
                }
            }

            [HarmonyPostfix]
            public static void Postfix(Pawn_StoryTracker __instance, IEnumerable<HeadTypeDef> options)
            {
                var pawn = GetPawnFromStoryTracker(__instance);
                if (swapBackToMale)
                {
                    pawn.gender = Gender.Male;
                }
                if (swapBackToFemale)
                {
                    pawn.gender = Gender.Female;
                }
                swapBackToMale = false;
                swapBackToFemale = false;
            }

            public static FieldInfo pawnFieldInfo = null;
            public static Pawn GetPawnFromStoryTracker(Pawn_StoryTracker storyTracker)
            {
                // Get private pawn field from story tracker.
                if (pawnFieldInfo == null)
                {
                    pawnFieldInfo = AccessTools.Field(typeof(Pawn_StoryTracker), "pawn");
                }
                return pawnFieldInfo.GetValue(storyTracker) as Pawn;
            }
        }
    }
}
