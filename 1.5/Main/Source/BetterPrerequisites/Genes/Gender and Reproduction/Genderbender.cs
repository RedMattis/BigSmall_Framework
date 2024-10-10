using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class Genderbender_AbilityEffect : CompProperties_AbilityEffect
    {

        public Genderbender_AbilityEffect()
        {
            compClass = typeof(Genderbender);
        }
    }

    public class Genderbender : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null) pawn = dest.Pawn;
            if (pawn != null)
            {
                GenderBend(pawn);
            }
        }

        public void GenderBend(Pawn pawn)
        {
            try
            {
                if (pawn.gender == Gender.Male)
                {
                    pawn.gender = Gender.Female;
                }
                else
                {
                    pawn.gender = Gender.Male;
                }
                GenderHelper.UpdateBodyHeadAndBeardPostGenderChange(pawn);
            }
            catch
            {
                Log.Error($"Error when gender-bending {pawn.LabelShortCap}");
            }
            pawn.Drawer.renderer.SetAllGraphicsDirty();

        }

        
    }

    public static class GenderHelper
    {
        public static GeneticBodyType BodyTypeToGeneticBodyType(this BodyTypeDef bodyType)
        {
            if (bodyType == BodyTypeDefOf.Fat)
            {
                return GeneticBodyType.Fat;
            }
            else if (bodyType == BodyTypeDefOf.Hulk)
            {
                return GeneticBodyType.Hulk;
            }
            else if (bodyType == BodyTypeDefOf.Thin)
            {
                return GeneticBodyType.Thin;
            }
            else
            {
                return GeneticBodyType.Standard;
            }
        }

        public static void UpdateBodyHeadAndBeardPostGenderChange(Pawn pawn)
        {
            bool headNeedsChange = pawn.story.headType.gender != 0 && pawn.story.headType.gender != pawn.gender;

            var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
            // Set body type.
            if (activeGenes.Any(x => x.def.bodyType != null))
            {
                var bodyType = activeGenes.First(x => x.def.bodyType != null).def.bodyType;
                if (bodyType != null)
                {
                    pawn.story.bodyType = bodyType.Value.ToBodyType(pawn);
                }
                else // Shouldn't happen, but just in case.
                {
                    pawn.story.bodyType = PawnGenerator.GetBodyTypeFor(pawn);//.BodyTypeToGeneticBodyType().ToBodyType(pawn);
                }
            }
            else
            {
                pawn.story.bodyType = PawnGenerator.GetBodyTypeFor(pawn);//.BodyTypeToGeneticBodyType().ToBodyType(pawn);
            }


            // If we have a head gene we don't want to use a randomchosen head.
            var headGenes = activeGenes.Where(x => !x.def.forcedHeadTypes.NullOrEmpty());
            var possibleHeads = headGenes.SelectMany(x => x.def.forcedHeadTypes).ToList();
            bool androgynous = activeGenes.Any(x => x.def == BSDefs.Body_Androgynous);

            if (possibleHeads.Count > 0)
            {
                Gender targetGender = pawn.gender;
                if (androgynous)
                {
                    targetGender = Gender.Female;
                }
                var validHeads = possibleHeads.Where(x => headGenes.All(ag => ag.def.forcedHeadTypes.Contains(x))).Where(x => x.gender == Gender.None || x.gender == targetGender).ToList();
                if (validHeads.Count > 0)
                {
                    Rand.PushState(pawn.thingIDNumber);
                    pawn.story.headType = validHeads.RandomElement();
                    Rand.PopState();
                    headNeedsChange = false;
                }
                else
                {
                    Log.Warning($"Couldn't find an appropriate head fitting {pawn}'s genes.");
                }
            }
            if (headNeedsChange && !pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs.Where((HeadTypeDef x) => x.randomChosen)))
            {
                if (!pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs.Where((HeadTypeDef x) => x.randomChosen)))
                {
                    Log.Warning($"Couldn't find an appropriate head for {pawn}.");
                }

            }
            if (!pawn.style.CanWantBeard && pawn.style.beardDef != BeardDefOf.NoBeard)
            {
                pawn.style.beardDef = BeardDefOf.NoBeard;
            }
        }
    }
}
