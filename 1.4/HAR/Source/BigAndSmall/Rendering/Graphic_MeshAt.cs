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
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class RenderPawnAt_Patch
    {
        public static void Prefix(ref Vector3 drawLoc, Pawn ___pawn)
        {
            if (BigSmallMod.settings.offsetBodyPos && ___pawn.GetPosture() == PawnPosture.Standing)
            {
                if (___pawn?.RaceProps?.Humanlike == true)
                {
                    var cache = HumanoidPawnScaler.GetBSDict(___pawn);
                    var factor = cache.bodyRenderSize;
                    var originalFactor = factor;
                    if (factor < 1) { factor = 1; }
                    float offsetFromCache = cache.bodyPosOffset;

                    drawLoc.z += (factor - 1) / 2 * (offsetFromCache + 1) + offsetFromCache * 0.25f * (originalFactor < 1 ? originalFactor : 1) * ___pawn.story.bodyType.bodyGraphicScale.y;
                }
            }
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
