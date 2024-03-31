using BigAndSmall;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class VFECore_PawnDataCache_Patch
    {
        public static bool VFE_Loaded = false;
        private static FieldInfo headRenderSizeField = null;
        private static FieldInfo bodyRenderSizeField = null;
        private static FieldInfo pawnField = null;

        private static readonly string[] VEF_GetPawnDataCachePatches = new string[]
        {
        "VFECore.CachedPawnData:RegenerateCache",
        };

        public static bool Prepare()
        {
            string[] vlfa_methods = VEF_GetPawnDataCachePatches;
            for (int i = 0; i < vlfa_methods.Length; i++)
            {
                if (!(AccessTools.Method(vlfa_methods[i], new Type[] { }) == null))
                {
                    VFE_Loaded = true;
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            string[] vlfa_methods = VEF_GetPawnDataCachePatches;
            for (int i = 0; i < vlfa_methods.Length; i++)
            {
                MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i], new Type[] { });
                if (!(methodInfo == null))
                    yield return methodInfo;
            }
        }

        public static void Postfix(ref object __instance)
        {
            if (__instance == null)
                return;

            if (pawnField == null)
                pawnField = __instance.GetType().GetField("pawn");

            if (pawnField.GetValue(__instance) is Pawn pawn)
            {

                if (headRenderSizeField == null)
                    headRenderSizeField = __instance.GetType().GetField("headRenderSize");
                if (bodyRenderSizeField == null)
                    bodyRenderSizeField = __instance.GetType().GetField("bodyRenderSize");

                float headRenderSize = (float)headRenderSizeField.GetValue(__instance);
                float bodyRenderSize = (float)bodyRenderSizeField.GetValue(__instance);

                // Try add to cache, or update is existing.
                if (VEF_CachedPawnDataWrapper.CachedPawnData.TryGetValue(pawn, out VEF_CachedPawnDataWrapper wrapper))
                {
                    wrapper.bodyRenderSize = bodyRenderSize;
                    wrapper.headRenderSize = headRenderSize;
                }
                else
                {
                    VEF_CachedPawnDataWrapper.CachedPawnData.Add(pawn, new VEF_CachedPawnDataWrapper(headRenderSize, bodyRenderSize));
                }
            }
        }
    }

    public class VEF_CachedPawnDataWrapper
    {
        public static Dictionary<Pawn, VEF_CachedPawnDataWrapper> CachedPawnData = new Dictionary<Pawn, VEF_CachedPawnDataWrapper>();
        public float headRenderSize;
        public float bodyRenderSize;
        public VEF_CachedPawnDataWrapper(float headRenderSize, float bodyRenderSize)
        {
            this.headRenderSize = headRenderSize;
            this.bodyRenderSize = bodyRenderSize;
        }
    }
}


//internal class VEF_Patches
//{

//    [HarmonyPatch]
//    public static class VEF_PawnDataCache_Patch
//    {
//        private static readonly string[] VEF_GetPawnDataCachePatches = new string[]
//        {
//            "VFECore.VanillaGenesExpanded:GetUpdatedMeshSet",
//        };

//        public static bool Prepare()
//        {
//            string[] vlfa_methods = VEF_GetPawnDataCachePatches;
//            for (int i = 0; i < vlfa_methods.Length; i++)
//            {
//                if (!(AccessTools.Method(vlfa_methods[i],
//                     new Type[] { typeof(float), typeof(Pawn) }
//                     ) == null)) return true;
//            }
//            return false;
//        }

//        public static IEnumerable<MethodBase> TargetMethods()
//        {
//            string[] vlfa_methods = VEF_GetPawnDataCachePatches;
//            for (int i = 0; i < vlfa_methods.Length; i++)
//            {
//                MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i],
//                    new Type[] { typeof(Pawn), typeof(bool) });
//                if (!(methodInfo == null))
//                    yield return methodInfo;
//            }
//        }

//        /// <summary>
//        /// Object is a CachedPawnData with a number of fields we want to modify.
//        /// </summary>
//        public static void Postfix(ref object __result, Pawn pawn, bool forceRefresh)
//        {
//            var bsCache = HumanoidPawnScaler.GetBSDict(pawn);

//        }
//    }
//}


//internal class VLFacial_Patches
//{

