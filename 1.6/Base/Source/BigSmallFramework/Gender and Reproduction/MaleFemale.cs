namespace BigAndSmall.SpecialGenes.Gender
{
    using HarmonyLib;
    using Verse;


    // Not totally sure what this tiny snippet of code does, but someone in the comment section of my mod said this AG code fixed
    // A bug with pawn relations, so I'm just copy-pasting it here.
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawnRelations")]
    [HarmonyPriority(int.MaxValue)]
    public static class PawnGenerator_GeneratePawnRelations_Patch
    {
        [HarmonyPrefix]
        public static bool DisableRelationsPrefix(Pawn pawn)
        {
            if (pawn.HasActiveGene(BSDefs.Body_FemaleOnly) || pawn.HasActiveGene(BSDefs.Body_MaleOnly))
            {
                return false;
            }
            return true;
        }
    }
}
