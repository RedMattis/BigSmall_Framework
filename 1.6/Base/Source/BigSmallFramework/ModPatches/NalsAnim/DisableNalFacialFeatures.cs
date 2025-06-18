using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace BigAndSmall
{
    internal class FactialAnimHarmonyPatches
    {
        [HarmonyPatch]
        public static class FA_DisableFeatures
        {
            private static readonly string[] fa_methods = new string[]
            {
                "FacialAnimation.FacialAnimationModSettings:ShouldDrawRaceXenoType",
            };

            public static bool Prepare()
            {
                string[] vlfa_methods = fa_methods;
                for (int i = 0; i < vlfa_methods.Length; i++)
                {
                    if (!(AccessTools.Method(vlfa_methods[i]) == null)) return true;
                }
                return false;
            }

            public static IEnumerable<MethodBase> TargetMethods()
            {
                string[] vlfa_methods = fa_methods;
                for (int i = 0; i < vlfa_methods.Length; i++)
                {
                    MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i]);
                    if (!(methodInfo == null))
                        yield return methodInfo;
                }
            }
            public static bool pawnInitialized = true;

            public static bool Prefix(ref bool __result, object __instance, Pawn pawn)
            {

                if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn, canRegenerate:false) is BSCache cache && cache.facialAnimationDisabled)
                {
                    __result = false;
                    return false; // Skip original method
                }
                return true;
            }

        }
    }
}