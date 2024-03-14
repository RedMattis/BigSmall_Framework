using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    public class BS_GeneCursedPower : BS_GenericResource, IGeneResourceDrain
    {
        protected override Color BarColor => new ColorInt(128, 3, 128).ToColor;
        protected override Color BarHighlightColor => new ColorInt(148, 22, 148).ToColor;
        
        public Gene_Resource Resource => this;

        public bool CanOffset
        {
            get
            {
                if (Active)
                {
                    return !pawn.Deathresting;
                }

                return false;
            }
        }

        public float ResourceLossPerDay => def.resourceLossPerDay;

        public Pawn Pawn => pawn;

        public string DisplayLabel => Label + " (" + "Gene".Translate() + ")";

        public override void Tick()
        {
            base.Tick();
            GeneResourceDrainUtility.TickResourceDrain(this);
        }
    }

    public class CompProperties_CursedCost : CompProperties_PoolCost
    {
        public CompProperties_CursedCost()
        {
            compClass = typeof(CompAbilityEffect_CursedCost);
        }
    }

    public class CompAbilityEffect_CursedCost : CompAbilityEffect_PoolCost
    {
        public new CompProperties_CursedCost Props => (CompProperties_CursedCost)props;
        protected override bool HasEnoughResource
        {
            // Replace in inherited class.
            get
            {
                BS_GeneCursedPower cPower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneCursedPower>();
                return cPower != null && cPower.Value >= Props.resourceCost;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            BS_GeneCursedPower cursedPower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneCursedPower>();
            ResourcePoolUtils.OffsetResource(parent.pawn, 0f - Props.resourceCost, cursedPower);
        }

        public override bool GizmoDisabled(out string reason)
        {
            BS_GeneCursedPower cPower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneCursedPower>();
            if (cPower == null)
            {
                reason = "Ability Disabled: Missing Cursed Power Gene";
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
