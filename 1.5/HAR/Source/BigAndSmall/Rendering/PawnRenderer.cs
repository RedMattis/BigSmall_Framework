using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using VariedBodySizes;
using Verse;

namespace BigAndSmall
{

    [HarmonyPatch(typeof(PawnRenderer), "BaseHeadOffsetAt")]
    public static class PawnRenderer_BaseHeadOffsetAt
    {
        public static void Postfix(PawnRenderer __instance, ref Vector3 __result)
        {
            FieldInfo field = GetPawnField();
            Pawn pawn = (Pawn)field.GetValue(__instance);
            if (pawn != null)
            {
                var sizeCache = HumanoidPawnScaler.GetBSDict(pawn);
                if (sizeCache != null)
                {
                    var bodySize = sizeCache.bodyRenderSize;
                    var headSize = sizeCache.headRenderSize;
                    var headPos = Mathf.Lerp(bodySize, headSize, 0.8f);
                    headPos *= sizeCache.headPosMultiplier;
                    //var headPos = Mathf.Max(bodySize, headSize);

                    // Move up the head for dwarves etc. so they don't end up a walking head.
                    if (headPos < 1) { headPos = Mathf.Pow(headPos, 0.96f); }
                    __result.z *= headPos;
                    __result.x *= headPos;
                }
            }
        }
        public static FieldInfo pawnFieldInfo = null;
        private static FieldInfo GetPawnField()
        {
            if (pawnFieldInfo == null)
            {
                pawnFieldInfo = typeof(PawnRenderer).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return pawnFieldInfo;
        }
    }

    [HarmonyPatch(typeof(PawnRenderTree), "TrySetupGraphIfNeeded")]
    public static class PawnRenderer_TrySetupGraphIfNeeded_Patch
    {
        /// <summary>
        /// Set the pawn schedueled for rendering as the active pawn for scaling.
        /// </summary>
        public static void Prefix(PawnRenderTree __instance, Pawn ___pawn)
        {
            BigSmall.activePawn = ___pawn;
        }


        /// <summary>
        /// Pawn has been processed, time to remove it.
        /// </summary>
        public static void Postfix()
        {
            BigSmall.activePawn = null;
        }
    }


    [HarmonyPatch(typeof(PawnRenderer), "DrawBodyGenes")]
    public static class PawnRenderer_DrawBodyGenes_Patch
    {
        public static Vector2 bodyGraphicScalePreFix = new Vector2(1, 1);
        /// <summary>
        /// Temporarily edit the bodyGraphic Scale while running the DrawBodyGenes code.
        /// </summary>
        public static void Prefix(Pawn ___pawn, Vector3 rootLoc, Quaternion quat, float angle, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
        {
            var sizeCache = HumanoidPawnScaler.GetBSDict(___pawn);
            if (sizeCache != null && ___pawn?.story?.bodyType?.bodyGraphicScale != null)
            {
                bodyGraphicScalePreFix = new Vector2(___pawn.story.bodyType.bodyGraphicScale.x, ___pawn.story.bodyType.bodyGraphicScale.y);
                ___pawn.story.bodyType.bodyGraphicScale *= sizeCache.bodyRenderSize;
            }
        }

        /// <summary>
        /// Remove our edit.
        /// </summary>
        public static void Postfix(Pawn ___pawn)
        {
            var sizeCache = HumanoidPawnScaler.GetBSDict(___pawn);
            if (sizeCache != null && ___pawn?.story?.bodyType?.bodyGraphicScale != null)
            {
                ___pawn.story.bodyType.bodyGraphicScale = bodyGraphicScalePreFix;
            }
        }
    }


    //[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    //public static class PawnRenderer_RenderPawnAt_Patch
    //{
    //    public static Vector2 bodyGraphicScalePreFix = new Vector2(1, 1);
    //    public static float? bodyWidth = null;
    //    /// <summary>
    //    /// Temporarily edit the bodyGraphic Scale while running the DrawBodyGenes code.
    //    /// </summary>
    //    public static void Prefix(Pawn ___pawn, Vector3 drawLoc, Rot4? rotOverride, bool neverAimWeapon)
    //    {
    //        Log.Message($"1{BigSmall.performScaleCalculations} 2{BigSmall.humnoidScaler != null}" +
    //            $"4{___pawn != null} 5{___pawn.story != null} 6{___pawn.story.bodyType != null} 7{___pawn.story.bodyType.bodyGraphicScale != null}" +
    //            $"8{___pawn.ageTracker != null} 9{___pawn.ageTracker.CurLifeStage != null} 10{___pawn.ageTracker.CurLifeStage.bodyWidth != null} ");

