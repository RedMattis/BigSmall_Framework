using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BigAndSmall
{
    public class CompProperties_AbilityFleckOnTargetFixed : CompProperties_AbilityFleckOnTarget
    {
        public CompProperties_AbilityFleckOnTargetFixed()
        {
            compClass = typeof(CompAbilityEffect_FleckOnTargetFixed);
        }
    }
    public class CompAbilityEffect_FleckOnTargetFixed : CompAbilityEffect_FleckOnTarget
    {
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return true;
        }
    }

    public class CompProperties_AbilityFleckOnSelf : CompProperties_AbilityFleckOnTargetFixed
    {
        public CompProperties_AbilityFleckOnSelf()
        {
            compClass = typeof(CompAbilityEffect_FleckOnSelfFixed);
        }
    }
    public class CompAbilityEffect_FleckOnSelfFixed : CompAbilityEffect_FleckOnTargetFixed
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(parent.pawn, parent.pawn);
        }
    }
}
