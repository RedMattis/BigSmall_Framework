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
    

    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
    public static class PawnRenderer_RenderPawnInternal_Patch
    {
        /// <summary>
        /// Set the pawn schedueled for rendering as the active pawn for scaling.
        /// </summary>
        public static void Prefix(Pawn ___pawn, Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
        {
            BigSmall.activePawn = ___pawn;
            //BigSmall.activeRenderParms = new RenderParameters(rootLoc, angle, renderBody, bodyFacing, bodyDrawType, flags);
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
            bodyGraphicScalePreFix = new Vector2(___pawn.story.bodyType.bodyGraphicScale.x, ___pawn.story.bodyType.bodyGraphicScale.y);

            ___pawn.story.bodyType.bodyGraphicScale *= HumanoidPawnScaler.GetBSDict(BigSmall.activePawn).bodyRenderSize;
        }

        /// <summary>
        /// Remove our edit.
        /// </summary>
        public static void Postfix(Pawn ___pawn)
        {
            ___pawn.story.bodyType.bodyGraphicScale = bodyGraphicScalePreFix;
        }
    }

}
