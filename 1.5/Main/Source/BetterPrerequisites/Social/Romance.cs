using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    // Patch InteractionWorker_RomanceAttempt's RandomSelectionWeight method

    [HarmonyPatch]
    public static class RomancePatches
    {
        private static StatDef _flirtChanceDef;
        public static StatDef FlirtChanceDef = _flirtChanceDef ??= DefDatabase<StatDef>.GetNamed("SM_FlirtChance");

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
                var cache = HumanoidPawnScaler.GetCache(initiator);
                if (cache != null && cache.succubusUnbonded)
                {
                    __result *= 20;
                }
            }

            

            // If recipient has no flirt chance, set result to 0. They are probably something that cannot be romanced.
            if (recipient.GetStatValue(FlirtChanceDef, cacheStaleAfterTicks:1000) == 0)
            {
                __result = 0;
            }

            __result *= initiator.GetStatValue(FlirtChanceDef, cacheStaleAfterTicks: 1000);
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


        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.CompatibilityWith))]
        [HarmonyPostfix]
        public static void CompatibilityWith_Postfix(ref float __result, Pawn_RelationsTracker __instance, Pawn otherPawn, Pawn ___pawn)
        {
            __result = GetCompatibilityWith(___pawn, otherPawn, __result);

        }
        public static float GetCompatibilityWith(this Pawn pawn, Pawn otherPawn, float defaultValue = 0)
        {
            float ConstantPerPawnsPairCompatibilityOffset(int otherPawnID)
            {
                Rand.PushState();
                Rand.Seed = (pawn.thingIDNumber ^ otherPawnID) * 37;
                float result = Rand.GaussianAsymmetric(0.3f, 1f, 1.4f);
                Rand.PopState();
                return result;
            }
            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && HumanoidPawnScaler.GetCacheUltraSpeed(otherPawn) is BSCache cacheTwo)
            {
                if (pawn == otherPawn) return 0;

                float? compatibility = RomanceTagsExtensions.GetHighestSharedTag(cache, cacheTwo);

                if (pawn.GetStatValue(FlirtChanceDef, cacheStaleAfterTicks: 1000) == 0 || otherPawn.GetStatValue(FlirtChanceDef, cacheStaleAfterTicks: 1000) == 0)
                    return 0;
                if (compatibility == null && pawn.def != otherPawn.def)
                {
                    return defaultValue;
                }

                float x = Mathf.Abs(cache.apparentAge - cacheTwo.apparentAge);
                float num = Mathf.Clamp(GenMath.LerpDouble(0f, 20f, 0.45f, -0.45f, x), -0.45f, 0.45f);
                float num2 = ConstantPerPawnsPairCompatibilityOffset(otherPawn.thingIDNumber);
                return (num + num2) * compatibility.Value;
            }
            return defaultValue;
        }
    }
}
