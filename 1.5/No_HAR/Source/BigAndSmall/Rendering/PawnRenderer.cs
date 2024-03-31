using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using VariedBodySizes;
using Verse;

namespace BigAndSmall
{


    //[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
    //public static class PawnRenderer_RenderPawnInternal_Patch
    //{
    //    /// <summary>
    //    /// Set the pawn schedueled for rendering as the active pawn for scaling.
    //    /// </summary>
    //    public static void Prefix(Pawn ___pawn, Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
    //    {
    //        BigSmall.activePawn = ___pawn;
    //        //BigSmall.activeRenderParms = new RenderParameters(rootLoc, angle, renderBody, bodyFacing, bodyDrawType, flags);
    //    }


    //    /// <summary>
    //    /// Pawn has been processed, time to remove it.
    //    /// </summary>
    //    public static void Postfix()
    //    {
    //        BigSmall.activePawn = null;
    //    }
    //}

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

    [HarmonyPatch(typeof(PawnRenderer), "ParallelPreRenderPawnAt")]
    public static class PawnRenderer_ParallelPreRenderPawnAt_Patch
    {
        /// <summary>
        /// Set the pawn schedueled for rendering as the active pawn for scaling.
        /// </summary>
        [HarmonyPrefix]
        public static void SetBSActivePawn(PawnRenderTree __instance, Pawn ___pawn)
        {
            BigSmall.activePawn = ___pawn;
        }


        /// <summary>
        /// Pawn has been processed, time to remove it.
        /// </summary>
        [HarmonyPostfix]
        public static void ClearBSActivePawn()
        {
            BigSmall.activePawn = null;
        }
    }

    //[HarmonyPatch(typeof(Graphic), nameof(Graphic.DrawWorker))]
    //public static class PawnRenderer_DrawWorker_Patch
    //{
    //    /// <summary>
    //    /// Set the pawn schedueled for rendering as the active pawn for scaling.
    //    /// </summary>
    //    public static void Prefix(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
    //    {
    //        if (thing is Pawn pawn)
    //            BigSmall.activePawn = pawn;
    //    }
    //}


    //[HarmonyPatch(typeof(PawnRenderer), "DrawBodyGenes")]
    //public static class PawnRenderer_DrawBodyGenes_Patch
    //{
    //    public static Vector2 bodyGraphicScalePreFix = new Vector2(1, 1);
    //    /// <summary>
    //    /// Temporarily edit the bodyGraphic Scale while running the DrawBodyGenes code.
    //    /// </summary>
    //    public static void Prefix(Pawn ___pawn, Vector3 rootLoc, Quaternion quat, float angle, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
    //    {
    //        bodyGraphicScalePreFix = new Vector2(___pawn.story.bodyType.bodyGraphicScale.x, ___pawn.story.bodyType.bodyGraphicScale.y);

    //        ___pawn.story.bodyType.bodyGraphicScale *= HumanoidPawnScaler.GetBSDict(BigSmall.activePawn).bodyRenderSize;
    //    }

    //    /// <summary>
    //    /// Remove our edit.
    //    /// </summary>
    //    public static void Postfix(Pawn ___pawn)
    //    {
    //        ___pawn.story.bodyType.bodyGraphicScale = bodyGraphicScalePreFix;
    //    }
    //}

}
