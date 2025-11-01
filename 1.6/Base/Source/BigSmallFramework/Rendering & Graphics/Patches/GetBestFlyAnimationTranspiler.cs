using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using System.Reflection.Emit;
using System.Reflection;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class GetBestFlyAnimationTranspiler
    {
        public static AnimationDef GetBestFlyAnimation_ForHumanlikeAnimal(Pawn pawn, Rot4? facingOverride = null)
        {
            if (pawn.ageTracker == null)
                return null;

            if (!HumanlikeAnimalGenerator.humanlikeAnimals.TryGetValue(pawn.def, out HumanlikeAnimal hueAni))
                return null;

            int lifeStageIndex = hueAni.GetLifeStageIndex(pawn);
            var lifeStages = hueAni.animalKind?.lifeStages;
            if (lifeStages == null)
                return null;

            Rot4 facing = facingOverride ?? pawn.Rotation;
            bool isFemale = pawn.gender == Gender.Female;

            // Helper function to select animation for a given life stage
            AnimationDef SelectAnimation(PawnKindLifeStage stage)
            {
                if (stage == null) return null;

                if (facing == Rot4.South)
                    return isFemale ? stage.flyingAnimationSouthFemale ?? stage.flyingAnimationSouth : stage.flyingAnimationSouth;
                if (facing == Rot4.North)
                    return isFemale ? stage.flyingAnimationNorthFemale ?? stage.flyingAnimationNorth : stage.flyingAnimationNorth;
                // Default to East if not North/South
                return isFemale ? stage.flyingAnimationEastFemale ?? stage.flyingAnimationEast : stage.flyingAnimationEast;
            }

            // Try current and previous life stages
            for (int i = lifeStageIndex; i >= 0; i--)
            {
                var stage = lifeStages[i];
                var anim = SelectAnimation(stage);
                if (anim != null)
                {
                    return anim;
                }
            }

            return null;
        }


        [HarmonyPatch(typeof(Pawn_FlightTracker), nameof(Pawn_FlightTracker.GetBestFlyAnimation))]
        [HarmonyPrefix]
        public static bool GetBestFlyAnimation_Prefix(ref AnimationDef __result, Pawn pawn, Rot4? facingOverride = null)
        {
            if (!HumanlikeAnimalGenerator.HasHumanlikeAnimals)
            {
                return true;
            }
            __result = GetBestFlyAnimation_ForHumanlikeAnimal(pawn, facingOverride);
            if (__result != null)
            {
                return false;
            }
            return true;
        }

        // Checks for
        //if (pawn.RaceProps.Humanlike)
        //{
        //   return null;
        //}
        // in the "GetBestFlyAnimation(Pawn pawn, Rot4? facingOverride = null)" method
        // And inserts our method call before it when found.

        //[HarmonyPatch(typeof(Pawn_FlightTracker), nameof(Pawn_FlightTracker.GetBestFlyAnimation))]
        //[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var codes = new List<CodeInstruction>(instructions);
        //    //var pawnRacePropsHumanlikeMethod = AccessTools.PropertyGetter(typeof(RaceProperties), nameof(RaceProperties.Humanlike));
        //    var pawnRacePropsHumanlikeMethod = AccessTools.PropertyGetter(typeof(Pawn), nameof(RaceProperties.Humanlike));
        //    bool found = false;
        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        Log.Message($"Instruction {i}: {codes[i].opcode} {codes[i].operand}");
        //        // Look for the first humanlike check
        //        if (i + 2 < codes.Count
        //            && codes[i].opcode == OpCodes.Ldarg_0
        //            && codes[i + 2].opcode == OpCodes.Callvirt && codes[i + 2].operand as MethodInfo == pawnRacePropsHumanlikeMethod)
        //        {
        //            Log.Message("Found pawn.RaceProps.Humanlike check.");
        //            var continueLabel = new Label();
        //            codes[i].labels.Add(continueLabel);

        //            var newInstructions = new List<CodeInstruction>
        //            {
        //                new(OpCodes.Ldarg_0), // Load pawn argument
        //                new(OpCodes.Ldarg_1), // Load facingOverride argument
        //                new(OpCodes.Call, typeof(GetBestFlyAnimationTranspiler).GetMethod(nameof(GetBestFlyAnimation_ForHumanlikeAnimal))),
        //                new(OpCodes.Dup),
        //                new(OpCodes.Brfalse_S, continueLabel), // If null, continue as normal
        //                new(OpCodes.Ret),
        //            };
        //            codes.InsertRange(i, newInstructions);
        //            found = true;
        //        }
        //        if (found)
        //        {
        //            break;
        //        }
        //    }
        //    Log.Message($"Transpiler completed. Success Status: {found}");
        //    foreach(var code in codes)
        //    {
        //        Log.Message($"{code.opcode} {code.operand}");
        //    }
        //    Log.Message("Transpiler finished.");

        //    return codes.AsEnumerable();
        //}
    }
}
