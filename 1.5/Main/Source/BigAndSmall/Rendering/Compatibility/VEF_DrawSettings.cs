//using BigAndSmall;
//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Reflection;
//using UnityEngine;
//using Verse;

//namespace BigAndSmall
//{
//    //This works perfectly well, but isn't used anywhere...
//    [HarmonyPatch]
//    public static class VFECore_PawnDataCache_Patch
//    {
//        public static bool VFE_Loaded = false;
//        private static FieldInfo headRenderSizeField = null;
//        private static FieldInfo bodyRenderSizeField = null;
//        private static FieldInfo pawnField = null;

//        private static readonly string[] VEF_GetPawnDataCachePatches = new string[]
//        {
//        "VFECore.CachedPawnData:RegenerateCache",
//        };

//        public static bool Prepare()
//        {
//            string[] vlfa_methods = VEF_GetPawnDataCachePatches;
//            for (int i = 0; i < vlfa_methods.Length; i++)
//            {
//                if (!(AccessTools.Method(vlfa_methods[i], new Type[] { }) == null))
//                {
//                    VFE_Loaded = true;
//                    return true;
//                }
//            }
//            return false;
//        }

//        public static IEnumerable<MethodBase> TargetMethods()
//        {
//            string[] vlfa_methods = VEF_GetPawnDataCachePatches;
//            for (int i = 0; i < vlfa_methods.Length; i++)
//            {
//                MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i], new Type[] { });
//                if (!(methodInfo == null))
//                    yield return methodInfo;
//            }
//        }

//        public static void Postfix(ref object __instance)
//        {
//            if (__instance == null)
//                return;

//            if (pawnField == null)
//                pawnField = __instance.GetType().GetField("pawn");

//            if (pawnField.GetValue(__instance) is Pawn pawn)
//            {

//                if (headRenderSizeField == null)
//                    headRenderSizeField = __instance.GetType().GetField("headRenderSize");
//                if (bodyRenderSizeField == null)
//                    bodyRenderSizeField = __instance.GetType().GetField("bodyRenderSize");

//                float headRenderSize = (float)headRenderSizeField.GetValue(__instance);
//                float bodyRenderSize = (float)bodyRenderSizeField.GetValue(__instance);

//                // Try add to cache, or update is existing.
//                if (VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper wrapper))
//                {
//                    wrapper.bodyRenderSize = bodyRenderSize;
//                    wrapper.headRenderSize = headRenderSize;
//                }
//                else
//                {
//                    VEF_CachedPawnDataWrapper.CachedPawnData.Add(pawn, new VEF_CachedPawnDataWrapper(headRenderSize, bodyRenderSize));
//                }
//            }
//        }
//    }

//    public class VEF_CachedPawnDataWrapper
//    {
//        public static Dictionary<Pawn, VEF_CachedPawnDataWrapper> CachedPawnData = new Dictionary<Pawn, VEF_CachedPawnDataWrapper>();
//        public float headRenderSize;
//        public float bodyRenderSize;
//        public VEF_CachedPawnDataWrapper(float headRenderSize, float bodyRenderSize)
//        {
//            this.headRenderSize = headRenderSize;
//            this.bodyRenderSize = bodyRenderSize;
//        }
//    }
//}

