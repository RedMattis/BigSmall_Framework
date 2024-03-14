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
    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    public static class Pawn_DrawTracker_Patch
    {
        public static bool skipOffset = false;

        public static void Prefix(PawnRenderer __instance, ref Vector3 drawLoc, Rot4 rotOverride, bool neverAimWeapon, ref bool disableCache, Pawn ___pawn)
        {
            // If caching disabled...
            if (BigSmallMod.settings.disableTextureCaching)
            {
                if (FastAcccess.GetCache(___pawn) is BSCache cache)
                {
                    if (cache.sizeOffset > 0 || cache.scaleMultiplier.linear > 1)
                    {
                        disableCache = true;
                    }
                }
            }

            // Offset pawn upwards if the option is enabled.
            if (!skipOffset
                && BigSmallMod.settings.offsetBodyPos
                && ___pawn.GetPosture() == PawnPosture.Standing)
            {
                if (___pawn?.RaceProps?.Humanlike == true)
                {
                    float offset = GetOffset(___pawn);

                    drawLoc.z += offset;
                }
            }
        }

        public static float GetOffset(Pawn ___pawn)
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

            return (factor - 1) / 2 * (offsetFromCache + 1) + offsetFromCache * 0.30f * (originalFactor < 1 ? originalFactor : 1) * bodyType.bodyGraphicScale.y;
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
