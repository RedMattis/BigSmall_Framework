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
            bool forceFemaleBody = HumanoidPawnScaler.GetCache(pawn) is BSCache cache && cache.forceFemaleBody;
            if (pawn != null && pawn.gender == Gender.Male && forceFemaleBody)
            {
                switch (bodyType)
                {
                    case GeneticBodyType.Standard:
                        __result = BodyTypeDefOf.Female;
                        return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GetBodyTypeFor))]
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPostfix]
        public static void PawnGenerator_GetBodyTypeFor(Pawn pawn, ref BodyTypeDef __result)
        {
            bool forceFemaleBody = HumanoidPawnScaler.GetCache(pawn) is BSCache cache && cache.forceFemaleBody;
            if (pawn != null && pawn.gender == Gender.Male && forceFemaleBody)
            {
                if (__result == BodyTypeDefOf.Male)
                {
                    __result = BodyTypeDefOf.Female;
                }
            }
        }

        [HarmonyPatch(typeof(PawnGenerator), "GenerateBodyType")]
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPostfix]
        public static void PawnGenerator_GenerateBodyType(Pawn pawn)
        {
            bool forceFemaleBody = HumanoidPawnScaler.GetCache(pawn) is BSCache cache && cache.forceFemaleBody;
            if (pawn != null && pawn.gender == Gender.Male && forceFemaleBody)
            {
                if (pawn.story.bodyType == BodyTypeDefOf.Male)
                {
                    pawn.story.bodyType = BodyTypeDefOf.Female;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_StoryTracker), nameof(Pawn_StoryTracker.TryGetRandomHeadFromSet))]
        public static class TryGetRandomHeadFromSet_Patch
        {
            public static bool swapBackToMale = false;
            [HarmonyPrefix]
            public static void Prefix(Pawn_StoryTracker __instance, IEnumerable<HeadTypeDef> options)
            {
                var pawn = GetPawnFromStoryTracker(__instance);
                bool forceFemaleBody = HumanoidPawnScaler.GetCache(pawn) is BSCache cache && cache.forceFemaleBody;
                if (forceFemaleBody && pawn.gender == Gender.Male)
                {
                    swapBackToMale = true;
                    pawn.gender = Gender.Female;
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
                swapBackToMale = false;
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
