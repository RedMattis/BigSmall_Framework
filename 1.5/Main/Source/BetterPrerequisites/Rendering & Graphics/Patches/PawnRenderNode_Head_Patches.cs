using HarmonyLib;
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
    public static class PawnRenderNode_Head_Patches
    {
        
        [HarmonyPatch(typeof(PawnRenderNode_Head), "GraphicFor")]
        [HarmonyPostfix]
        public static void PawnRenderNode_Head_GraphicFor_Patch(PawnRenderNode_Head __instance, Pawn pawn, ref Graphic __result)
        {
            if (__result == null) return;
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache && !cache.isDefaultCache && cache.isHumanlike)
            {
                HeadGraphics.CalculateHeadGraphicsForPawn(pawn, ref __result, cache);
            }
        }

    public static class HeadGraphics
    {
            public static void CalculateHeadGraphicsForPawn(Pawn pawn, ref Graphic __result, BSCache cache)
            {
                if (cache.hideHead) { __result = GraphicsHelper.GetBlankMaterial(pawn); return; }

                if (cache.headMaterial?.overrideDesiccated != true && pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated)
                {
                    return;
                }

                if (cache.headGraphicPath is string headGraphicPath)
                {
                    //Debug.Log("DEBUG! HeadGraphicPath: " + headGraphicPath);
                    __result = GraphicsHelper.TryGetCustomGraphics(pawn, headGraphicPath, __result.color, __result.colorTwo, __result.drawSize, cache.headMaterial);
                }
            }
        }
    }
}
