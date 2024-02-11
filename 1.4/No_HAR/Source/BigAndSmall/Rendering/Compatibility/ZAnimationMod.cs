//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
////using VariedBodySizes;
//using Verse;


// Doesn't work. Kept around for reference when I try to fix it later.

//namespace BigAndSmall
//{
//    //DrawFaceGraphicsComp

//    internal class ZAnimationMod_Patch
//    {

//        [HarmonyPatch]
//        public static class ZAnim_GetWorldPosition
//        {
//            private static readonly List<string> zAnimMethods = new List<string>()
//            {
//                "AnimationRendererWorker:PreRenderPawn",
//            };

//            public static bool Prepare()
//            {
//                List<string> vlfa_methods = zAnimMethods;
//                for (int i = 0; i < vlfa_methods.Count; i++)
//                {
//                    if (!(AccessTools.Method(vlfa_methods[i]) == null))
//                    {
//                        //Log.Message($"DEBUG - Big and Small: ZAnim method {vlfa_methods[i]} found.");
//                        return true;
//                    }
//                    else
//                    {
//                        Log.Message($"DEBUG - Big and Small: ZAnim method {vlfa_methods[i]} not found.");
//                    }
//                }
//                return false;
//            }

//            public static IEnumerable<MethodBase> TargetMethods()
//            {
//                List<string> vlfa_methods = zAnimMethods;
//                for (int i = 0; i < vlfa_methods.Count; i++)
//                {
//                    MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i]);
//                    if (!(methodInfo == null))
//                        yield return methodInfo;
//                    else
//                    {
//                        Log.Message($"DEBUG - Big and Small: ZAnim method {vlfa_methods[i]} could not be targeted.");
//                    }
//                }
//            }

//            public static void Prefix(ref object part, ref Vector3 position, ref Rot4 rotation, Pawn pawn)
//            {
//                Log.Message($"DEBUG - Big and Small: ZAnim method called.");
//                if (BigSmall.performScaleCalculations && BigSmall.activePawn != null && BigSmall.humnoidScaler != null)
//                {
//                    float offset = RenderPawnAt_Patch.GetOffset(pawn);
//                    position.y += offset;
//                    Log.Message($"DEBUG: Offset by {offset}");
//                }
//            }
//        }
//    }
//}
