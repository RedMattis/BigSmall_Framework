using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class GenerateNewPawnInternal
    {
        // private static Pawn TryGenerateNewPawnInternal(ref PawnGenerationRequest request, out string error, bool ignoreScenarioRequirements, bool ignoreValidator)
        [HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
        [HarmonyPrefix]
        public static void GenerateNewPawnInternalPrefix(ref Pawn __result, ref PawnGenerationRequest request)
        {
            try
            {
                var race = request.KindDef?.race;
                if (race != null && race.GetRaceExtensions()?.FirstOrDefault() is RaceExtension raceExtension)
                {
                    if (raceExtension.femaleGenderChance != null && request.FixedGender == null)
                    {
                        bool forceFemale = Rand.Value < raceExtension.femaleGenderChance;
                        request.FixedGender = forceFemale ? Gender.Female : Gender.Female;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Managed error when setting female gender chance in GenerateNewPawnInternalPrefix: {e}");
            }
        }

        // private static Pawn TryGenerateNewPawnInternal(ref PawnGenerationRequest request, out string error, bool ignoreScenarioRequirements, bool ignoreValidator)
        [HarmonyPatch(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
        [HarmonyPrefix]
        public static void GiveAppropriateBioAndNameToPrefix(Pawn pawn, FactionDef factionType, PawnGenerationRequest request, XenotypeDef xenotype = null)
        {
            if (xenotype != null)
            {
                try
                {
                    var pawnExtensions = xenotype.AllGenes.SelectMany(x => x.ExtensionsOnDef<PawnExtension, GeneDef>()).ToList();
                    bool foundFemale = pawnExtensions.Any(x => x.forceGender == Gender.Female);
                    bool foundMale = pawnExtensions.Any(x => x.forceGender == Gender.Male);
                    if (foundFemale && !foundMale)
                    {
                        pawn.gender = Gender.Female;
                    }
                    else if (foundMale && !foundFemale)
                    {
                        pawn.gender = Gender.Male;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Managed error in GiveAppropriateBioAndNameToPrefix when setting gender based on genes: {e}");
                }
            }
        }
    }
}