    //        if (BigSmall.performScaleCalculations
    //            && BigSmall.humnoidScaler != null
    //            && ___pawn != null
    //            && ___pawn.story != null
    //            && ___pawn.story.bodyType != null
    //            && ___pawn.story.bodyType.bodyGraphicScale != null
    //            && ___pawn.ageTracker != null
    //            && ___pawn.ageTracker.CurLifeStage != null
    //            && ___pawn.ageTracker.CurLifeStage.bodyWidth != null)
    //        {
    //            bodyGraphicScalePreFix = new Vector2(___pawn.story.bodyType.bodyGraphicScale.x, ___pawn.story.bodyType.bodyGraphicScale.y);
    //            bodyWidth = ___pawn.ageTracker.CurLifeStage.bodyWidth;

    //            float multiplier = BigSmall.humnoidScaler.GetBodyRenderSize(___pawn);

    //            ___pawn.story.bodyType.bodyGraphicScale *= multiplier;
    //            ___pawn.ageTracker.CurLifeStage.bodyWidth *= multiplier;

    //            if (multiplier > 1)
    //            {
    //                Log.Message($"{___pawn.story.bodyType.bodyGraphicScale}, was {bodyGraphicScalePreFix}");
    //            }

    //        }
    //    }

    //    /// <summary>
    //    /// Remove our edit.
    //    /// </summary>
    //    public static void Postfix(Pawn ___pawn)
    //    {
    //        if (BigSmall.performScaleCalculations
    //            && BigSmall.humnoidScaler != null
    //            && ___pawn != null
    //            && ___pawn.story != null
    //            && ___pawn.story.bodyType != null
    //            && ___pawn.story.bodyType.bodyGraphicScale != null
    //            && ___pawn.ageTracker != null
    //            && ___pawn.ageTracker.CurLifeStage != null
    //            && ___pawn.ageTracker.CurLifeStage.bodyWidth != null)
    //        {
    //            ___pawn.story.bodyType.bodyGraphicScale = bodyGraphicScalePreFix;
    //            ___pawn.ageTracker.CurLifeStage.bodyWidth = bodyWidth;
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(PawnRenderer), "DrawPawnBody")]
    //public static class PawnRenderer_DrawPawnBody_Patch
    //{
    //    public static void Prefix(Pawn ___pawn, Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags, Mesh bodyMesh)
    //    {
    //    }

    //    public static void Postfix(Pawn ___pawn, Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags, Mesh bodyMesh)
    //    {
    //    }
    //}

    

    //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn))]
    //public static class HumanlikeBodyWidthForPawn_ETC_Patch
    //{
    //    public static void Postfix(GraphicMeshSet __result, Pawn pawn)
    //    {
    //        if (BigSmall.performScaleCalculations
    //            && BigSmall.humnoidScaler != null)
    //        {
    //            var sizeCache = HumanoidPawnScaler.GetPawnBSDict(pawn);
    //            if (sizeCache != null && pawn?.story?.bodyType?.bodyGraphicScale != null)
    //            {
    //                float scale = HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
    //                var meshes = new List<Mesh> { __result.MeshAt(Rot4.East), __result.MeshAt(Rot4.North), __result.MeshAt(Rot4.West), __result.MeshAt(Rot4.South) };

    //                foreach(var mesh in meshes)
    //                {
    //                    var center = new Vector3(mesh.vertices.Average(x => x.x), mesh.vertices.Average(x => x.y), mesh.vertices.Average(x => x.z));
    //                    mesh.vertices = mesh.vertices.Select(x=>(x-center) * scale + center).ToArray();
    //                }
    //            }
    //        }
    //    }
    //}


    //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.HumanlikeBodyWidthForPawn))]
    //public static class HumanlikeBodyWidthForPawn_Patch
    //{
    //    public static float? bodyWidthPrevious = 1;
    //    /// <summary>
    //    /// Temporarily edit the bodyGraphic Scale while running the DrawBodyGenes code.
    //    /// </summary>
    //    public static void Prefix(Pawn pawn)
    //    {
    //        Log.Message($"{BigSmall.performScaleCalculations}, {BigSmall.humnoidScaler}, {pawn.ageTracker.CurLifeStage.bodyWidth.HasValue}");
    //        if (BigSmall.performScaleCalculations
    //            && BigSmall.humnoidScaler != null
    //            && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
    //        {
    //            bodyWidthPrevious = pawn.ageTracker.CurLifeStage.bodyWidth;
    //            float multiplier = BigSmall.humnoidScaler.GetBodyRenderSize(BigSmall.activePawn);
    //            pawn.ageTracker.CurLifeStage.bodyWidth *= multiplier;
    //        }
    //    }

