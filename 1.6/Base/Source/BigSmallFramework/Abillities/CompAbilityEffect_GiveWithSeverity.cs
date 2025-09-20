
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_AbilityGiveHediffComplex : CompProperties_AbilityGiveHediff
    {
        public class OffsetSeverityByStats
        {
            public StatDef stat;
            public float multiplier;
        }
        public class OffsetSeverityByBodySize
        {
            public float multiplier;
        }
        public List<OffsetSeverityByStats> offsetSeverityByStats = [];
        public float offsetSeverityBodySizeFactor = 0;
        public bool hediffStacks = false;


        [Obsolete]
        public StatDef offsetSeverityByStat = null;
        [Obsolete]
        public bool offsetSeverityBodySize = false;
    }

    public class CompAbilityEffect_GiveHediffComplex : CompAbilityEffect_WithDuration
    {
        public new CompProperties_AbilityGiveHediffComplex Props => (CompProperties_AbilityGiveHediffComplex)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (!Props.ignoreSelf || target.Pawn != parent.pawn)
            {
                if (!Props.onlyApplyToSelf && Props.applyToTarget)
                {
                    ApplyInner(target.Pawn, parent.pawn);
                }

                if (Props.applyToSelf || Props.onlyApplyToSelf)
                {
                    ApplyInner(parent.pawn, target.Pawn);
                }
            }
        }

        protected void ApplyInner(Pawn target, Pawn other)
        {
            if (target == null)
            {
                return;
            }

            if (TryResist(target))
            {
                MoteMaker.ThrowText(target.DrawPos, target.Map, "Resisted".Translate());
                return;
            }

            if (Props.replaceExisting)
            {
                Hediff firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
                if (firstHediffOfDef != null)
                {
                    target.health.RemoveHediff(firstHediffOfDef);
                }
            }

            Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, target, Props.onlyBrain ? target.health.hediffSet.GetBrain() : null);
            HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (hediffComp_Disappears != null)
            {
                hediffComp_Disappears.ticksToDisappear = GetDurationSeconds(target).SecondsToTicks();
            }

            SetSeverity(target, hediff);
            
            HediffComp_Link hediffComp_Link = hediff.TryGetComp<HediffComp_Link>();
            if (hediffComp_Link != null)
            {
                hediffComp_Link.other = other;
                hediffComp_Link.drawConnection = target == parent.pawn;
            }

            target.health.AddHediff(hediff);
        }

        private void SetSeverity(Pawn target, Hediff hediff)
        {
            if (Props.severity >= 0f)
            {
                hediff.Severity = Props.severity;
            }
            foreach (var offset in Props.offsetSeverityByStats)
            {
                if (offset.stat != null)
                {
                    float statScale = target.GetStatValue(offset.stat) * offset.multiplier;
                    hediff.Severity += statScale;
                }
            }
            if (Props.offsetSeverityBodySizeFactor != 0)
            {
                hediff.Severity += target.BodySize * Props.offsetSeverityBodySizeFactor;
            }
            SetSeverityLegacy(target, hediff);
        }

        private void SetSeverityLegacy(Pawn target, Hediff hediff)
        {
            if (Props.offsetSeverityByStat != null)
            {
                float statScale = target.GetStatValue(Props.offsetSeverityByStat);
                hediff.Severity += statScale;
            }
            if (Props.offsetSeverityBodySize)
            {
                hediff.Severity += target.BodySize;
            }
        }

        protected virtual bool TryResist(Pawn pawn)
        {
            return false;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (Props.onlyApplyToSelf)
            {
                target = parent.pawn;
            }
            if (!Props.hediffStacks)
            {
                if (target.Pawn != null && target.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef) != null)
                {
                    return false;
                }
            }

            return target.Pawn != null;
        }
    }

}