using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    

    public abstract class CompProperties_PoolCost : CompProperties_AbilityEffect
    {
        public float resourceCost;

        public CompProperties_PoolCost()
        {
            compClass = typeof(CompAbilityEffect_PoolCost);
        }

        public override IEnumerable<string> ExtraStatSummary()
        {
            yield return (string)("AbilityPoolCost".Translate() + ": ") + Mathf.RoundToInt(resourceCost * 100f);
        }
    }

    public abstract class CompAbilityEffect_PoolCost : CompAbilityEffect
    {
        public new CompProperties_PoolCost Props => (CompProperties_PoolCost)props;

        protected abstract bool HasEnoughResource { get; }

        public override bool CanCast => HasEnoughResource && base.CanCast;

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return HasEnoughResource;
        }

        protected float TotalostOfQueuedAbilities()
        {
            object obj = parent.pawn.jobs?.curJob?.verbToUse;
            float num;
            if (!(obj is Verb_CastAbility verb_CastAbility))
            {
                num = 0f;
            }
            else
            {
                Ability ability = verb_CastAbility.ability;
                num = ((ability != null) ? ResourcePoolUtils.PoolCost(ability) : 0f);
            }
            float num2 = num;
            if (parent.pawn.jobs != null)
            {
                for (int i = 0; i < parent.pawn.jobs.jobQueue.Count; i++)
                {
                    if (parent.pawn.jobs.jobQueue[i].job.verbToUse is Verb_CastAbility verb_CastAbility2)
                    {
                        float num3 = num2;
                        Ability ability2 = verb_CastAbility2.ability;
                        num2 = num3 + ((ability2 != null) ? ResourcePoolUtils.PoolCost(ability2) : 0f);
                    }
                }
            }
            return num2;
        }
    }

}
