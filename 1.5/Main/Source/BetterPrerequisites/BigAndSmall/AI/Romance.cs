using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    // Patch InteractionWorker_RomanceAttempt's RandomSelectionWeight method
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight), MethodType.Normal)]
    public static class InteractionWorker_RomanceAttempt_Patch
    {
        public static StatDef flirtChanceDef;

        public static void Postfix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null)
            {
                return;
            }
            if (initiator != null
                && initiator.needs != null)
            {
                var cache = HumanoidPawnScaler.GetBSDict(initiator);
                if (cache != null && cache.succubusUnbonded)
                {
                    __result *= 20;
                }
            }

            if (flirtChanceDef == null)
            {
                flirtChanceDef = DefDatabase<StatDef>.GetNamed("SM_FlirtChance");
            }

            // If recipient has no flirt chance, set result to 0. They are probably something that cannot be romanced.
            if (recipient.GetStatValue(flirtChanceDef) == 0)
            {
                __result = 0;
            }

            __result *= initiator.GetStatValue(flirtChanceDef);
        }
    }
}
