using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Noise;
using System.Linq;

namespace BigAndSmall
{
    // Patch InteractionWorker_RomanceAttempt's RandomSelectionWeight method

    [HarmonyPatch]
    public static class RomancePatches
    {
        //[HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight), MethodType.Normal)]
        //[HarmonyPostfix]
        //public static void RomanceAttemptPostfix(ref float __result, Pawn initiator, Pawn recipient)
        //{
        //    if (initiator == null || recipient == null)
        //    {
        //        return;
        //    }
        //    if (initiator != null
        //        && initiator.needs != null)
        //    {
        //        var cache = HumanoidPawnScaler.GetCache(initiator);
        //        if (cache != null && cache.succubusUnbonded)
        //        {
        //            __result *= 20;
        //        }
        //    }
        //    float compatibility = initiator.GetCompatibilityWith(recipient, reductionScale: 1.0f, oldValue: 1.0f);


        //    // If recipient has no flirt chance, set result to 0. They are probably something that cannot be romanced.
        //    if (recipient.GetStatValue(BSDefs.SM_FlirtChance, cacheStaleAfterTicks:1000) == 0)
        //    {
        //        __result = 0;
        //    }

        //    __result *= initiator.GetStatValue(BSDefs.SM_FlirtChance, cacheStaleAfterTicks: 1000) * compatibility;
        //}

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
        [HarmonyPriority(Priority.Low)]
        public static void CompatibilityWith_Postfix(ref float __result, Pawn_RelationsTracker __instance, Pawn otherPawn, Pawn ___pawn)
        {
            __result = GetCompatibilityWith(___pawn, otherPawn, reductionScale:0.5f, __result);

        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryLovinChanceFactor))]
        public static IEnumerable<CodeInstruction> LovingFactor_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo defField = AccessTools.Field(typeof(Thing), nameof(Thing.def));
            MethodInfo racePropsGetter = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.RaceProps));
            MethodInfo humanlikeGetter = AccessTools.PropertyGetter(typeof(RaceProperties), nameof(RaceProperties.Humanlike));
            List<CodeInstruction> humanlikeInstructions =
            [
                new (OpCodes.Callvirt, racePropsGetter),
                new (OpCodes.Callvirt, humanlikeGetter),
                new (OpCodes.Ldarg_1),
                new (OpCodes.Callvirt, racePropsGetter),
                new (OpCodes.Callvirt, humanlikeGetter)
            ];

            var codes = new List<CodeInstruction>(instructions);
            bool done = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (i + 3 < codes.Count && !done)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i].OperandIs(defField) &&
                        codes[i + 1].opcode == OpCodes.Ldarg_1 &&
                        codes[i + 2].opcode == OpCodes.Ldfld && codes[i + 2].OperandIs(defField))
                    {
                        codes.RemoveRange(i, 3);
                        codes.InsertRange(i, humanlikeInstructions);
                        done = true;
                        break;
                    }
                }
            }
            if (!done) Log.Warning("Big and Small: RomanceFactor Transpiler failed. Instruction group not found. Did another mod patch it?");
            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.RomanceEligiblePair))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void RomanceEligiblePairPostfix(ref AcceptanceReport __result, Pawn initiator, Pawn target, bool forOpinionExplanation)
        {
            if (__result.Accepted && initiator != null && target != null)
            {
                if (initiator.GetStatValue(BSDefs.SM_FlirtChance, cacheStaleAfterTicks: 1000) == 0 || target.GetStatValue(BSDefs.SM_FlirtChance, cacheStaleAfterTicks: 1000) == 0)
                {
                    __result = new AcceptanceReport("CantRomanceTargetZeroChance".Translate(initiator.LabelShort, target.LabelShort));
                }
                else
                {
                    var compatiblity = initiator.GetCompatibilityWith(target, reductionScale: 1.0f, oldValue: 1);
                    if (compatiblity <= 0)
                    {
                        __result = new AcceptanceReport("CantRomanceTargetZeroChance".Translate(initiator.LabelShort, target.LabelShort));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryRomanceChanceFactor))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void RomanceFactorPostfix(ref float __result, Pawn_RelationsTracker __instance, Pawn otherPawn, Pawn ___pawn)
        {
            __result = GetLovinghanceFactor(___pawn, otherPawn, __result);
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryLovinChanceFactor))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void LovingFactorPostfix(ref float __result, Pawn_RelationsTracker __instance, Pawn otherPawn, Pawn ___pawn)
        {
            __result = GetLovinghanceFactor(___pawn, otherPawn, __result);

        }
        public static float GetLovinghanceFactor(Pawn pawn, Pawn otherPawn, float oldResult)
        {
            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && HumanoidPawnScaler.GetCacheUltraSpeed(otherPawn) is BSCache cacheTwo)
            {
                if (pawn == otherPawn || oldResult <= 0) return oldResult;

                float? compatibility = GetCompatibilityWith(pawn, otherPawn, reductionScale: 1f, oldValue: oldResult);

                if (pawn.GetStatValue(BSDefs.SM_FlirtChance, cacheStaleAfterTicks: 1000) == 0 || otherPawn.GetStatValue(BSDefs.SM_FlirtChance, cacheStaleAfterTicks: 1000) == 0)
                    return 0;
                if (compatibility == null)//  && pawn.def != otherPawn.def
                {
                    return oldResult;
                }
                oldResult *= compatibility.Value;
            }
            return oldResult;
        }

        public static float GetCompatibilityWith(this Pawn pawn, Pawn otherPawn, float reductionScale=1f, float oldValue = 0)
        {
            float ConstantPerPawnsPairCompatibilityOffset(int otherPawnID)
            {
                Rand.PushState();
                Rand.Seed = (pawn.thingIDNumber ^ otherPawnID) * 37;
                float result = Rand.GaussianAsymmetric(0.3f, 1f, 1.4f);
                Rand.PopState();
                return result;
            }
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache && HumanoidPawnScaler.GetCache(otherPawn) is BSCache cacheTwo)
            {
                if (pawn == otherPawn) return 0;

                if (pawn == null || cache.isDefaultCache || cache.romanceTags == null)
                {
                    return oldValue;
                }
                if (otherPawn == null || cacheTwo.isDefaultCache || cacheTwo.romanceTags == null)
                {
                    return oldValue;
                }

                float? compatibility = RomanceTagsExtensions.GetHighestSharedTag(cache, cacheTwo);
                if (compatibility == null)// && pawn.def != otherPawn.def)
                {
                    return oldValue;
                }

                float x = Mathf.Abs(cache.apparentAge - cacheTwo.apparentAge);
                float num = Mathf.Clamp(GenMath.LerpDouble(0f, 20f, 0.45f, -0.45f, x), -0.45f, 0.45f);
                float num2 = ConstantPerPawnsPairCompatibilityOffset(otherPawn.thingIDNumber);

                if (compatibility < 1)
                {
                    compatibility = Mathf.Lerp(1, compatibility.Value, reductionScale);
                    return (num + num2) * compatibility.Value;
                }
                return Mathf.Max((num + num2) * compatibility.Value, oldValue);
            }
            return oldValue;
        }

        

    }
}
