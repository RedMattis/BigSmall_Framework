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
using Verse.Noise;
using System.Threading;
using System.Collections.Concurrent;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(PawnRenderer), "BaseHeadOffsetAt")]
    public static class PawnRenderer_BaseHeadOffsetAt
    {
        public static void Postfix(PawnRenderer __instance, ref Vector3 __result)
        {
            Pawn pawn = GetPawnFromRef(__instance);
            //if (pawn != null)
            //{
                //float factorFromVEF = 1;
                //if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
                //{
                //    factorFromVEF *= VEPawnData.bodyRenderSize;
                //}

            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn, canRegenerate: false) is BSCache sizeCache)
            {
                __result = new Vector3(__result.x * sizeCache.headPositionMultiplier, __result.y, __result.z * sizeCache.headPositionMultiplier);
            }
            if (pawn == null)
            {
                Log.Warning($"PawnRenderer_BaseHeadOffsetAt: pawn is null ({__instance}");
            }
            //}
        }

        

        public static AccessTools.FieldRef<PawnRenderer, Pawn> pawnFieldRef = null;
        private static Pawn GetPawnFromRef(PawnRenderer __instance)
        {
            pawnFieldRef ??= AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");
            return pawnFieldRef(__instance);
        }
    }


    //[HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    public static class ParallelGetPreRenderResults_Patch
    {
        public struct PGPRRCache
        {
            public Pawn pawn;
            public BSCache cache;
            public bool approxNoChange;
            public bool cachingDisabled;
            public bool doOffset;
            public bool spawned;
        }
        [ThreadStatic]
        static PGPRRCache threadStaticCache;

        public static bool skipOffset = false;

        public static void Prefix(PawnRenderer __instance, ref Vector3 drawLoc, Rot4 rotOverride, bool neverAimWeapon, ref bool disableCache, Pawn ___pawn)
        {
            if (___pawn == null) return;

            if (threadStaticCache.pawn != ___pawn)
            {
                threadStaticCache.cache = HumanoidPawnScaler.GetCacheUltraSpeed(___pawn, canRegenerate: false);
                threadStaticCache.pawn = ___pawn;
                threadStaticCache.approxNoChange = threadStaticCache.cache.approximatelyNoChange;
                if (!threadStaticCache.approxNoChange)
                {
                    threadStaticCache.cachingDisabled = (!disableCache && BigSmallMod.settings.disableTextureCaching) &&
                        (threadStaticCache.cache.totalSizeOffset > 0 || threadStaticCache.cache.scaleMultiplier.linear > 1 || threadStaticCache.cache.renderCacheOff);
                    threadStaticCache.doOffset = !skipOffset && BigSmallMod.settings.offsetBodyPos && ___pawn.GetPosture() == PawnPosture.Standing && ___pawn.RaceProps?.Humanlike == true;
                    threadStaticCache.spawned = ___pawn.Spawned;
                }
            }
            if (threadStaticCache.approxNoChange || !threadStaticCache.spawned) return;

            // If caching disabled...
            if (threadStaticCache.cachingDisabled)
            {
                disableCache = true;
            }
            // Offset pawn upwards if the option is enabled.
            if (threadStaticCache.doOffset)
            {
                drawLoc.z += threadStaticCache.cache.worldspaceOffset;
            }
        }
    }

    [HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.DrawSelectionBracketFor))]
    public static class SelectionDrawer_DrawSelection_Patch
    {
        public static void Prefix(object obj)
        {
            ParallelGetPreRenderResults_Patch.skipOffset = true;
        }

        public static void Postfix(object obj)
        {
            ParallelGetPreRenderResults_Patch.skipOffset = false;
        }
    }

    [HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
    public static class PawnUIOverlay_DrawSelection_Patch
    {
        public static void Prefix()
        {
            ParallelGetPreRenderResults_Patch.skipOffset = true;
        }

        public static void Postfix()
        {
            ParallelGetPreRenderResults_Patch.skipOffset = false;
        }
    }

    [HarmonyPatch]
    public static class RenderingPatches
    {
        static readonly float lifestageFactor = 1.5f;

        /// <summary>
        /// Body
        /// </summary>
        //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn))]
        //public static class HumanlikeMeshPoolUtility_GetHumanlikeBodySetForPawnPatch
        //{
        //    public static void Prefix(ref GraphicMeshSet __result, Pawn pawn, ref float wFactor, ref float hFactor)
        //    {
        //        //float factor = lifestageFactor;
        //        //if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
        //        //{
        //        //    factor = pawn.ageTracker.CurLifeStage.bodyWidth.Value;
        //        //}
        //        float factor = HumanoidPawnScaler.GetBSDict(pawn).bodyRenderSize;
        //        //if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
        //        //{
        //        //    factor *= VEPawnData.bodyRenderSize;
        //        //}
        //        wFactor *= factor;
        //        hFactor *= factor;
        //    }
        //}

        ///// <summary>
        ///// Head
        ///// </summary>
        //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn))]
        //public static class HumanlikeMeshPoolUtility_HumanlikeMeshPoolUtilityPatch
        //{
        //    public static void Prefix(ref GraphicMeshSet __result, Pawn pawn, ref float wFactor, ref float hFactor)
        //    {
        //        //float factor = lifestageFactor;
        //        //if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.bodyWidth.HasValue)
        //        //{
        //        //    factor = pawn.ageTracker.CurLifeStage.bodyWidth.Value;
        //        //}
        //        float factor = HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;

        //        //if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
        //        //{
        //        //    factor *= VEPawnData.headRenderSize;
        //        //}

        //        wFactor *= factor;
        //        hFactor *= factor;
        //    }
        //}

        ///// <summary>
        ///// Hair
        ///// </summary>
        //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn))]
        //public static class HumanlikeMeshPoolUtility_GetHumanlikeHairSetForPawPatch
        //{
        //    public static void Prefix(ref GraphicMeshSet __result, Pawn pawn, ref float wFactor, ref float hFactor)
        //    {

        //        //Vector2 hairMeshSize = pawn.story.headType.hairMeshSize;
        //        //if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.headSizeFactor.HasValue)
        //        //{
        //        //    hairMeshSize *= pawn.ageTracker.CurLifeStage.headSizeFactor.Value;
        //        //}
        //        float hairMeshSize = HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;
        //        //if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
        //        //{
        //        //    hairMeshSize *= VEPawnData.headRenderSize;
        //        //}
        //        wFactor *= hairMeshSize;
        //        hFactor *= hairMeshSize;
        //    }
        //}

        ///// <summary>
        ///// Beard
        ///// </summary>
        //[HarmonyPatch(typeof(HumanlikeMeshPoolUtility), nameof(HumanlikeMeshPoolUtility.GetHumanlikeBeardSetForPawn))]
        //public static class HumanlikeMeshPoolUtility_GetHumanlikeBeardSetForPawnPatch
        //{

        //    public static void Prefix(ref GraphicMeshSet __result, Pawn pawn, ref float wFactor, ref float hFactor)
        //    {
        //        //Vector2 hairMeshSize = pawn.story.headType.hairMeshSize;
        //        //if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.headSizeFactor.HasValue)
        //        //{
        //        //    hairMeshSize *= pawn.ageTracker.CurLifeStage.headSizeFactor.Value;
        //        //}
        //        float hairMeshSize = HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;
        //        //if (BigSmallLegacy.VEFActive && VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper VEPawnData))
        //        //{
        //        //    hairMeshSize *= VEPawnData.headRenderSize;
        //        //}
        //        wFactor *= hairMeshSize;
        //        hFactor *= hairMeshSize;
        //    }
        //}
        public struct PerThreadMiniCache
        {
            public Pawn pawn;
            public BSCache cache;
        }
        [ThreadStatic]
        static PerThreadMiniCache threadStaticCache;

        //static readonly ConcurrentDictionary<int, PerThreadMiniCache> threadDictCache = [];
        //static readonly ThreadLocal<PerThreadMiniCache> threadLocalCache = new(() => new PerThreadMiniCache());
        // This WORKS, but maybe it is a bit too aggresive since it patches everything? Using it for the time being just so it is the same as VEF.
        [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
        [HarmonyPostfix]
        public static void ScaleForPatch(ref Vector3 __result, PawnRenderNode node, PawnDrawParms parms)
        {
            var pawn = parms.pawn;
            if (pawn == null)
            {
                //Log.Error($"PawnRenderNodeWorker.ScaleFor was called with a null pawn! {parms}");
                return;
            }
            if (threadStaticCache.pawn != pawn)
            {
                threadStaticCache.cache = HumanoidPawnScaler.GetCache(pawn, canRegenerate: false);
                threadStaticCache.pawn = pawn;
            }
            var cache = threadStaticCache.cache;
            if (cache.approximatelyNoChange) return;
            if (cache.isHumanlike)
            {
                if (node is PawnRenderNode_Body)
                {
                    __result *= cache.bodyRenderSize;
                }
                else if (node is PawnRenderNode_Head)
                {
                    __result *= cache.headRenderSize;
                }
            }
            else
            {
                __result *= cache.bodyRenderSize;
            }
        }
    }
}
