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

        //[HarmonyPatch]
        //public static class VLFA_DrawSettings_TryGetNewMeshPatch
        //{
        //    private static readonly string[] VLFA_Methods = new string[]
        //    {
        //        "FacialAnimation.GraphicHelper:DrawMeshNowOrLaterWithScale",
        //    };

        //    public static bool Prepare()
        //    {
        //        string[] vlfa_methods = VLFA_Methods;
        //        for (int i = 0; i < vlfa_methods.Length; i++)
        //        {
        //            if (!(AccessTools.Method(vlfa_methods[i],
        //                 new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool), typeof(float), typeof(float) }
        //                 ) == null)) return true;
        //        }
        //        return false;
        //    }

        //    public static IEnumerable<MethodBase> TargetMethods()
        //    {
        //        string[] vlfa_methods = VLFA_Methods;
        //        for (int i = 0; i < vlfa_methods.Length; i++)
        //        {
        //            MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i],
        //                new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool), typeof(float), typeof(float) });
        //            if (!(methodInfo == null))
        //                yield return methodInfo;
        //        }
        //    }

        //    public static void Prefix(ref Mesh mesh, ref Vector3 loc, ref Quaternion quat, ref Material mat, ref bool drawNow, ref float scaleW, ref float scaleH)
        //    {
        //        if (BigSmall.activePawn != null)
        //        {
        //            float val = HumanoidPawnScaler.GetBSDict(BigSmall.activePawn).headRenderSize;
        //            scaleW *= val;
        //            scaleH *= val;
        //        }
        //    }
        //}
    }
}