    //    /// <summary>
    //    /// Remove our edit.
    //    /// </summary>
    //    public static void Postfix(Pawn pawn)
    //    {
    //        pawn.ageTracker.CurLifeStage.bodyWidth = bodyWidthPrevious;
    //    }
    //}

    //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn))]
    //public static class GetHumanlikeBodySetForPawn_Patch
    //{
    //    public static float? bodyWidthPrevious = 1;
    //    /// <summary>
    //    /// Temporarily edit the bodyGraphic Scale while running the DrawBodyGenes code.
    //    /// </summary>
    //    public static void Prefix(Pawn pawn)
    //    {
    //        Log.Message($"{BigSmall.performScaleCalculations}, {BigSmall.humnoidScaler}, {pawn.ageTracker.CurLifeStage.bodyWidth.HasValue}");
    //        if (BigSmall.performScaleCalculations
    //            && BigSmall.humnoidScaler != null
    //            && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
    //        {
    //            bodyWidthPrevious = pawn.ageTracker.CurLifeStage.bodyWidth;
    //            float multiplier = BigSmall.humnoidScaler.GetBodyRenderSize(BigSmall.activePawn);
    //            pawn.ageTracker.CurLifeStage.bodyWidth *= multiplier;
    //        }
    //    }

    //    /// <summary>
    //    /// Remove our edit.
    //    /// </summary>
    //    public static void Postfix(Pawn pawn)
    //    {
    //        pawn.ageTracker.CurLifeStage.bodyWidth = bodyWidthPrevious;
    //    }
    //}

    //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn))]
    //public static class GetHumanlikeHeadSetForPawn_Patch
    //{
    //    public static float? bodyWidthPrevious = 1;
    //    /// <summary>
    //    /// Temporarily edit the bodyGraphic Scale while running the DrawBodyGenes code.
    //    /// </summary>
    //    public static void Prefix(Pawn pawn)
    //    {
    //        Log.Message($"{BigSmall.performScaleCalculations}, {BigSmall.humnoidScaler}, {pawn.ageTracker.CurLifeStage.bodyWidth.HasValue}");
    //        if (BigSmall.performScaleCalculations
    //            && BigSmall.humnoidScaler != null
    //            && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
    //        {
    //            bodyWidthPrevious = pawn.ageTracker.CurLifeStage.bodyWidth;
    //            float multiplier = BigSmall.humnoidScaler.GetBodyRenderSize(BigSmall.activePawn);
    //            pawn.ageTracker.CurLifeStage.bodyWidth *= multiplier;
    //        }
    //    }

    //    /// <summary>
    //    /// Remove our edit.
    //    /// </summary>
    //    public static void Postfix(Pawn pawn)
    //    {
    //        pawn.ageTracker.CurLifeStage.bodyWidth = bodyWidthPrevious;
    //    }
    //}


    //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn))]
    //public static class GetHumanlikeHairSetForPawn_Patch
    //{
    //    public static float? bodyWidthPrevious = 1;
    //    /// <summary>
    //    /// Temporarily edit the bodyGraphic Scale while running the DrawBodyGenes code.
    //    /// </summary>
    //    public static void Prefix(Pawn pawn)
    //    {
    //        Log.Message($"{BigSmall.performScaleCalculations}, {BigSmall.humnoidScaler}, {pawn.ageTracker.CurLifeStage.bodyWidth.HasValue}");
    //        if (BigSmall.performScaleCalculations
    //            && BigSmall.humnoidScaler != null
    //            && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
    //        {
    //            bodyWidthPrevious = pawn.ageTracker.CurLifeStage.bodyWidth;
    //            float multiplier = BigSmall.humnoidScaler.GetBodyRenderSize(BigSmall.activePawn);
    //            pawn.ageTracker.CurLifeStage.bodyWidth *= multiplier;
    //        }
    //    }

    //    /// <summary>
    //    /// Remove our edit.
    //    /// </summary>
    //    public static void Postfix(Pawn pawn)
    //    {
    //        pawn.ageTracker.CurLifeStage.bodyWidth = bodyWidthPrevious;
    //    }
    //}
}
