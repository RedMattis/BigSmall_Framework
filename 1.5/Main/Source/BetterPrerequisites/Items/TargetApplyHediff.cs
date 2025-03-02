using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_TargetAddHediff : CompProperties
    {
        public HediffDef hediffDef;

        public CompProperties_TargetAddHediff()
        {
            compClass = typeof(CompUseEffect_TargetAddHediff);
        }
    }

    public class CompUseEffect_TargetAddHediff : CompTargetEffect
    {
        public CompProperties_TargetAddHediff Props => (CompProperties_TargetAddHediff)props;

        public override void DoEffectOn(Pawn _, Thing target)
        {
            if (target is Pawn pawn)
            {
                pawn.health.AddHediff(Props.hediffDef);
            }
        }
    }

}