//    [HarmonyPatch]
//    public static class VEF_PawnDataCache_Patch
//    {
//        private static readonly string[] VEF_GetPawnDataCachePatches = new string[]
//        {
//            "VFECore.PawnDataCache:GetPawnDataCache",
//        };

//        public static bool Prepare()
//        {
//            string[] vlfa_methods = VEF_GetPawnDataCachePatches;
//            for (int i = 0; i < vlfa_methods.Length; i++)
//            {
//                if (!(AccessTools.Method(vlfa_methods[i],
//                     new Type[] { typeof(Pawn), typeof(bool) }
//                     ) == null)) return true;
//            }
//            return false;
//        }

//        public static IEnumerable<MethodBase> TargetMethods()
//        {
//            string[] vlfa_methods = VEF_GetPawnDataCachePatches;
//            for (int i = 0; i < vlfa_methods.Length; i++)
//            {
//                MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i],
//                    new Type[] { typeof(Pawn), typeof(bool) });
//                if (!(methodInfo == null))
//                    yield return methodInfo;
//            }
//        }

//        /// <summary>
//        /// Object is a CachedPawnData with a number of fields we want to modify.
//        /// </summary>
//        public static void Postfix(ref object __result, Pawn pawn, bool forceRefresh)
//        {
//            var bsCache = HumanoidPawnScaler.GetBSDict(pawn);

//        }
//    }
//}


//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Verse;

//namespace BigAndSmall
//{

//}

//namespace BigAndSmall
//{

//    //"BigAndSmall.HumanoidPawnScaler:GetBodyRenderSize")

//    public static partial class HarmonyPatches
//    {

//        [HarmonyPatch]
//        public static class VEF_DrawSettings_TryGetNewMeshPatch
//        {
//            private static readonly string[] VEFMethods = new string[4]
//            {
//                "VFECore.Patch_PawnRenderer_DrawPawnBody_Transpiler:ModifyMesh",
//                "VFECore.Patch_DrawHeadHair_DrawApparel_Transpiler:TryModifyMeshRef",
//                "VFECore.Harmony_PawnRenderer_DrawBodyApparel:ModifyShellMesh",
//                "VFECore.Harmony_PawnRenderer_DrawBodyApparel:ModifyPackMesh",
//                //"VFECore.DrawSettings:TryGetNewMesh"
//            };

//            public static bool Prepare()
//            {
//                string[] vEFMethods = VEFMethods;
//                for (int i = 0; i < vEFMethods.Length; i++)
//                {
//                    if (!(AccessTools.Method(vEFMethods[i]) == null))
//                    {
//                        return true;
//                    }
//                    //else
//                    //{
//                    //    // Remove these warning later.
//                    //    Log.Warning($"Failed to find method to patch ({VEFMethods[i]})");
//                    //}
//                }
//                return false;
//            }

//            public static IEnumerable<MethodBase> TargetMethods()
//            {
//                string[] vEFMethods = VEFMethods;
//                for (int i = 0; i < vEFMethods.Length; i++)
//                {
//                    MethodInfo methodInfo = AccessTools.Method(vEFMethods[i]);
//                    if (!(methodInfo == null))
//                    {
//                        yield return methodInfo;
//                    }
//                    //else
//                    //{
//                    //    Log.Warning($"Method was null ({VEFMethods[i]})");
//                    //}
//                }
//            }

//            public static void Prefix(ref Pawn pawn, ref Mesh mesh)
//            {
//                if (BigSmall.performScaleCalculations && BigSmall.activePawn != null && BigSmall.humnoidScaler != null)
//                {
//                    RaceProperties raceProps = pawn.RaceProps;
//                    if (raceProps != null && raceProps.Humanlike)
//                    {
//                        //var sizeCache = HumanoidPawnScaler.GetPawnSizeDict(BigSmall.activePawn);
//                        var renderSize = HumanoidPawnScaler.GetBodyRenderSize(out _, BigSmall.activePawn);
//                        mesh = BigSmall.GetPawnMesh(renderSize, pawn.Rotation.AsInt == 3);
//                    }
//                }
//            }
//        }

