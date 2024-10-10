using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.Noise;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(Pawn), "BodySize", MethodType.Getter)]
    public static class Pawn_BodySize
    {
        public static void Postfix(ref float __result, Pawn __instance)
        {
            if (HumanoidPawnScaler.GetCacheUltraSpeed(__instance) is BSCache sizeCache)
            {
                __result += sizeCache.totalSizeOffset;
                if (__result < 0.05f)
                {
                    __result = 0.05f;
                }
            }
        }
    }

}
