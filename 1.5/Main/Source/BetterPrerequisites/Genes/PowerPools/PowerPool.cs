using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class ResourcePoolUtils
    {
        public static void OffsetResource(Pawn pawn, float gain, BS_GenericResource gene)
        {
            gene.Value += gain;
        }

        public static float PoolCost(Ability ab)
        {
            if (ab.comps != null)
            {
                foreach (AbilityComp comp in ab.comps)
                {
                    if (comp is CompAbilityEffect_PoolCost compAbilityEffect_PoolCost)
                    {
                        return compAbilityEffect_PoolCost.Props.resourceCost;
                    }
                }
            }
            return 0f;
        }
    }

    public abstract class BS_GenericResource : Gene_Resource
    {
        public override float InitialResourceMax => 1f;
        public override float MinLevelForAlert => 0f;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!Active)
            {
                yield break;
            }
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }
    }

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
