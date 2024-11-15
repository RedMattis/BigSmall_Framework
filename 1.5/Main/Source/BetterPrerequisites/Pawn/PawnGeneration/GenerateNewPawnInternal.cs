using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class GenerateNewPawnInternal
    {
        // private static Pawn TryGenerateNewPawnInternal(ref PawnGenerationRequest request, out string error, bool ignoreScenarioRequirements, bool ignoreValidator)
        [HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
        [HarmonyPrefix]
        public static void GenerateNewPawnInternalPostfix(ref Pawn __result, ref PawnGenerationRequest request)
        {
            var thingDef = request.KindDef?.race;
            if (thingDef != null && thingDef.GetRaceExtensions()?.FirstOrDefault() is RaceExtension raceExtension)
            {
                if (raceExtension.femaleGenderChance != null && request.FixedGender == null)
                {
                    bool forceFemale = Rand.Value < raceExtension.femaleGenderChance;
                    request.FixedGender = forceFemale ? Gender.Female : Gender.Female;
                }
            }
        }
    }
}
