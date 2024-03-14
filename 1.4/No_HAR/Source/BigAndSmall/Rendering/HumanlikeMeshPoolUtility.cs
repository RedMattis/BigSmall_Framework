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
                    float offset = GetOffset(___pawn);

                    __result.z += offset;
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
                if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                {
                    factor *= VEPawnData.bodyRenderSize;
                }
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

                if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                {
                    factor *= VEPawnData.headRenderSize;
                }


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
                if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                {
                    hairMeshSize *= VEPawnData.headRenderSize;
                }
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
                if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                {
                    hairMeshSize *= VEPawnData.headRenderSize;
                }
                __result = MeshPool.GetMeshSetForWidth(hairMeshSize.x, hairMeshSize.y);
            }
        }
    }
}
