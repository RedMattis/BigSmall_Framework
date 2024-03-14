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
        public static void Postfix(PawnRenderer __instance, ref Vector3 __result)
        {
            FieldInfo field = GetPawnField();
            Pawn pawn = (Pawn)field.GetValue(__instance);
            if (pawn != null)
            {
                var sizeCache = HumanoidPawnScaler.GetBSDict(pawn);
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
        public static FieldInfo pawnFieldInfo = null;
        private static FieldInfo GetPawnField()
        {
            if (pawnFieldInfo == null)
            {
                pawnFieldInfo = typeof(PawnRenderer).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return pawnFieldInfo;
        }
    }


    //[HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
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
        static readonly float lifestageFactor = 1.5f;

        /// <summary>
        /// Body
        /// </summary>
        [HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn))]
        public static class HumanlikeMeshPoolUtility_GetHumanlikeBodySetForPawnPatch
        {
            public static void Prefix(ref GraphicMeshSet __result, Pawn pawn, ref float wFactor, ref float hFactor)
            {
                //float factor = lifestageFactor;
                //if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
                //{
                //    factor = pawn.ageTracker.CurLifeStage.bodyWidth.Value;
                //}
                float factor = HumanoidPawnScaler.GetBSDict(pawn).bodyRenderSize;
                if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                {
                    factor *= VEPawnData.bodyRenderSize;
                }
                wFactor *= factor;
                hFactor *= factor;
            }
        }

        /// <summary>
        /// Head
        /// </summary>
        [HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn))]
        public static class HumanlikeMeshPoolUtility_HumanlikeMeshPoolUtilityPatch
        {
            public static void Prefix(ref GraphicMeshSet __result, Pawn pawn, ref float wFactor, ref float hFactor)
            {
                //float factor = lifestageFactor;
                //if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
                //{
                //    factor = pawn.ageTracker.CurLifeStage.bodyWidth.Value;
                //}
                float factor = HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;

                if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                {
                    factor *= VEPawnData.headRenderSize;
                }

                wFactor *= factor;
                hFactor *= factor;
            }
        }

        /// <summary>
        /// Hair
        /// </summary>
        [HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn))]
        public static class HumanlikeMeshPoolUtility_GetHumanlikeHairSetForPawPatch
        {
            public static void Prefix(ref GraphicMeshSet __result, Pawn pawn, ref float wFactor, ref float hFactor)
            {

                //Vector2 hairMeshSize = pawn.story.headType.hairMeshSize;
                //if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.headSizeFactor.HasValue)
                //{
                //    hairMeshSize *= pawn.ageTracker.CurLifeStage.headSizeFactor.Value;
                //}
                float hairMeshSize = HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;
                if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                {
                    hairMeshSize *= VEPawnData.headRenderSize;
                }
                wFactor *= hairMeshSize;
                hFactor *= hairMeshSize;
            }
        }

        /// <summary>
        /// Beard
        /// </summary>
        [HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBeardSetForPawn))]
        public static class HumanlikeMeshPoolUtility_GetHumanlikeBeardSetForPawnPatch
        {
            
            public static void Prefix(ref GraphicMeshSet __result, Pawn pawn, ref float wFactor, ref float hFactor)
            {
                //Vector2 hairMeshSize = pawn.story.headType.hairMeshSize;
                //if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.headSizeFactor.HasValue)
                //{
                //    hairMeshSize *= pawn.ageTracker.CurLifeStage.headSizeFactor.Value;
                //}
                float hairMeshSize = HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;
                if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                {
                    hairMeshSize *= VEPawnData.headRenderSize;
                }
                wFactor *= hairMeshSize;
                hFactor *= hairMeshSize;
            }
        }
    }
}
