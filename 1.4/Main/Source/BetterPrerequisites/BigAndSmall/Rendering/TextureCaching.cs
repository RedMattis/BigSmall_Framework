using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{

    // Override VFE if it exists.
    [HarmonyPatch]
    public static class Patch_DisableAtlasCaching_Patch
    {
        private static readonly string[] vfeMethods = new string[1]
        {
            "VFECore.Patch_RenderPawnAt:ShouldDisableCaching",
        };

        public static bool Prepare()
        {
            string[] methods = vfeMethods;
            for (int i = 0; i < methods.Length; i++)
            {
                if (!(AccessTools.Method(methods[i]) == null))
                {
                    return true;
                }
                else
                {
                    // Remove these warning later.
                    //Log.Warning($"DEBUG: Failed to find method to patch ({methods[i]})");
                }
            }
            return false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            string[] methods = vfeMethods;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo methodInfo = AccessTools.Method(methods[i]);
                if (!(methodInfo == null))
                {
                    //Log.Message($"Big & Small found VFE and Postfixed ({methods[i]})");
                    yield return methodInfo;
                }
                else
                {
                    //// Remove these warning later.
                    //Log.Warning($"DEBUG: Failed to patch method ({methods[i]})");
                }
            }
        }

        //[HarmonyPatch]
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (BigSmallMod.settings.disableVFETextureCaching)
            {
                __result = true;
            }
        }
    }
}
