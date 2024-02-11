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
                    if (pawn.story.bodyType.defName == BodyTypeDefOf.Male.defName)
                        pawn.story.bodyType = BodyTypeDefOf.Female;
                }
                else
                {
                    pawn.gender = Gender.Male;
                    if (pawn.story.bodyType.defName == BodyTypeDefOf.Female.defName)
                        pawn.story.bodyType = BodyTypeDefOf.Male;
                }

                if (pawn.story.headType.gender != 0 && pawn.story.headType.gender != pawn.gender && !pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs.Where((HeadTypeDef x) => x.randomChosen)))
                {
                    Log.Warning("Couldn't find an appropriate head after changing pawn gender.");
                }
                if (!pawn.style.CanWantBeard && pawn.style.beardDef != BeardDefOf.NoBeard)
                {
                    pawn.style.beardDef = BeardDefOf.NoBeard;
                }
            }
            catch
            {
                Log.Error($"Error when gender-bending {pawn.LabelShortCap}");
            }
            pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();

            //if (pawn.story.headType.gender != 0 && pawn.story.headType.gender != pawn.gender && !pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs.Where((HeadTypeDef x) => x.randomChosen)))
            //{
            //    Log.Warning("Couldn't find an appropriate head after changing pawn gender.");
            //}
            //if (!pawn.style.CanWantBeard && pawn.style.beardDef != BeardDefOf.NoBeard)
            //{
            //    pawn.style.beardDef = BeardDefOf.NoBeard;
            //}
        }
    }
}
