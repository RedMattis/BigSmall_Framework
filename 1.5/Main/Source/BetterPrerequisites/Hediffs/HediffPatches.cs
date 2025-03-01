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
            bool supressMngrChangeMade = false;
            Pawn pawn = __instance?.pawn;
            if (pawn != null)
            {
                if (pawn.genes != null)
                {
                    if (GeneSuppressorManager.supressedGenesPerPawn_Hediff.Keys.Contains(pawn))
                    {
                        var suppressDict = GeneSuppressorManager.supressedGenesPerPawn_Hediff[pawn];
                        // Remove the Hediff from the Suppressors in the dictionary list.
                        foreach (var key in suppressDict.Keys)
                        {
                            if (suppressDict[key].Contains(__instance.def))
                            {
                                suppressDict[key].Remove(__instance.def);
                                supressMngrChangeMade = true;
                            }
                        }
                        // Remove all dictionary entries with no suppressors.
                        foreach (var key in suppressDict.Keys.ToList())
                        {
                            if (suppressDict[key].Count == 0)
                            {
                                suppressDict.Remove(key);
                                supressMngrChangeMade = true;
                            }
                        }
                    }
                }
            
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
            if (pawn == null)
            {
                return;
            }

            HumanoidPawnScaler.LazyGetCache(pawn, 30);
        }
    }

}