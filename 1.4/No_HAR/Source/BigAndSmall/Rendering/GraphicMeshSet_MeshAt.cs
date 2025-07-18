﻿using HarmonyLib;
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
