using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    // Not totally sure what this tiny snippet of code does, but someone in the comment section of my mod said this AG code fixed
    // A bug with pawn relations, so I'm just copy-pasting it here.
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawnRelations")]
    [HarmonyPriority(int.MaxValue)]
    public static class PawnGenerator_GeneratePawnRelations_Patch
    {
        [HarmonyPrefix]
        public static bool DisableRelationsForForceGenderedPawns(Pawn pawn)
        {
            if (pawn == null) return true;
            if (pawn.HasActiveGene(BSDefs.Body_FemaleOnly) || pawn.HasActiveGene(BSDefs.Body_MaleOnly))
            {
                return false;
            }
            try
            {
                var pawnDef = pawn.def;
                if (pawnDef != null && pawnDef.GetRaceExtensions()?.FirstOrDefault() is RaceExtension raceExtension)
                {
                    if (raceExtension.femaleGenderChance != null)
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Managed error in PawnGenerator_GeneratePawnRelations_Patch:\n{e.Message}\n{e.StackTrace}");
            }
            return true;
        }
    }
}
