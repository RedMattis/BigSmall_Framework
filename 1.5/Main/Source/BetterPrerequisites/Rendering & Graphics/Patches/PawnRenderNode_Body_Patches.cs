﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(PawnRenderNode_Body), "GraphicFor")]
    public static class PawnRenderNode_Body_GraphicFor_Patch
    {
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode_Body __instance, ref Pawn pawn, ref Graphic __result)
        {
            if (__result == null) return;
            //var debugCache = HumanoidPawnScaler.GetCache(pawn, reevaluateGraphics: true);

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
            if (cache.hideBody) { __result = GraphicsHelpers.GetBlankMaterial(pawn); return; }

            if (cache.bodyMaterial?.overrideDesiccated != true && pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated)
            {
                return;
            }

            if (cache.bodyGraphicPath is string bodyGraphicPath)
            {
                __result = GraphicsHelpers.TryGetCustomGraphics(pawn, bodyGraphicPath, __result.color, __result.colorTwo, __result.drawSize, cache.bodyMaterial);
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
            bool mutantBody = pawn?.IsMutant != true && pawn?.mutant?.Def?.bodyTypeGraphicPaths.NullOrEmpty() == false;
            bool creepBody = !pawn?.IsCreepJoiner == true && pawn.story.bodyType != null && pawn?.creepjoiner?.form?.bodyTypeGraphicPaths.NullOrEmpty() == false;
            bool doRun = !mutantBody && !creepBody && pawn.story?.bodyType?.bodyNakedGraphicPath != null && !__result.path.Contains("EmptyImage");
            return doRun;
        }
    }
}