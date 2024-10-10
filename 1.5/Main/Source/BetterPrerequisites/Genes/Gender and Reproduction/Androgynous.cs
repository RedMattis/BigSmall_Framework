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
            if (pawn != null && pawn.gender == Gender.Male && pawn?.genes?.GenesListForReading?.Any(x=>x.def == BSDefs.Body_Androgynous) == true)
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
            if (pawn != null && pawn.gender == Gender.Male && pawn?.genes?.GenesListForReading?.Any(x => x.def == BSDefs.Body_Androgynous) == true)
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
            if (pawn != null && pawn.gender == Gender.Male && pawn?.genes?.GenesListForReading?.Any(x => x.def == BSDefs.Body_Androgynous) == true)
            {
                if (pawn.story.bodyType == BodyTypeDefOf.Male)
                {
                    pawn.story.bodyType = BodyTypeDefOf.Female;
                }
            }
        }

        // The purpose of these two lists is to avoid doing repeated lookups.
        public static HashSet<string> failedPaths = new HashSet<string>();
        public static HashSet<string> successPaths = new HashSet<string>();
        [HarmonyPatch(typeof(PawnRenderNode_Body), "GraphicFor")]
        public static class PawnRenderNode_Body_GraphicFor_Patch
        {
            [HarmonyPriority(Priority.VeryLow)]
            [HarmonyPostfix]
            public static void Postfix(PawnRenderNode_Body __instance, ref Pawn pawn, ref Graphic __result)
            {
                if (__result == null) return;
                // Uses the same general logic as the FemaleBodyVariant for compatibility reasons.
                // This is needed for Body_Androgynous

                bool mutantBody = pawn?.IsMutant != true && pawn?.mutant?.Def?.bodyTypeGraphicPaths.NullOrEmpty() == false;
                bool creepBody = !pawn?.IsCreepJoiner == true && pawn.story.bodyType != null && pawn?.creepjoiner?.form?.bodyTypeGraphicPaths.NullOrEmpty() == false;

                if (pawn.Drawer.renderer.CurRotDrawMode != RotDrawMode.Dessicated && !mutantBody && !creepBody && pawn.story?.bodyType?.bodyNakedGraphicPath != null && !__result.path.Contains("EmptyImage"))
                {
                    string bodyNakedGraphicPath = pawn.story.bodyType.bodyNakedGraphicPath;
                    bool femaleBody = pawn.gender != Gender.Male || pawn?.genes?.GenesListForReading.Any(x=>x.def == BSDefs.Body_Androgynous) == true;

                    if (femaleBody && bodyNakedGraphicPath != null && !bodyNakedGraphicPath.Contains("_Female") && (bodyNakedGraphicPath.Contains("_Thin") || bodyNakedGraphicPath.Contains("_Fat") || bodyNakedGraphicPath.Contains("_Hulk")))
                    {
                        bodyNakedGraphicPath += "_Female";

                        if (failedPaths.Contains(bodyNakedGraphicPath))
                        {
                            return;
                        }

                        // Check so the path actually exists.
                        if (successPaths.Contains(bodyNakedGraphicPath) || ContentFinder<Texture2D>.Get(bodyNakedGraphicPath + "_south", reportFailure:false) != null)
                        {
                            Shader shader = __instance.ShaderFor(pawn);
                            __result = GraphicDatabase.Get<Graphic_Multi>(bodyNakedGraphicPath, shader, Vector2.one, __instance.ColorFor(pawn));
                            successPaths.Add(bodyNakedGraphicPath);
                        }
                        else
                        {
                            failedPaths.Add(bodyNakedGraphicPath);
                        }
                    }
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
                bool androgynous = pawn.genes?.GenesListForReading?.Any(x => x.def == BSDefs.Body_Androgynous) == true;
                if (androgynous && pawn.gender == Gender.Male)
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


        //[HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
        //public static class Pawn_GeneTracker__Notify_GenesChanged
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(ref Pawn ___pawn, GeneDef addedOrRemovedGene)
        //    {
        //        if (___pawn != null && addedOrRemovedGene == BSDefs.Body_Androgynous && ___pawn.gender == Gender.Male)
        //        {
        //            ___pawn.story.bodyType = Verse.PawnGenerator.GetBodyTypeFor(___pawn);

        //            if (___pawn.story.bodyType == BodyTypeDefOf.Male)
        //            {
        //                ___pawn.story.bodyType = BodyTypeDefOf.Female;
        //            }

        //            ___pawn.Drawer.renderer.SetAllGraphicsDirty();

        //            if (___pawn.story.bodyType == BodyTypeDefOf.Male)
        //            {
        //                ___pawn.story.bodyType = BodyTypeDefOf.Female;
        //            }
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(Verse.PawnGenerator), nameof(Verse.PawnGenerator.GetBodyTypeFor))]
        //public static class PawnGenerator
        //{
        //    [HarmonyPostfix]
        //    public static void GetBodyTypeFor(Pawn pawn, ref BodyTypeDef __result)
        //    {
        //        if (pawn != null && __result == BodyTypeDefOf.Male && pawn.genes != null && pawn.genes.HasActiveGene(BSDefs.Body_Androgynous))
        //        {
        //            __result = BodyTypeDefOf.Female;
        //        }
        //    }

        //}
    }



}
