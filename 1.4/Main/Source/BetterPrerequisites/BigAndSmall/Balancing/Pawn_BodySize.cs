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
            if (!BSCache.regenerationInProgress)
            {
                var sizeCache = HumanoidPawnScaler.GetBSDict(__instance);
                if (sizeCache != null)
                    __result = sizeCache.totalSize;
            }
        }
    }

}
