using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class MechanicalColonistPatches
    {
        [HarmonyPatch(typeof(HealthUtility), "TryAnesthetize")]
        [HarmonyPrefix] [HarmonyPriority(Priority.Low)]
        public static bool TryAnesthetizePatch(Pawn pawn)
        {
            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && cache.isMechanical)
            {
                return false;
            }
            return true;
        }

        //public ResolvedWound ChooseWoundOverlay(Hediff hediff)
        [HarmonyPatch(typeof(FleshTypeDef), nameof(FleshTypeDef.ChooseWoundOverlay))]
        [HarmonyPrefix] [HarmonyPriority(Priority.Low)]
        public static bool ChooseWoundOverlayPatch(ref FleshTypeDef.ResolvedWound __result, FleshTypeDef __instance, Hediff hediff)
        {
            if (__instance != FleshTypeDefOf.Mechanoid && HumanoidPawnScaler.GetCacheUltraSpeed(hediff.pawn) is BSCache cache && cache.isMechanical)
            {
                __result = FleshTypeDefOf.Mechanoid.ChooseWoundOverlay(hediff);
                return false;
            }
            return true;
        }
    }
}
