using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigAndSmall.SpecialGenes.Gender
{
    using HarmonyLib;
    using RimWorld;
    using Verse;


    // Not totally sure what this tiny snippet of code does, but someone in the comment section of my mod said this AG code fixed
    // A bug with pawn relations, so I'm just copy-pasting it here.
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawnRelations")]
    public static class AlphaGenes_PawnGenerator_GeneratePawnRelations_Patch
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


    // This is just taken from the Genes mod. Integrated because my users kept complaining about the dependency.
    [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    public static class Pawn_GeneTracker__Notify_GenesChanged
    {
        [HarmonyPostfix]
        public static void Postfix(ref Pawn ___pawn, GeneDef addedOrRemovedGene)
        {
            bool flag = false;
            if (addedOrRemovedGene == BSDefs.Body_FemaleOnly && ___pawn.gender != Gender.Female)
            {
                ___pawn.gender = Gender.Female;
                if (___pawn.story.bodyType == BodyTypeDefOf.Male)
                {
                    ___pawn.story.bodyType = BodyTypeDefOf.Female;
                }
                flag = true;
            }
            else if (addedOrRemovedGene == BSDefs.Body_MaleOnly && ___pawn.gender != Gender.Male)
            {
                ___pawn.gender = Gender.Male;
                if (___pawn.story.bodyType == BodyTypeDefOf.Female)
                {
                    ___pawn.story.bodyType = BodyTypeDefOf.Male;
                }
                flag = true;
            }
            if (flag)
            {
                if (___pawn.story.headType.gender != 0 && ___pawn.story.headType.gender != ___pawn.gender && !___pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs.Where((HeadTypeDef x) => x.randomChosen))) //.Where((HeadTypeDef x) => x.randomChosen)
                {
                    Log.Warning("Couldn't find an appropriate head after changing pawn gender.");
                }
                if (!___pawn.style.CanWantBeard && ___pawn.style.beardDef != BeardDefOf.NoBeard)
                {
                    ___pawn.style.beardDef = BeardDefOf.NoBeard;
                }
                ___pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
            }
        }
    }

}
