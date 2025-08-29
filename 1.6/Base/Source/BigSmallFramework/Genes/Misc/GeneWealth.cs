using BigAndSmall;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.ForceRecount))]
    public static class WealthWatcher_ForceRecount_Patch
    {
        public static bool raidWealthActive = false;
        [HarmonyPrefix]
        public static void WealthCountStart(WealthWatcher __instance, bool allowDuringInit)
        {
            raidWealthActive = true;
        }
        [HarmonyPostfix]
        public static void WealthCountEnd(WealthWatcher __instance, bool allowDuringInit)
        {
            raidWealthActive = false;
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.MarketValue), MethodType.Getter)]
    public static class MarketValuePatch
    {
        [HarmonyPostfix]
        public static void MarketValuePostfix(Thing __instance, ref float __result)
        {
            if (WealthWatcher_ForceRecount_Patch.raidWealthActive)
            {
                if (__instance is Pawn pawn &&
                    HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
                {
                    __result *= cache.raidWealthMultiplier;
                    __result += cache.raidWealthOffset;
                }
            }
        }
    }
}
