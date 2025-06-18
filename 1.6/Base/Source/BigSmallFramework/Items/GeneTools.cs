using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace BigAndSmall
{
    public class CompTargetEffect_Discombobulate : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is Pawn pawn) Discombobulator.Discombobulate(pawn);
        }
    }

    public class CompTargetEffect_IntegrateGenes : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is Pawn pawn) Discombobulator.IntegrateGenes(pawn);
        }
    }

    public class CompTargetEffect_XenoCopy : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is Pawn pawn) Discombobulator.XenoCopy(pawn);
        }
    }
}
