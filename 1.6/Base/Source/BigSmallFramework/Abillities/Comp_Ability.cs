using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall.Abillities
{
    public abstract class CompProperties_AbilityJumpAndUseOn : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityJumpAndUseOn()
        {
            compClass = typeof(CompAbilityEffect_JumpAndUseOn);
        }
    }

    public abstract class CompAbilityEffect_JumpAndUseOn : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
    {
        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            try
            {
                Pawn tPawn = target.Pawn;
                if (tPawn != null)
                {
                    ApplyEffect(origin, target);
                }
            }
            catch (Exception e)
            {
                // Capture the error if any, because otherwise the pawn will fail to spawn back in which is bad.
                Log.Error($"Error in {nameof(CompAbilityEffect_JumpAndUseOn)} (target {target.Pawn}, user: {parent?.pawn}).\n{e.Message}\n{e.StackTrace}");
            }
        }

        public abstract void ApplyEffect(IntVec3 origin, LocalTargetInfo target);
    }

}
