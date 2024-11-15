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
    [HarmonyPriority(1000)]
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

    //[HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    public static class Patch_NotifyGendesChanged_Gender
    {
        //[HarmonyPostfix]
        public static void RunInPostfix(Pawn pawn, GeneDef addedOrRemovedGene)
        {
            bool update = false;
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                var apparentGender = cache.apparentGender;
                if (addedOrRemovedGene == BSDefs.Body_FemaleOnly)
                {
                    pawn.gender = Gender.Female;
                    //if (___pawn.story.bodyType == BodyTypeDefOf.Male && apparentGender != Gender.Male)
                    //{
                    //    ___pawn.story.bodyType = BodyTypeDefOf.Female;
                    //}
                    update = true;
                }
                else if (addedOrRemovedGene == BSDefs.Body_MaleOnly && pawn.gender != Gender.Male)
                {
                    pawn.gender = Gender.Male;
                    //if (___pawn.story.bodyType == BodyTypeDefOf.Female && apparentGender != Gender.Female)
                    //{
                    //    ___pawn.story.bodyType = BodyTypeDefOf.Male;
                    //}
                    update = true;
                }
                if (addedOrRemovedGene == BSDefs.Body_Androgynous ||
                    addedOrRemovedGene.modExtensions?
                        .Any(x => x is PawnExtension pExt && pExt.ApparentGender is Gender) == true)
                {
                    update = true;
                }
                if (update)
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
        }
    }

}
