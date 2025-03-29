
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
        public StatDef offsetSeverityByStat = null;
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

            if (Props.severity >= 0f)
            {
                hediff.Severity = Props.severity;
            }
            if (Props.offsetSeverityByStat != null)
            {
                float statScale = target.GetStatValue(Props.offsetSeverityByStat);
                hediff.Severity += statScale;
            }
            if (Props.offsetSeverityBodySize)
            {
                hediff.Severity += target.BodySize;
            }

            HediffComp_Link hediffComp_Link = hediff.TryGetComp<HediffComp_Link>();
            if (hediffComp_Link != null)
            {
                hediffComp_Link.other = other;
                hediffComp_Link.drawConnection = target == parent.pawn;
            }

            target.health.AddHediff(hediff);
        }

        protected virtual bool TryResist(Pawn pawn)
        {
            return false;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }

            return target.Pawn != null;
        }
    }

}