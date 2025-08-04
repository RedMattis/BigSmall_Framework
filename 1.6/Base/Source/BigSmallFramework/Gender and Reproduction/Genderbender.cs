using RimWorld;
using System;
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

        public static void GenderBend(Pawn pawn)
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
                HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
                GenderMethods.UpdateBodyHeadAndBeardPostGenderChange(pawn, force:true);
            }
            catch (Exception e)
            {
                Log.Error($"Error when gender-bending {pawn.LabelShortCap}\n{e.Message}\n{e.StackTrace}");
            }
            pawn.Drawer.renderer.SetAllGraphicsDirty();

        }

        
    }
}
