using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace BigAndSmall
{
    public class Factions : DefModExtension
    {
        public bool canUseDropPods = true;
    }

    // Postfix PawnArrivalModeDef's CanUseWith so the maximum permitted parms.faction.def.techlevel is Medieval.
    [HarmonyPatch(typeof(PawnsArrivalModeWorker), nameof(PawnsArrivalModeWorker.CanUseWith))]
    public static class PawnsArrivalModeWorker_CanUseWith
    {
        public static void Postfix(ref bool __result, PawnsArrivalModeWorker __instance, IncidentParms parms)
        {
            if (__result && parms.faction != null && parms.faction.def.HasModExtension<Factions>())
            {
                var canUseDropPods = parms.faction.def.GetModExtension<Factions>().canUseDropPods;
                if (!canUseDropPods)
                {
                    __result = parms.faction.def.techLevel <= TechLevel.Medieval;
                }
            }
        }
    }
}
