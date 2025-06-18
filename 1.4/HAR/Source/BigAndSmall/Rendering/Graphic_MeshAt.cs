using HarmonyLib;
using RimWorld;
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
    [HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    public static class Pawn_DrawTracker_Patch
    {
        public static bool skipOffset = false;
        [HarmonyPostfix]
        public static void DrawPos_Patch(ref Vector3 __result, Pawn_DrawTracker __instance, Pawn ___pawn)
        {
            if (!skipOffset 
                && BigSmallMod.settings.offsetBodyPos
                && ___pawn.GetPosture() == PawnPosture.Standing)
            {
                if (___pawn?.RaceProps?.Humanlike == true)
                {
                    var cache = HumanoidPawnScaler.GetBSDict(___pawn);
                    var factor = cache.bodyRenderSize;
                    var originalFactor = factor;
                    if (factor < 1) { factor = 1; }
                    float offsetFromCache = cache.bodyPosOffset;

                    var bodyType = ___pawn.story.bodyType;

                    // Check if hulk. If so increase the value, because hulks are weirldy offset down in vanilla.
                    if (bodyType == BodyTypeDefOf.Hulk)
                    {
                        offsetFromCache += 0.25f;
                    }

                    __result.z += (factor - 1) / 2 * (offsetFromCache + 1) + offsetFromCache * 0.30f * (originalFactor < 1 ? originalFactor : 1) * bodyType.bodyGraphicScale.y;
                }
            }
        }
    }

    [HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.DrawSelectionBracketFor))]
    public static class SelectionDrawer_DrawSelection_Patch
    {
        public static void Prefix(object obj)
        {
            Pawn_DrawTracker_Patch.skipOffset = true;
        }

        public static void Postfix(object obj)
        {
            Pawn_DrawTracker_Patch.skipOffset = false;
        }
    }

    [HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
    public static class PawnUIOverlay_DrawSelection_Patch
    {
        public static void Prefix()
        {
            Pawn_DrawTracker_Patch.skipOffset = true;
        }

        public static void Postfix()
        {
            Pawn_DrawTracker_Patch.skipOffset = false;
        }
    }

    public static partial class HarmonyPatches
    {
        [HarmonyPatch(typeof(Graphic), "MeshAt")]
        public static class Graphic_MeshAt
        {
            public static void Prefix(ref Vector2 ___drawSize, out Vector2 __state)
            {
                __state = ___drawSize;

                if (BigSmall.activePawn == null)
                    return;
                
                // Only scale animals using this method.
                if (BigSmall.activePawn.RaceProps.Humanlike) return;
                if (!BigSmallMod.settings.scaleAnimals) return;

                var sizeCache = HumanoidPawnScaler.GetBSDict(BigSmall.activePawn);
                if (sizeCache != null)
                {
                    float variedBodySize = sizeCache.cosmeticScaleMultiplier.linear;

                    ___drawSize = new Vector2(___drawSize.x * variedBodySize, ___drawSize.y * variedBodySize);
                }
            }

            public static void Postfix(ref Vector2 ___drawSize, Vector2 __state)
            {
                ___drawSize = __state;
            }
        }
    }

}
