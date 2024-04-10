using HarmonyLib;
using UnityEngine;
using Verse;

//namespace BigAndSmall
//{
//    public static partial class HarmonyPatches
//    {
//        [HarmonyPatch(typeof(GraphicMeshSet), nameof(GraphicMeshSet.MeshAt))]
//        public static class GraphicMeshSet_MeshAt
//        {
//            public static void Postfix(GraphicMeshSet __instance, ref Mesh __result, Rot4 rot)
//            {
//                //// We PostFix their method instead if it exists.

//                if (BigSmall.performScaleCalculations
//                && BigSmall.humnoidScaler != null
//                && BigSmall.activePawn != null)
//                {
//                    __result = BigSmall.GetPawnMesh(HumanoidPawnScaler.GetBodyRenderSize(out _, BigSmall.activePawn), rot.AsInt == 3);
//                    //if (HasVFE)
//                    //{
//                    //    //BigSmall.humnoidScaler.GetBodyRenderSize(BigSmall.activePawn, setVEFBody: true);
//                    //}
//                    //else
//                    //{
//                    //var sizeCache = HumanoidPawnScaler.GetPawnSizeDict(BigSmall.activePawn);
//                    //if (sizeCache != null)

//                    //}
//                }

//            }
//        }
//    }
//}

namespace BigAndSmall
{
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

                if (HumanoidPawnScaler.GetBSDict(BigSmall.activePawn) is BSCache sizeCache)
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

        //[HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.OffsetFor))]
        //public static class DrawData_OffsetForRot_Patch
        //{
        //    [HarmonyPostfix]
        //    public static void MultiplyOffset(ref Vector3 __result, PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms, Vector3 pivot)
        //    {
        //        var pawn = node?.tree?.pawn;
        //        if (__result.magnitude > 0 && HumanoidPawnScaler.GetBSDict(pawn) is BSCache sizeCache)
        //        {
        //            // Commented out because we're using PawnRenderNodeWorker.ScaleFor at the moment.
        //            //__result.x *= sizeCache.bodyRenderSize;
        //            //__result.z *= sizeCache.bodyRenderSize;

        //            __result.x *= pawn?.story?.bodyType?.bodyGraphicScale.x ?? 1;
        //            __result.z *= pawn?.story?.bodyType?.bodyGraphicScale.y ?? 1;
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(DrawData), nameof(DrawData.OffsetForRot))]
        //public static class DrawData_OffsetForRot_Patch
        //{
        //    [HarmonyPostfix]
        //    public static void MultiplyOffset(ref Vector3 __result, DrawData __instance)
        //    {
        //        Log.Message($"Graphic_DrawOffset_Patch... {HumanoidPawnScaler.GetBSDict(BigSmall.activePawn)}");
        //        if (__result.magnitude > 0 && HumanoidPawnScaler.GetBSDict(BigSmall.activePawn) is BSCache sizeCache)
        //        {
        //            Log.Message($"Offset for {BigSmall.activePawn} was {__result.x}, {__result.y}, {__result.z}");
        //            __result *= sizeCache.bodyRenderSize;
        //            Log.Message($"Offset {BigSmall.activePawn} for is now: {__result.x}, {__result.y}, {__result.z}");

        //        }
        //    }
        //}
    }

}