//        [HarmonyPatch]
//        public static class VEF_HumanlikeMeshPoolUtility_HeadSizeFactorVectorPatch
//        {
//            private static readonly string[] VEFMethods = new string[1]
//            {
//                "VanillaGenesExpanded.HumanlikeMeshPoolUtility_Patches:GetUpdatedMeshSetXY",
//                //"VanillaGenesExpanded.HumanlikeMeshPoolUtility_Patches:GetUpdatedHeadMeshSet",
//                //"VanillaGenesExpanded.HumanlikeMeshPoolUtility_Patches:headSizeVectorFromFactor",
//                //"VFECore.DrawSettings:TryGetNewMesh"
//            };

//            public static bool Prepare()
//            {
//                string[] vEFMethods = VEFMethods;
//                for (int i = 0; i < vEFMethods.Length; i++)
//                {
//                    if (!(AccessTools.Method(vEFMethods[i]) == null))
//                    {
//                        return true;
//                    }
//                    else
//                    {
//                        // Remove these warning later.
//                        Log.Warning($"DEBUG: Failed to find method to patch ({VEFMethods[i]})");
//                    }
//                }
//                return false;
//            }

//            public static IEnumerable<MethodBase> TargetMethods()
//            {
//                string[] vEFMethods = VEFMethods;
//                for (int i = 0; i < vEFMethods.Length; i++)
//                {
//                    MethodInfo methodInfo = AccessTools.Method(vEFMethods[i]);
//                    if (!(methodInfo == null))
//                    {
//                        yield return methodInfo;
//                    }
//                    else
//                    {
//                        Log.Warning($"DEBUG: Method was null ({VEFMethods[i]})");
//                    }
//                }
//            }

//            public static void Prefix(ref float x, ref float y, ref Pawn pawn)
//            {
//                if (BigSmall.performScaleCalculations && BigSmall.activePawn != null && BigSmall.humnoidScaler != null)
//                {
//                    RaceProperties raceProps = pawn.RaceProps;
//                    if (raceProps != null && raceProps.Humanlike)
//                    {
//                        var renderSize = HumanoidPawnScaler.GetHeadRenderSize(BigSmall.activePawn);
//                        x *= renderSize;
//                        y *= renderSize;
//                    }
//                }
//            }
//        }

//        // -----------------------------------------
//        // Old stuff
//        // -----------------------------------------

//        //[HarmonyPatch]
//        //public static class VefCompatibilityPatches
//        //{
//        //    static string methodName = "VanillaGenesExpanded.HumanlikeMeshPoolUtility_Patches:GeneScaleFactor";

//        //    private static readonly BodyScaleDelegate vefBodyScaleMethod = HasVFE
//        //        ? AccessTools.MethodDelegate<BodyScaleDelegate>(methodName) : null;


//        //    public static bool Prepare()
//        //    {
//        //        return HasVFE && NotNull(vefBodyScaleMethod);
//        //    }


//        //    public static MethodBase TargetMethod()
//        //    {
//        //        return AccessTools.Method("BigAndSmall.HumanoidPawnScaler:GetBodyRenderSize");
//        //    }

//        //    public static float Postfix(float __result, Pawn pawn)
//        //    {
//        //        Log.Message($"Get Body Render reported {__result} for {pawn}");
//        //        return pawn is null ? __result : vefBodyScaleMethod(__result, pawn);
//        //    }

//        //    //public static void Postfix(float __result, Pawn pawn)
//        //    //{
//        //    //    if (BigSmall.performScaleCalculations && BigSmall.activePawn != null && BigSmall.humnoidScaler != null)
//        //    //    {
//        //    //        RaceProperties raceProps = pawn.RaceProps;
//        //    //        if (raceProps != null && raceProps.Humanlike)
//        //    //        {

//        //    //            __result = BigSmall.humnoidScaler.GetBodyRenderSize(pawn);
//        //    //        }
//        //    //    }

//        //    //    //return pawn is null ? result : vefBodyScaleMethod(result, pawn);
//        //    //}

//        //    private delegate float BodyScaleDelegate(float width, Pawn pawn);
//        //}


