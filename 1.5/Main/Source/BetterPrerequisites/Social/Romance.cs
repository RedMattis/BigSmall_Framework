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

    [HarmonyPatch]
    public static class RomancePatches
    {
        public static StatDef flirtChanceDef;

        [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight), MethodType.Normal)]
        [HarmonyPostfix]
        public static void RomanceAttemptPostfix(ref float __result, Pawn initiator, Pawn recipient)
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

        [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), nameof(InteractionWorker_MarriageProposal.RandomSelectionWeight), MethodType.Normal)]
        [HarmonyPrefix]
        public static bool MarriageProposalPrefix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null || __result == 0)
            {
                return true;
            }
            if (initiator != null && initiator.needs != null)
            {
                if (FastAcccess.GetCache(initiator) is BSCache cache)
                {
                    // Implement check to avoid pawns propossing to non-sapient humanoids, e.g. robots.
                    if (cache.isDrone)
                    {
                        __result = 0;
                        return false;
                    }
                }
            }
            return true;
        }


    }


}
