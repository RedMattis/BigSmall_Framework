using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    public abstract class CompProperties_AbilityEngluf_Abstract : CompProperties_AbilityEffect
    {
        public FloatRange relativeSizeThreshold = new(0.35f, 0.8f);
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

        public float GetSizeThreshold(Pawn pawn)
        {
            var sm = HumanoidPawnScaler.GetCacheUltraSpeed(pawn, canRegenerate:false).scaleMultiplier.linear;
            float divisor = StatWorker_MaxNutritionFromSize.GetNutritionMultiplier(sm);
            float mnutrition = pawn.GetStatValue(StatDefOf.MaxNutrition) / divisor * pawn.def.GetStatValueAbstract(StatDefOf.MaxNutrition);
            float asRange = (mnutrition - 1) / 4;
            return relativeSizeThreshold.ClampToRange(relativeSizeThreshold.LerpThroughRange(asRange));
        }
    }
    public class CompProperties_AbilityEnglufJump : CompProperties_AbilityEngluf_Abstract
    {
        public CompProperties_AbilityEnglufJump()
        {
            compClass = typeof(CompAbilityEffect_SlimeEnglufJump);
        }
    }

    public class CompProperties_AbilityEngluf : CompProperties_AbilityEngluf_Abstract
    {
        public CompProperties_AbilityEngluf()
        {
            compClass = typeof(CompAbilityEffect_SlimeEngluf);
        }
    }

    public class CompAbilityEffect_SlimeEnglufJump : CompAbilityEffect_SlimeEngluf_Abstract, ICompAbilityEffectOnJumpCompleted
    {
        public override CompProperties_AbilityEngluf_Abstract Props => (CompProperties_AbilityEnglufJump)props;

        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            
            try
            {
                Pawn tPawn = target.Pawn;
                if (tPawn != null)
                {
                    foreach (int i in Enumerable.Range(0, Rand.Range(2, 6)))
                    {
                        FleckMaker.ThrowDustPuff(target.Cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteLow), parent.pawn.Map, 1);
                    }
                    DoEngulf(parent.pawn, tPawn);
                }
            }
            catch (Exception e)
            {
                // Capture the error if any, because otherwise the pawn will fail to spawn back in which is bad.
                Log.Error($"Error in OnJumpCompleted (target {target.Pawn}, user: {parent?.pawn}).\n{e.Message}\n{e.StackTrace}");
            }
        }
    }

    public class CompAbilityEffect_SlimeEngluf : CompAbilityEffect_SlimeEngluf_Abstract
    {
        public override CompProperties_AbilityEngluf_Abstract Props => (CompProperties_AbilityEngluf)props;
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn tPawn = target.Pawn;
            if (tPawn != null)
            {
                foreach (int i in Enumerable.Range(0, Rand.Range(2, 6)))
                {
                    FleckMaker.ThrowDustPuff(target.Cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteLow), parent.pawn.Map, 1);
                }
                DoEngulf(parent.pawn, tPawn);
            }
        }
    }

    public abstract class CompAbilityEffect_SlimeEngluf_Abstract : CompAbilityEffect
    {
        public abstract new CompProperties_AbilityEngluf_Abstract Props { get; }

        public override bool AICanTargetNow(LocalTargetInfo target)
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
            if (parent?.pawn?.health?.hediffSet?.PainTotal > 0.5)
            {
                if (throwMessages)
                {
                    Messages.Message("BS_InTooMuchPain".Translate(enemy.Label), enemy, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            if (EngulfHediff.PowScale(enemy.BodySize) > EngulfHediff.PowScale(parent.pawn.BodySize) * Props.GetSizeThreshold(parent.pawn))
            {
                if (throwMessages)
                {
                    Messages.Message("BS_TooLargeToSwallow".Translate(enemy.Label), enemy, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            // Check if the parent _has_ the digestion capacity. If it does not it is probably a mechanoid or something and can be presumed to use a furnace or something.
            if (parent.pawn.health.capacities.CapableOf(BSDefs.Metabolism))
            {
                var digestCapacity = parent.pawn.health.capacities.GetLevel(BSDefs.Metabolism);
                if (digestCapacity <= 0.55f)
                {
                    Messages.Message("DigestiveAbilityTooLow".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                    return false;
                }
            }
            // Check if the target will fit in the capacity of the existing hediff (if any)
            var hediff = parent.pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("BS_Engulfed"));
            if (hediff != null)
            {
                var engulfHediff = (EngulfHediff)hediff;
                if (engulfHediff.TotalMass + EngulfHediff.PowScale(enemy.BodySize) > engulfHediff.MaxCapacity*1.1f)
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
            engulfHediff.baseCapacity = Props.max != null ? Props.max.Value : Props.GetSizeThreshold(parent.pawn);
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
