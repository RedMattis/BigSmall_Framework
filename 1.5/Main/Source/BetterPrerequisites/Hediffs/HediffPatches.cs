using BigAndSmall;
using HarmonyLib;
using System;
using System.Linq;
using Verse;

namespace BetterPrerequisites
{

    [HarmonyPatch]
    public static class Hediff_Patches
    {
        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostRemoved))]
        [HarmonyPostfix]
        public static void Hediff_PostRemove(Hediff __instance)
        {
            Pawn pawn = __instance?.pawn;
            if (pawn != null)
            {
                bool supressMngrChangeMade = GeneSuppressorManager.TryRemoveSupressorHediff(__instance, pawn);

                bool requiresRefresh = __instance?.def?.GetAllPawnExtensionsOnHediff() is var extensions && extensions.Any(x => x.RequiresCacheRefresh());
                if (requiresRefresh || (pawn?.Drawer?.renderer != null && pawn.Spawned))
                {
                    if (supressMngrChangeMade)
                    {
                        __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
                    }
                    HumanoidPawnScaler.ShedueleForceRegenerateSafe(pawn, 40);
                }
                else
                {
                    HumanoidPawnScaler.LazyGetCache(pawn, 40);
                }
            }
        }

        

        

        [HarmonyPatch(typeof(Hediff), "OnStageIndexChanged")]
        [HarmonyPostfix]
        public static void OnStageIndexChanged(Hediff __instance, int stageIndex)
        {
            if (__instance?.pawn?.RaceProps?.Humanlike != true) return;
            HumanoidPawnScaler.LazyGetCache(__instance.pawn, 60);
        }

        [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostAdd))]
        [HarmonyPostfix]
        public static void Hediff_PostAdd(Hediff __instance, DamageInfo? dinfo)
        {
            var pawn = __instance?.pawn;
            if (pawn == null || __instance == null)
            {
                return;
            }
            GeneSuppressorManager.TryAddSuppressorHediff(__instance, pawn);
            HumanoidPawnScaler.LazyGetCache(pawn, 30);
        }
    }

}