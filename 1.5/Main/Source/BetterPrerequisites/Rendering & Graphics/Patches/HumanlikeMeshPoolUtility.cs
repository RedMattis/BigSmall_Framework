using HarmonyLib;
using System;
using UnityEngine;
using Verse;
using RimWorld;
using static Verse.PawnRenderer;
using System.Runtime;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BaseHeadOffsetAt))]
    public static class PawnRenderer_BaseHeadOffsetAt
    {
        public static void Postfix(PawnRenderer __instance, ref Vector3 __result, ref Rot4 rotation)
        {
            Pawn pawn = __instance.pawn;

            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn, canRegenerate: false) is BSCache sizeCache)
            {
                __result = new Vector3(__result.x * sizeCache.headPositionMultiplier, __result.y, __result.z * sizeCache.headPositionMultiplier);

                if (sizeCache.hasComplexHeadOffsets && __instance.results.parms is PawnDrawParms pdp && !pdp.flags.HasFlag(PawnRenderFlags.Portrait))
                {
                    var rot = rotation.AsInt;
                    Vector3 headPosOffset = rot switch
                    {
                        0 => sizeCache.complexHeadOffsets[0],
                        1 => sizeCache.complexHeadOffsets[1],
                        2 => sizeCache.complexHeadOffsets[2],
                        3 => sizeCache.complexHeadOffsets[3],
                        _ => Vector3.zero
                    };
                    __result += headPosOffset;
                }
            }
            if (pawn == null)
            {
                Log.Warning($"PawnRenderer_BaseHeadOffsetAt: pawn is null ({__instance}");
            }
            //}
        }
    }


    //[HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    public static class ParallelGetPreRenderResults_Patch
    {
        static readonly int maxUses = 100;
        public struct PGPRRCache
        {
            public Pawn pawn;
            public BSCache cache;
            public bool approxNoChange;
            public bool cachingDisabled;
            public bool doOffset;
            public bool doComplexBodyOffset;
            public bool doComplexHeadOffset;
            public bool spawned;
            public Rot4 rotation;
            public Rot4 lastRot;
            public int lastRotAsInt;
            public int uses;
        }
        [ThreadStatic]
        static PGPRRCache threadStaticCache;

        public static bool skipOffset = false;

        public static void Prefix(PawnRenderer __instance, ref Vector3 drawLoc, Rot4? rotOverride, bool neverAimWeapon, ref bool disableCache, Pawn ___pawn)
        {
            if (___pawn == null) return;

            if (threadStaticCache.pawn != ___pawn || threadStaticCache.rotation != ___pawn.rotationInt || threadStaticCache.uses > maxUses)
            {
                threadStaticCache.cache = HumanoidPawnScaler.GetCacheUltraSpeed(___pawn, canRegenerate: false);
                threadStaticCache.pawn = ___pawn;
                threadStaticCache.approxNoChange = threadStaticCache.cache.approximatelyNoChange;
                if (!threadStaticCache.approxNoChange)
                {
                    var posture = ___pawn.GetPosture();
                    threadStaticCache.cachingDisabled = (!disableCache && BigSmallMod.settings.disableTextureCaching) &&
                        (threadStaticCache.cache.totalSizeOffset > 0 || threadStaticCache.cache.scaleMultiplier.linear > 1 || threadStaticCache.cache.renderCacheOff);
                    threadStaticCache.doOffset = BigSmallMod.settings.offsetBodyPos && ___pawn.GetPosture() == PawnPosture.Standing &&
                        (BigSmallMod.settings.offsetAnimalBodyPos || ___pawn.RaceProps?.Humanlike == true);
                    threadStaticCache.doComplexHeadOffset = threadStaticCache.cache.complexHeadOffsets != null;
                    threadStaticCache.doComplexBodyOffset = threadStaticCache.cache.complexBodyOffsets != null;
                    threadStaticCache.rotation = rotOverride ?? ___pawn.Rotation;
                    threadStaticCache.lastRot = rotOverride ?? ((posture == PawnPosture.Standing || ___pawn.Crawling) ? threadStaticCache.rotation : __instance.LayingFacing());
                    threadStaticCache.lastRotAsInt = threadStaticCache.lastRot.AsInt;
                    threadStaticCache.spawned = ___pawn.Spawned;
                    threadStaticCache.uses = 0;
                }
            }
            threadStaticCache.uses++;
            if (threadStaticCache.approxNoChange || !threadStaticCache.spawned) return;

            // If caching disabled...
            if (threadStaticCache.cachingDisabled)
            {
                disableCache = true;
            }
            // Offset pawn upwards if the option is enabled.
            if (!skipOffset && threadStaticCache.doOffset)
            {
                drawLoc.z += threadStaticCache.cache.worldspaceOffset;
            }
            if (!skipOffset && threadStaticCache.doComplexBodyOffset)
            {
                // Get the rotation.
                
                Rot4 orientation = threadStaticCache.rotation;
                // rotate the offset based on the orientation.
                switch (orientation.AsInt)
                {
                    case 0:
                        drawLoc += threadStaticCache.cache.complexBodyOffsets[0];
                        break;
                    case 1:
                        drawLoc += threadStaticCache.cache.complexBodyOffsets[1];
                        break;
                    case 2:
                        drawLoc += threadStaticCache.cache.complexBodyOffsets[2];
                        break;
                    case 3:
                        drawLoc += threadStaticCache.cache.complexBodyOffsets[3];
                        break;
                }
            }
            
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.CurRotDrawMode), MethodType.Getter)]
    public static class RotDrawModePatch
    {
        static readonly int maxUses = 1000;
        public struct PGPRRCache
        {
            public Pawn pawn;
            public BSCache cache;
            public Rot4 lastRot;
            public bool hasForcedRotDrawMode;
            public RotDrawMode rotDrawMode;
            public int uses;
        }
        [ThreadStatic]
        static PGPRRCache threadStaticCache;

        [HarmonyPostfix]
        public static void CurRotDrawModePostfix(PawnRenderer __instance, ref RotDrawMode __result, Pawn ___pawn)
        {
            if (___pawn == null) return;

            if (threadStaticCache.pawn != ___pawn || threadStaticCache.uses > maxUses)
            {
                threadStaticCache.cache = HumanoidPawnScaler.GetCacheUltraSpeed(___pawn, canRegenerate: false);
                threadStaticCache.pawn = ___pawn;
                threadStaticCache.hasForcedRotDrawMode = threadStaticCache.cache.forcedRotDrawMode.HasValue;
                threadStaticCache.rotDrawMode = threadStaticCache.cache.forcedRotDrawMode ?? RotDrawMode.Fresh;
                threadStaticCache.uses = 0;
            }
            if (!threadStaticCache.hasForcedRotDrawMode) return;
            else
            {
                __result = threadStaticCache.rotDrawMode;
            }
            threadStaticCache.uses++;
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

        [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
        [HarmonyPostfix]
        public static void ScaleForPatch(ref Vector3 __result, PawnRenderNode node, PawnDrawParms parms)
        {
            var pawn = parms.pawn;
            if (pawn == null)
            {
                return;
            }
            if (threadStaticCache.pawn != pawn)
            {
                threadStaticCache.cache = HumanoidPawnScaler.GetCache(pawn, canRegenerate: false);
                threadStaticCache.pawn = pawn;
            }
            var cache = threadStaticCache.cache;
            if (cache.approximatelyNoChange) return;

            // Tiny performance win because Unity Casts all float multiplications to double.
            double bodyRenderSizeD = cache.bodyRenderSize;
            double resultX = __result.x;
            double resultZ = __result.z;
            
            if (node.parent != null && node.parent.props.tagDef == BSDefs.Root)
            {
                if (cache.isHumanlike)
                {
                    if (node is PawnRenderNode_Body)
                    {
                        __result.x = (float)(resultX * bodyRenderSizeD);
                        __result.z = (float)(resultZ * bodyRenderSizeD);
                    }
                    else if (node is PawnRenderNode_Head)
                    {
                        double headerRenderSizeD = cache.headRenderSize;
                        __result.x = (float)(resultX * headerRenderSizeD);
                        __result.z = (float)(resultZ * headerRenderSizeD);
                    }
                }
                else
                {
                    __result.x = (float)(resultX * bodyRenderSizeD);
                    __result.z = (float)(resultZ * bodyRenderSizeD);
                }
            }

            //if (cache.isHumanlike)
            //{
            //    if (node.parent is PawnRenderNode_Parent && node.parent.props.tagDef == BSDefs.Root)
            //    {
            //        __result.x = (float)(resultX * bodyRenderSizeD);
            //        __result.z = (float)(resultZ * bodyRenderSizeD);
            //    }
            //    else if (node is PawnRenderNode_Head)
            //    {
            //        double headerRenderSizeD = cache.headRenderSize;
            //        __result.x = (float)(resultX * headerRenderSizeD);
            //        __result.z = (float)(resultZ * headerRenderSizeD);
            //    }
            //}
            
        }
    }
}
