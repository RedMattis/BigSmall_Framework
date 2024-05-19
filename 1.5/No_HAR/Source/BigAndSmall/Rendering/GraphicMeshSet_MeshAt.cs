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
    }

}
