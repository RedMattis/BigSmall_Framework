using BigAndSmall;
using BigAndSmall.SpecialGenes.Gender;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace BetterPrerequisites
{

    public static class Hediff_Patches
    {
        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostRemoved))]
        [HarmonyPostfix]
        public static void Hediff_PostRemove(Hediff __instance)
        {
            bool supressMngrChangeMade = false;
            if (__instance.pawn != null)
            {
                if (GeneSuppressorManager.supressedGenesPerPawn_Hediff.Keys.Contains(__instance.pawn))
                {
                    var suppressDict = GeneSuppressorManager.supressedGenesPerPawn_Hediff[__instance.pawn];
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
            if (__instance?.pawn?.Drawer?.renderer != null && __instance.pawn.Spawned)
            {
                if (supressMngrChangeMade)
                {
                    __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                HumanoidPawnScaler.GetCache(__instance.pawn, scheduleForce: 1);
            }
        }

        [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostAdd))]
        [HarmonyPostfix]
        public static void Hediff_PostAdd(Hediff __instance, DamageInfo? dinfo)
        {
            var raceProps = __instance?.pawn?.RaceProps;
            if (raceProps == null || __instance?.pawn?.RaceProps?.Animal == true)
            {
                return;
            }
            var genes = __instance?.pawn?.genes;
            if (genes == null) return;
            var geneList = genes.GenesListForReading;
            if (genes != null && geneList.Count > 0)
            {
                bool changeMade = false;
                try
                {
                    PGene.supressPostfix = true;
                    changeMade = GeneSuppressorManager.TryAddSuppressor(__instance, __instance.pawn);
                }
                finally
                {
                    PGene.supressPostfix = false;
                }
                if (__instance?.pawn?.Drawer?.renderer != null && __instance.pawn.Spawned)
                {
                    if (changeMade)
                    {
                        __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
                    }
                    HumanoidPawnScaler.GetCache(__instance.pawn, scheduleForce: 1);
                }

            }
        }
    }

}