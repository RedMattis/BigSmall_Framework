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
