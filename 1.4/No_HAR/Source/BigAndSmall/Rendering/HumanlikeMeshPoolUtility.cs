using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(PawnRenderer), "BaseHeadOffsetAt")]
    public static class PawnRenderer_BaseHeadOffsetAt
    {
        public static void Postfix(ref Vector3 __result)
        {
            if (BigSmall.activePawn != null)
            {
                var sizeCache = HumanoidPawnScaler.GetBSDict(BigSmall.activePawn);
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
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class RenderPawnAt_Patch
    {
        public static void Prefix(ref Vector3 drawLoc, Pawn ___pawn)
        {
            if (BigSmallMod.settings.offsetBodyPos && ___pawn.GetPosture() == PawnPosture.Standing)
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

            return (factor - 1) / 2 * (offsetFromCache + 1) + offsetFromCache * 0.25f * (originalFactor < 1 ? originalFactor : 1) * ___pawn.story.bodyType.bodyGraphicScale.y;
        }
    }

    public static partial class HarmonyPatches
    {
        static readonly float lifestageFactor = 1.5f;

        /// <summary>
        /// Body
        /// </summary>
        [HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn))]
        public static class HumanlikeMeshPoolUtility_GetHumanlikeBodySetForPawnPatch
        {
            public static void Postfix(ref GraphicMeshSet __result, Pawn pawn)
            {
                float factor = lifestageFactor;
                if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
                {
                    factor = pawn.ageTracker.CurLifeStage.bodyWidth.Value;
                }
                factor *= HumanoidPawnScaler.GetBSDict(pawn).bodyRenderSize;

                __result = MeshPool.GetMeshSetForWidth(factor);
            }
        }

        /// <summary>
        /// Head
        /// </summary>
        [HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn))]
        public static class HumanlikeMeshPoolUtility_HumanlikeMeshPoolUtilityPatch
        {
            public static void Postfix(ref GraphicMeshSet __result, Pawn pawn)
            {
                float factor = lifestageFactor;
                if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
                {
                    factor = pawn.ageTracker.CurLifeStage.bodyWidth.Value;
                }
                factor *= HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;

                __result = MeshPool.GetMeshSetForWidth(factor);
            }
        }

        /// <summary>
        /// Hair
        /// </summary>
        [HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn))]
        public static class HumanlikeMeshPoolUtility_GetHumanlikeHairSetForPawPatch
        {
            public static void Postfix(ref GraphicMeshSet __result, Pawn pawn)
            {
                Vector2 hairMeshSize = pawn.story.headType.hairMeshSize;
                if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.headSizeFactor.HasValue)
                {
                    hairMeshSize *= pawn.ageTracker.CurLifeStage.headSizeFactor.Value;
                }
                hairMeshSize *= HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;
                __result = MeshPool.GetMeshSetForWidth(hairMeshSize.x, hairMeshSize.y);
            }
        }

        /// <summary>
        /// Beard
        /// </summary>
        [HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBeardSetForPawn))]
        public static class HumanlikeMeshPoolUtility_GetHumanlikeBeardSetForPawnPatch
        {
            
            public static void Postfix(ref GraphicMeshSet __result, Pawn pawn)
            {
                Vector2 hairMeshSize = pawn.story.headType.hairMeshSize;
                if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.headSizeFactor.HasValue)
                {
                    hairMeshSize *= pawn.ageTracker.CurLifeStage.headSizeFactor.Value;
                }
                hairMeshSize *= HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;
                __result = MeshPool.GetMeshSetForWidth(hairMeshSize.x, hairMeshSize.y);
            }
        }
    }
}
