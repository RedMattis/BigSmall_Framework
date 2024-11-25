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
                GenderMethods.UpdateBodyHeadAndBeardPostGenderChange(pawn, force:true);
            }
            catch
            {
                Log.Error($"Error when gender-bending {pawn.LabelShortCap}");
            }
            pawn.Drawer.renderer.SetAllGraphicsDirty();

        }

        
    }
}