//        //[HarmonyPatch]
//        //public static class VanillaGenesExpanded_DrawBodyGenes_Patch_Patch
//        //{
//        //    private static readonly string[] VEFMethods = new string[1]
//        //    {
//        //        //"VFECore.VanillaGenesExpanded.DrawBodyGenes_Patch:SetBodyScale",
//        //        "VanillaGenesExpanded.DrawBodyGenes_Patch:SetBodyScale",
//        //        //"VFECore.DrawBodyGenes_Patch:SetBodyScale",
//        //        //"DrawBodyGenes:SetBodyScale",
//        //        //"VFECore.DrawBodyApparel:SetBodyScale",
//        //    };

//        //    public static bool Prepare()
//        //    {
//        //        string[] vEFMethods = VEFMethods;
//        //        for (int i = 0; i < vEFMethods.Length; i++)
//        //        {
//        //            if (!(AccessTools.Method(vEFMethods[i]) == null))
//        //            {
//        //                return true;
//        //            }
//        //            else
//        //            {
//        //                // Remove these warning later.
//        //                Log.Warning($"Failed to find method to patch ({vEFMethods[i]})");
//        //            }
//        //        }
//        //        return false;
//        //    }

//        //    public static IEnumerable<MethodBase> TargetMethods()
//        //    {
//        //        string[] vEFMethods = VEFMethods;
//        //        for (int i = 0; i < vEFMethods.Length; i++)
//        //        {
//        //            MethodInfo methodInfo = AccessTools.Method(vEFMethods[i]);
//        //            if (!(methodInfo == null))
//        //            {
//        //                yield return methodInfo;
//        //            }
//        //            else
//        //            {
//        //                Log.Warning($"Method was null ({vEFMethods[i]})");
//        //            }
//        //        }
//        //    }

//        //    public static void Postfix(ref Pawn pawn, ref Vector2 scale, ref Vector2 __result)
//        //    {
//        //        if (BigSmall.performScaleCalculations && BigSmall.activePawn != null && BigSmall.humnoidScaler != null)
//        //        {
//        //            RaceProperties raceProps = pawn.RaceProps;
//        //            if (raceProps != null && raceProps.Humanlike)
//        //            {
//        //                Log.Warning($"GetBodyRenderSize. Was {__result}, modified to {BigSmall.humnoidScaler.GetBodyRenderSize(pawn) * __result}");
//        //                __result *= BigSmall.humnoidScaler.GetBodyRenderSize(pawn);
//        //            }
//        //        }
//        //    }
//        //}
//        //[HarmonyPatch]
//        //public static class VanillaGenesExpanded_DrawGeneEyes_Patch_Patch
//        //{
//        //    private static readonly string[] VEFMethods = new string[2]
//        //    {
//        //        "VanillaGenesExpanded.DrawGeneEyes_Patch:SetHeadScale",
//        //        "VanillaGenesExpanded.HumanlikeMeshPoolUtility_Patches:GeneScaleFactor",
//        //    };

//        //    public static bool Prepare()
//        //    {
//        //        string[] vEFMethods = VEFMethods;
//        //        for (int i = 0; i < vEFMethods.Length; i++)
//        //        {
//        //            if (!(AccessTools.Method(vEFMethods[i]) == null))
//        //            {
//        //                return true;
//        //            }
//        //            else
//        //            {
//        //                Log.Warning($"Failed to find method to patch ({vEFMethods[i]})");
//        //            }
//        //        }
//        //        return false;
//        //    }

//        //    public static IEnumerable<MethodBase> TargetMethods()
//        //    {
//        //        string[] vEFMethods = VEFMethods;
//        //        for (int i = 0; i < vEFMethods.Length; i++)
//        //        {
//        //            MethodInfo methodInfo = AccessTools.Method(vEFMethods[i]);
//        //            if (!(methodInfo == null))
//        //            {
//        //                yield return methodInfo;
//        //            }
//        //            else
//        //            {
//        //                Log.Message($"Method was null ({vEFMethods[i]})");
//        //            }
//        //        }
//        //    }

//        //    public static void Postfix(ref Pawn pawn, ref float __result)
//        //    {
//        //        if (BigSmall.performScaleCalculations && BigSmall.activePawn != null && BigSmall.humnoidScaler != null)
//        //        {
//        //            RaceProperties raceProps = pawn.RaceProps;
//        //            if (raceProps != null && raceProps.Humanlike)
//        //            {
//        //                __result *= BigSmall.humnoidScaler.GetHeadRenderSize(pawn);
//        //            }
//        //        }
//        //    }
//        //}

//    }
//}
