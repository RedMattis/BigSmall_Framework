using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BigAndSmall
{
    public class CompProperties_TargetEffectApplySerum : CompProperties
    {
        public string hediffName = null;
        public string specialEffect = null;
        public bool animalsOnly = false;
        public bool humanoidsOnly = false;
        public bool canTargetNonColonists = true;

        public float factor = 1.0f;
        public float falloff = 2.5f;
        

        public CompProperties_TargetEffectApplySerum()
        {
            compClass = typeof(CompTargetEffect_ApplySerum);
        }
    }

    public class CompTargetEffect_ApplySerum : CompTargetEffect
    {
        public CompProperties_TargetEffectApplySerum Props => (CompProperties_TargetEffectApplySerum)props;

        public static CompProperties_TargetEffectApplySerum currentProps = null;

        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (Props.hediffName == null && Props.specialEffect == null)
            {
                Log.Error("CompTargetEffect_SizeSerum: Props.HediffName is null. You need to specify the Hediff and/or special effect to apply.");
                return;
            }

            // This is DEFINITELY not the best way to do this, and if users queue up several different serums it will get messy.
            // Preferably we should find a way to pass the information to the job, but I don't know how to do that at the moment. :3
            currentProps = Props;

            // Get all jobs
            List<JobDef> jobs = DefDatabase<JobDef>.AllDefsListForReading;

            // Get the first job of type JobDriver_ApplySizeSerum
            JobDef jobDef = jobs.Find(x => x.driverClass == typeof(JobDriver_ApplySerum));
            if (jobDef == null)
            {
                Log.Error("Could not find JobDriver_ApplySizeSerum");
                return;
            }

            if (jobDef != null && user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                Job job = JobMaker.MakeJob(jobDef, target, parent);
                job.count = 1;
                user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }
    }

    public class JobDriver_ApplySerum : JobDriver
    {
        private Pawn Pawn => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        private Thing Item => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(Pawn, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(Item, job, 1, -1, null, errorOnFailed);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(600);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickAction = delegate
            {
                
            };
            yield return toil;
            yield return Toils_General.Do(ApplySerumSerum);
        }

        private void ApplySerumSerum()
        {
            var props = CompTargetEffect_ApplySerum.currentProps;
            bool isValidTargetType = props.animalsOnly && Pawn?.RaceProps?.Humanlike == false || props.humanoidsOnly && Pawn?.RaceProps?.Humanlike == true ||
                !props.animalsOnly && !props.humanoidsOnly;

            bool isValidTargetFaction = props.canTargetNonColonists || Pawn.Faction == Faction.OfPlayer;

            if (isValidTargetType && isValidTargetFaction)
            {
                if (props.hediffName != null)
                {
                    var animalHediff = HediffMaker.MakeHediff(HediffDef.Named(props.hediffName), Pawn);
                    Pawn.health.AddHediff(animalHediff);
                }
                else if (props.specialEffect != null)
                {
                    if (props.specialEffect == "Dicombobulator" && Pawn?.RaceProps?.Humanlike == true)
                    {
                        Discombobulator.Discombobulate(Pawn);
                    }
                    else if (props.specialEffect == "GeneIntegrator" && Pawn?.RaceProps?.Humanlike == true)
                    {
                        Discombobulator.IntegrateGenes(Pawn);
                    }
                    else if (props.specialEffect == "XenoCopy" && Pawn?.RaceProps?.Humanlike == true)
                    {
                        Discombobulator.XenoCopy(Pawn);
                    }
                    else if (props.specialEffect == "CreateXenogerm" && Pawn?.RaceProps?.Humanlike == true)
                    {
                        Discombobulator.CreateXenogerm(Pawn);
                    }
                    else if (props.specialEffect == "CreateArchiteXenogerm" && Pawn?.RaceProps?.Humanlike == true)
                    {
                        Discombobulator.CreateXenogerm(Pawn, archite:true);
                    }
                    else if (props.specialEffect == "AddSoulPower")
                    {
                        var scHediff = CompAbilityEffect_ConsumeSoul.MakeGetSoulCollectorHediff(Pawn);
                        scHediff.AddSoulPowerDirect(props.factor, props.falloff);

                        // Spread blood filith around the area.
                        if (Pawn?.Map != null)
                        {
                            var bloodFilth = ThingDefOf.Filth_Blood;
                            for (int i = 0; i < 2; i++)
                            {
                                IntVec3 randomCell = Pawn.Position + GenRadial.RadialPattern[i];
                                if (randomCell.InBounds(Pawn.Map))
                                {
                                    FilthMaker.TryMakeFilth(randomCell, Pawn.Map, bloodFilth, 1);
                                }
                            }
                        }
                    }
                }
                
                Item.SplitOff(1).Destroy();
            }
        }
    }

    //CompProperties_TargetEffectResurrect
}
