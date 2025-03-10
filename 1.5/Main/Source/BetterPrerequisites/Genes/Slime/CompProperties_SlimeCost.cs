using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_SlimeCost : CompProperties_PoolCost
    {
        public CompProperties_SlimeCost()
        {
            compClass = typeof(CompAbilityEffect_SlimeCost);
        }
    }

    public class CompAbilityEffect_SlimeCost : CompAbilityEffect_PoolCost
    {
        public new CompProperties_SlimeCost Props => (CompProperties_SlimeCost)props;
        protected override bool HasEnoughResource
        {
            // Replace in inherited class.
            get
            {
                BS_GeneSlimePower cPower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneSlimePower>();
                return cPower != null && cPower.Value >= Props.resourceCost;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            BS_GeneSlimePower slimePower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneSlimePower>();
            ResourcePoolUtils.OffsetResource(parent.pawn, 0f - Props.resourceCost, slimePower);
            slimePower.GetSlimeHediff().Severity = Mathf.Clamp(slimePower.Value, 0.05f, 9999);
        }

        public override bool GizmoDisabled(out string reason)
        {
            BS_GeneSlimePower cPower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneSlimePower>();
            if (cPower == null)
            {
                reason = "Ability Disabled: Missing Required Power Gene";
                return true;
            }
            if (cPower.Value < Props.resourceCost)
            {
                reason = "Ability Disabled: Not enough Power";
                return true;
            }
            float num = TotalostOfQueuedAbilities();
            float num2 = Props.resourceCost + num;
            if (Props.resourceCost > float.Epsilon && num2 > cPower.Value)
            {
                reason = "Ability Disabled: Not enough Power";
                return true;
            }
            reason = null;
            return false;
        }
    }
}
