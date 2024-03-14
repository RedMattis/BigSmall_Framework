using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_CutBond : CompProperties_AbilityEffect
    {
        public CompProperties_CutBond()
        {
            compClass = typeof(CompProperties_CutBondEffect);
        }
    }
    public class CompProperties_CutBondEffect : CompAbilityEffect
    {
        public new CompProperties_CutBond Props => (CompProperties_CutBond)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;

            Pawn partner = null;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) is Hediff_PsychicBond hediff_PsychicBond)
            {
                partner = hediff_PsychicBond.target as Pawn;
                pawn.health.RemoveHediff(hediff_PsychicBond);
            }

            Hediff parasiticBond = pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_SuccubusBond);
            if (parasiticBond != null)
            {
                pawn.health.RemoveHediff(parasiticBond);
            }

            if (partner != null)
            {
                if (partner.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) is Hediff_PsychicBond hediff_PsychicBond2)
                {
                    partner.health.RemoveHediff(hediff_PsychicBond2);
                }
                Hediff parasiticVictim = partner.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_SuccubusBond_Victim);
                if (parasiticVictim != null)
                {
                    partner.health.RemoveHediff(parasiticVictim);
                }
            }            
        }
    }
}

