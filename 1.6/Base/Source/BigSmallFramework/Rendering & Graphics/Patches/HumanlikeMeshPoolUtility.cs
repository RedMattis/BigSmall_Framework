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

            if (pawn.GetCachePrepatchedThreaded() is BSCache sizeCache)
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
        public struct PGPRRCache
        {
            public Pawn pawn;
            public BSCache cache;
            public bool cachingDisabled;
            public bool doOffset;
            public bool doComplexBodyOffset;
            public bool spawned;
            public Rot4 rotation;
            public int tick10;
            public uint changeIndex; // Used to check if the cache has changed since the last time this was run.
        }
        [ThreadStatic]
        static PGPRRCache threadStaticCache;

        public static bool skipOffset = false;

        private static bool SpawnedOrVisible(Pawn pawn)
        {
            return pawn.Spawned || pawn.ParentHolder is PawnFlyer;
        }

        public static void Prefix(PawnRenderer __instance, ref Vector3 drawLoc, Rot4? rotOverride, bool neverAimWeapon, ref bool disableCache, Pawn ___pawn)
        {
            if (___pawn == null) return;
            bool requestNewCache = threadStaticCache.pawn != ___pawn || threadStaticCache.cache.IsTempCache;
            if (requestNewCache)
            {
                threadStaticCache.cache = ___pawn.GetCachePrepatchedThreaded();
                threadStaticCache.pawn = ___pawn;
                if (!threadStaticCache.cache.approximatelyNoChange)
                {
                    threadStaticCache.spawned = SpawnedOrVisible(___pawn);
                }
            }
            if (threadStaticCache.cache.approximatelyNoChange || !threadStaticCache.spawned)
            {
                return;
            }
            var rotInt = ___pawn.rotationInt;
            if (requestNewCache || BS.Tick10 != threadStaticCache.tick10 || threadStaticCache.rotation != rotInt)
            {
                threadStaticCache.tick10 = BS.Tick10;
                threadStaticCache.cachingDisabled = (!disableCache && BigSmallMod.settings.disableTextureCaching) &&
                    (threadStaticCache.cache.totalSizeOffset > 0 || threadStaticCache.cache.scaleMultiplier.linear > 1 || threadStaticCache.cache.renderCacheOff);
                threadStaticCache.doOffset = BigSmallMod.settings.offsetBodyPos && ___pawn.GetPosture() == PawnPosture.Standing &&
                    (BigSmallMod.settings.offsetAnimalBodyPos || ___pawn.RaceProps?.Humanlike == true);
                threadStaticCache.doComplexBodyOffset = threadStaticCache.cache.complexBodyOffsets != null;
                threadStaticCache.rotation = rotOverride ?? ___pawn.Rotation;
            }

            // If caching disabled...
            if (threadStaticCache.cachingDisabled)
            {
                disableCache = true;
            }
            // Offset pawn upwards if the option is enabled.
            if (!skipOffset)
            {
                if (threadStaticCache.doOffset)
                {
                    drawLoc.z += threadStaticCache.cache.worldspaceOffset;
                }
                if (threadStaticCache.doComplexBodyOffset)
                {
                    // rotate the offset based on the orientation.
                    switch (rotInt.AsInt)
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
            public int tick10;
        }
        [ThreadStatic]
        static PGPRRCache threadStaticCache;

        [HarmonyPostfix]
        public static void CurRotDrawModePostfix(PawnRenderer __instance, ref RotDrawMode __result, Pawn ___pawn)
        {
            if (___pawn == null) return;

            if (threadStaticCache.pawn != ___pawn || threadStaticCache.tick10 != BS.Tick10)
            {
                threadStaticCache.cache = ___pawn.GetCachePrepatchedThreaded();
                threadStaticCache.pawn = ___pawn;
                threadStaticCache.hasForcedRotDrawMode = threadStaticCache.cache.forcedRotDrawMode.HasValue;
                threadStaticCache.rotDrawMode = threadStaticCache.cache.forcedRotDrawMode ?? RotDrawMode.Fresh;
                threadStaticCache.tick10 = BS.Tick10;
            }
            if (!threadStaticCache.hasForcedRotDrawMode) return;
            else
            {
                __result = threadStaticCache.rotDrawMode;
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
        //public struct PerThreadMiniCache
        //{
        //    public Pawn pawn;
        //    public BSCache cache;
        //    public uint changeIndex;
        //}
        //[ThreadStatic]
        //static PerThreadMiniCache threadStaticCache;

        [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
        [HarmonyPostfix]
        public static void ScaleForPatch(ref Vector3 __result, PawnRenderNode node, PawnDrawParms parms)
        {
            var pawn = parms.pawn;
            if (pawn == null)
            {
                return;
            }
            var cache = pawn.GetCachePrepatchedThreaded();
            //if (cache.changeIndex != threadStaticCache.changeIndex || threadStaticCache.pawn != pawn)
            //{
            //    threadStaticCache.cache = cache;
            //    threadStaticCache.pawn = pawn;
            //}
            //var cache = threadStaticCache.cache;
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
                        double headRenderSizeD = cache.headRenderSize;
                        __result.x = (float)(resultX * headRenderSizeD);
                        __result.z = (float)(resultZ * headRenderSizeD);
                    }
                    else if (node is PawnRenderNode_HAnimalPart)
                    {
                        __result.x = (float)(resultX * bodyRenderSizeD);
                        __result.z = (float)(resultZ * bodyRenderSizeD);
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
