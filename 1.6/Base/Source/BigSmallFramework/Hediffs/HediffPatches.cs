using BigAndSmall;
using HarmonyLib;
using System;
using System.Linq;
using Verse;

namespace BigAndSmall
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
                //bool supressMngrChangeMade = GeneSuppressorManager.TryRemoveSupressorHediff(__instance, pawn);

                bool requiresRefresh = __instance?.def?.GetAllPawnExtensionsOnHediff() is var extensions && extensions.Any(x => x.RequiresCacheRefresh());
                if (requiresRefresh || (pawn?.Drawer?.renderer != null && pawn.Spawned))
                {
                    //if (supressMngrChangeMade)
                    //{
                    //    __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
                    //}
                    HumanoidPawnScaler.ShedueleForceRegenerateSafe(pawn, 40);
                }
                else
                {
                    HumanoidPawnScaler.GetInvalidateLater(pawn, 40);
                }
            }
        }

        

        

        [HarmonyPatch(typeof(Hediff), "OnStageIndexChanged")]
        [HarmonyPostfix]
        public static void OnStageIndexChanged(Hediff __instance, int stageIndex)
        {
            if (__instance?.pawn?.RaceProps?.Humanlike != true) return;
            HumanoidPawnScaler.GetInvalidateLater(__instance.pawn, 60);
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
            //GeneSuppressorManager.TryAddSuppressorHediff(__instance, pawn);
            HumanoidPawnScaler.GetInvalidateLater(pawn, 30);
        }

        [HarmonyPatch(typeof(Hediff), nameof(Hediff.GetTooltip))]
        [HarmonyPostfix]
        public static void Hediff_GetTooltip(Hediff __instance, ref string __result)
        {
            if (__instance?.def?.GetAllPawnExtensionsOnHediff() is var extensions && extensions.Any())
            {
                try
                {
                    if (PawnExtensionExtension.TryGetDescription(extensions, out string pawnDesc))
                    {
                        __result += $"\n\n{pawnDesc}";
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Error generating Hediff.Description.\n{e.Message}\n{e.StackTrace}");
                }
            }
        }
    }

}