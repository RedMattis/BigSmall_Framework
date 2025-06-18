using HarmonyLib;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(Pawn), "BodySize", MethodType.Getter)]
    public static class Pawn_BodySize
    {
        public static void Postfix(ref float __result, Pawn __instance)
        {
            if (__instance.GetCacheFast() is BSCache sizeCache)
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
