using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using VariedBodySizes;
using Verse;

namespace BigAndSmall
{
    //DrawFaceGraphicsComp

    internal class VLFacial_Patches
    {

        [HarmonyPatch]
        public static class VLFA_DrawSettings_TryGetNewMeshPatch
        {
            private static readonly string[] VLFA_Methods = new string[]
            {
                "FacialAnimation.GraphicHelper:DrawMeshNowOrLaterWithScale",
            };

            public static bool Prepare()
            {
                string[] vlfa_methods = VLFA_Methods;
                for (int i = 0; i < vlfa_methods.Length; i++)
                {
                    if (!(AccessTools.Method(vlfa_methods[i],
                         new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool), typeof(float), typeof(float) }
                         ) == null)) return true;
                }
                return false;
            }

            public static IEnumerable<MethodBase> TargetMethods()
            {
                string[] vlfa_methods = VLFA_Methods;
                for (int i = 0; i < vlfa_methods.Length; i++)
                {
                    MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i], 
                        new Type[]{ typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool), typeof(float), typeof(float) });
                    if (!(methodInfo == null))
                        yield return methodInfo;
                }
            }

            public static void Prefix(ref Mesh mesh, ref Vector3 loc, ref Quaternion quat, ref Material mat, ref bool drawNow, ref float scaleW, ref float scaleH)
            {
                if (BigSmall.activePawn != null)
                {
                    float val = HumanoidPawnScaler.GetBSDict(BigSmall.activePawn).bodyRenderSize;
                    scaleW *= val;
                    scaleH *= val;
                }
            }
        }

        /// <summary>
        /// Patch Facial Animations so we don't get itty bitty beards on giant and vice versa on dwarves.
        /// </summary>
        [HarmonyPatch]
        public static class FA_PrefixGetHumanlikeBeardSetForPawn_Patch
        {
            //public static GraphicMeshSet gMeshSet = null;

            private static readonly string[] FA_Methods = new string[]
            {
                "FacialAnimation.HarmonyPatches:PrefixGetHumanlikeBeardSetForPawn",
            };

            public static bool Prepare()
            {
                string[] fa_methods = FA_Methods;
                for (int i = 0; i < fa_methods.Length; i++)
                {
                    if (!(AccessTools.Method(fa_methods[i]) == null)) return true;
                }
                return false;
            }

            public static IEnumerable<MethodBase> TargetMethods()
            {
                string[] vlfa_methods = FA_Methods;
                for (int i = 0; i < vlfa_methods.Length; i++)
                {
                    MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i]);
                    if (!(methodInfo == null))
                        yield return methodInfo;
                }
            }

            //public static void Prefix(Pawn __0, ref GraphicMeshSet __1, ref bool __result)
            //{
            //    gMeshSet = __1;
            //}

            /// <summary>
            /// Basically just overwrites the beards, because I couldn't figure out a way to otherwise stop it from messing with their size.
            /// </summary>
            public static void Postfix(Pawn __0, ref GraphicMeshSet __1, ref bool __result)
            {
                //if (gMeshSet != null)
                //    __1 = gMeshSet;

                var pawn = __0;
                Vector2 hairMeshSize = pawn.story.headType.hairMeshSize;
                if (ModsConfig.BiotechActive && pawn.ageTracker.CurLifeStage.headSizeFactor.HasValue)
                {
                    hairMeshSize *= pawn.ageTracker.CurLifeStage.headSizeFactor.Value;
                }
                hairMeshSize *= HumanoidPawnScaler.GetBSDict(__0).headRenderSize;
                __1 = MeshPool.GetMeshSetForWidth(hairMeshSize.x, hairMeshSize.y);
                
            }
        }
    }
}
