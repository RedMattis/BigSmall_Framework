using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static RimWorld.ColonistBar;

namespace BigAndSmall
{
    public class CompProperties_AbilityEngluf : CompProperties_AbilityEffect
    {
        public float relativeSizeThreshold = 0.8f;
        public float? max = null;
        public int maxAgeStage = 3;
        public float internalBaseDamage = 10f;
        public float selfDamageMultiplier = 0.2f;
        public DamageDef damageDef = null;

        public bool alliesAttackBack = true;
        public bool dealsDamage = true;
        public float healPerDay = -1; // hp per day. -1 for no healing
        public float regularHealingMultiplier = -1; // hp per day. -1 for no healing
        public bool healsScars = false;
        public bool canHealBrain = false;
        public float bodyPartsRegeneratedPerDay = 0;

        public CompProperties_AbilityEngluf()
        {
            compClass = typeof(CompAbilityEffect_SlimeEngluf);
        }
    }

    public class CompAbilityEffect_SlimeEngluf : CompAbilityEffect
    {
        public new CompProperties_AbilityEngluf Props => (CompProperties_AbilityEngluf)props;


        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                DoEngulf(parent.pawn, pawn);
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn enemy = target.Pawn;
            if (enemy == null)
            {
                return false;
            }
            if (enemy.BodySize > parent.pawn.BodySize * Props.relativeSizeThreshold)
            {
                if (throwMessages)
                {
                    Messages.Message("MessagerTargetTooBigToEngulf".Translate(enemy.Label), enemy, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            // Check if the parent _has_ the digestion capacity. If it does not it is probably a mechanoid or something and can be presumed to use a furnace or something.
            if (parent.pawn.health.capacities.CapableOf(PawnCapacityDefOf.Metabolism))
            {
                var digestCapacity = parent.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Metabolism);
                if (digestCapacity <= 0.55f)
                {
                    Messages.Message("DigestiveAbillityTooLow".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                    return false;
                }
            }
            // Check if the target will fit in the capacity of the existing hediff (if any)
            var hediff = parent.pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("BS_Engulfed"));
            if (hediff != null)
            {
                var engulfHediff = (EngulfHediff)hediff;
                if (engulfHediff.TotalMass + enemy.BodySize > engulfHediff.MaxCapacity)
                {
                    if (throwMessages)
                    {
                        Messages.Message("BS_NotEnoughRoom".Translate(enemy.Label), enemy, MessageTypeDefOf.RejectInput, historical: false);
                    }
                    return false;
                }
            }
            return true;
        }

        public void DoEngulf(Pawn attacker, Pawn victim)
        {

            // Get all hediffs in the library
            var hediffs = DefDatabase<HediffDef>.AllDefsListForReading;

            // Get the hediff with the defname of "BS_Engulfed"
            var hediffList = hediffs.Where(x => x.defName == "BS_Engulfed");

            if (hediffList.Count() == 0)
            {
                Log.Error("BS_Engulfed hediff not found in the library.");
                return;
            }
            // Add hediff to attacker
            var hediff = hediffList.First();

            EngulfHediff engulfHediff;

            // Check if we already have the hediff
            if (attacker.health.hediffSet.HasHediff(hediff))
            {
                // Get the hediff we added
                engulfHediff = (EngulfHediff)attacker.health.hediffSet.GetFirstHediffOfDef(hediff);
                engulfHediff.Severity = 1;
            }
            else
            {
                attacker.health.AddHediff(hediff);
                engulfHediff = (EngulfHediff)attacker.health.hediffSet.GetFirstHediffOfDef(hediff);
            }

            engulfHediff.selfDamageMultiplier = Props.selfDamageMultiplier;
            engulfHediff.internalBaseDamage = Props.internalBaseDamage;
            engulfHediff.baseCapacity = Props.max != null ? Props.max.Value : Props.relativeSizeThreshold ;
            engulfHediff.damageDef = Props.damageDef;

            engulfHediff.alliesAttackBack = Props.alliesAttackBack;
            engulfHediff.dealsDamage = Props.dealsDamage;
            engulfHediff.healPerDay = Props.healPerDay;
            engulfHediff.regularHealingMultiplier = Props.regularHealingMultiplier;
            engulfHediff.healsScars = Props.healsScars;
            engulfHediff.canHealBrain = Props.canHealBrain;
            engulfHediff.bodyPartsRegeneratedPerDay = Props.bodyPartsRegeneratedPerDay;
            // Add the victim to the hediff's inner container
            engulfHediff.Engulf(victim);

        }
    }

    public class CompProperties_AbilityRegurgitate : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityRegurgitate()
        {
            compClass = typeof(CompAbilityEffect_SlimeRegurgitate);
        }
    }
    public class CompAbilityEffect_SlimeRegurgitate : CompAbilityEffect
    {
        public new CompProperties_AbilityRegurgitate Props => (CompProperties_AbilityRegurgitate)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;

            var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.Where(x => x.defName == "BS_Engulfed");
            // Remove the hediff if it exists
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffs.FirstOrDefault());
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }

            // Make pawn vomit
            var vomit = JobMaker.MakeJob(JobDefOf.Vomit);

            // Remove all jobs to avoid the pawn attemption this action due to being interrupted by the vomit job.
            pawn.jobs.StopAll();
            pawn.jobs.StartJob(vomit, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);

        }
    }
}
