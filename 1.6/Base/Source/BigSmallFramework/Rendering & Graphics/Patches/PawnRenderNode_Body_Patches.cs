using HarmonyLib;
using Verse;
using UnityEngine;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(PawnRenderNode_Body), nameof(PawnRenderNode_Body.GraphicFor))]
    public static class PawnRenderNode_Body_GraphicFor_Patch
    {
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode_Body __instance, ref Pawn pawn, ref Graphic __result)
        {
            if (__result == null) return;
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache && !cache.isDefaultCache && cache.isHumanlike)
            {
                BodyGraphics.CalculateBodyGraphicsForPawn(__instance, pawn, ref __result, cache);
            }
        }
    }

    public static class BodyGraphics
    {
        public static void CalculateBodyGraphicsForPawn(PawnRenderNode_Body __instance, Pawn pawn, ref Graphic __result, BSCache cache)
        {
            if (cache.hideBody) { __result = GraphicsHelper.GetBlankMaterial(); return; }

            bool dessicated = pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated;
            if (dessicated)
            {
                if (cache.bodyDessicatedGraphicPath != null)
                {
                    var dessicatedBodyPath = cache.bodyDessicatedGraphicPath;
                    var res = GraphicsHelper.TryGetCustomGraphics(__instance, dessicatedBodyPath, __result.color, __result.colorTwo, Color.white, __result.drawSize, cache.bodyMaterial);
                    if (res != null)
                    {
                        __result = res;
                    }
                    else
                    {
                        Log.ErrorOnce($"Failed to get dessicated body graphic for {pawn?.Name} at {dessicatedBodyPath}. Keeping previous graphic instead", 93484);
                    }
                    return;
                }
                if (cache.bodyMaterial?.overrideDesiccated != true)
                {
                    return;
                }
                
            }
            if (cache.bodyGraphicPath is string bodyGraphicPath)
            {
                var res = GraphicsHelper.TryGetCustomGraphics(__instance, bodyGraphicPath, __result.color, __result.colorTwo, Color.white, __result.drawSize, cache.bodyMaterial);
                if (res != null)
                {
                    __result = res;
                }
                else
                {
                    Log.ErrorOnce($"Failed to get body graphic for {pawn?.Name} at {bodyGraphicPath}. Keeping previous graphic instead.", 99333);
                }
                return;
            }

            bool showStandardBody = ShowStandardBody(pawn, __result);
            if (showStandardBody)
            {
                GenderMethods.TrySetGenderBody(__instance, pawn, ref __result);
            }
        }

        public static bool ShowStandardBody(Pawn pawn, Graphic __result)
        {
            if (__result.path == null) return false;
            bool mutantBody = pawn?.IsMutant != true && pawn?.mutant?.Def?.bodyTypeGraphicPaths.NullOrEmpty() == false;
            bool creepBody = !pawn?.IsCreepJoiner == true && pawn?.story?.bodyType != null && pawn?.creepjoiner?.form?.bodyTypeGraphicPaths.NullOrEmpty() == false;
            bool doRun = !mutantBody && !creepBody && pawn.story?.bodyType?.bodyNakedGraphicPath != null && !__result.path.Contains("EmptyImage");
            return doRun;
        }
    }
}
