using HarmonyLib;
using Verse;
using UnityEngine;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class PawnRenderNode_Head_Patches
    {

        [HarmonyPatch(typeof(PawnRenderNode_Head), "GraphicFor")]
        [HarmonyPostfix]
        public static void PawnRenderNode_Head_GraphicFor_Patch(PawnRenderNode_Head __instance, Pawn pawn, ref Graphic __result)
        {
            if (__result == null)
            {
                return;
            }
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache && !cache.isDefaultCache && cache.isHumanlike)
            {
                HeadGraphics.CalculateHeadGraphicsForPawn(__instance, ref __result, cache);
            }
        }

        public static class HeadGraphics
        {
            public static void CalculateHeadGraphicsForPawn(PawnRenderNode_Head headNode, ref Graphic __result, BSCache cache)
            {
                if (cache.hideHead) { __result = GraphicsHelper.GetBlankMaterial(); return; }

                bool dessicated = headNode.tree.pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated;
                if (dessicated)
                {
                    if (cache.headDessicatedGraphicPath != null)
                    {
                        var dessicatedHeadPath = cache.headDessicatedGraphicPath;
                        __result = GraphicsHelper.TryGetCustomGraphics(headNode, dessicatedHeadPath, __result.color, __result.colorTwo, Color.white, __result.drawSize, cache.headMaterial);
                        return;
                    }
                    if (cache.headMaterial?.overrideDesiccated != true)
                    {
                        return;
                    }
                }

                if (cache.headGraphicPath is string headGraphicPath)
                {
                    var result = GraphicsHelper.TryGetCustomGraphics(headNode, headGraphicPath, __result.color, __result.colorTwo, Color.white, __result.drawSize, cache.headMaterial);
                    if (result != null)
                    {
                        __result = result;
                        return;
                    }
                    else
                    {
                        Log.Warning($"{headNode?.tree?.pawn}  requested headGraphicPath, but TryGetCustomGraphics returned null");
                    }
                }
            }
        }
    }
}
