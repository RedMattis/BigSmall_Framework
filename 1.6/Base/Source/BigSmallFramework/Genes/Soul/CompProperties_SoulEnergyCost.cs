using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_SoulEnergyCost : CompProperties_PoolCost
    {
        public CompProperties_SoulEnergyCost()
        {
            compClass = typeof(CompAbilityEffect_SoulEnergyCost);
        }
    }

    public class SoulEnergyTracker
    {
        private const int cacheTimeout = 1000;
        private int cacheTime = 0;
        protected SoulResourceHediff soulResourceHediff;
        public SoulResourceHediff Resource(Pawn pawn)
        {
            int tick = Find.TickManager.TicksGame;
            if (soulResourceHediff == null || cacheTime + cacheTimeout < tick)
            {
                soulResourceHediff = pawn.health.GetOrAddHediff(BSDefs.BS_SoulPowerHediff) as SoulResourceHediff;
                cacheTime = tick;
            }
            return soulResourceHediff;
        }
    }

    public class CompAbilityEffect_SoulEnergyCost : CompAbilityEffect_PoolCost
    {
        public new CompProperties_SoulEnergyCost Props => (CompProperties_SoulEnergyCost)props;
        protected readonly SoulEnergyTracker soulTracker = new();
        protected SoulResourceHediff Resource => soulTracker.Resource(parent.pawn);
        protected override bool HasEnoughResource
        {
            get
            {
                return Resource is SoulResourceHediff srh && srh.Value >= Props.resourceCost;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
                Resource.Value -= Props.resourceCost;
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (Resource.Value < Props.resourceCost)
            {
                reason = "BS_NotEnoughSoulEnergy".Translate();
                return true;
            }
            float num = TotalostOfQueuedAbilities();
            float num2 = Props.resourceCost + num;
            if (Props.resourceCost > float.Epsilon && num2 > Resource.Value)
            {
                reason = "BS_NotEnoughSoulEnergy".Translate();
                return true;
            }
            reason = null;
            return false;
        }
    }

    public class Comp_DrainsSoulEnergy : Comp_DrainsResource
    {
        protected readonly SoulEnergyTracker soulTracker = new();
        protected SoulResourceHediff Resource => soulTracker.Resource(parent.pawn);
        protected override void DrainResource()
        {
            if (Resource != null)
            {
                Resource.Value -= CompProperties_DrainResource.drainAmount;
                if (Resource.Value <= 0 && Props.removeOnZero)
                {
                    parent.pawn.health.RemoveHediff(parent);
                }
            }
        }
    }
}
