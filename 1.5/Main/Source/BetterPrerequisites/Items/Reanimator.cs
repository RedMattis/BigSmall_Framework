using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BigAndSmall
{
    public class CompProperties_TargetEffectReanimate : CompProperties
    {
        public ThingDef moteDef;
        public XenotypeDef xenoTypeDef;
        public CompProperties_TargetEffectReanimate()
        {
            compClass = typeof(CompTargetEffect_Reanimate);
        }
    }

    public class CompTargetEffect_Reanimate : CompTargetEffect
    {
        public CompProperties_TargetEffectReanimate Props => (CompProperties_TargetEffectReanimate)props;
        public static CompProperties_TargetEffectReanimate currentProps = null; // Extremely ugly hack to pass the properties to the JobDriver

        public override void DoEffectOn(Pawn user, Thing target)
        {
            // Get all jobs
            List<JobDef> jobs = DefDatabase<JobDef>.AllDefsListForReading;

            // Get the first job of type JobDriver_Resurrect
            JobDef jobDef = jobs.Find(x => x.driverClass == typeof(JobDriver_Reanimate));
            if (jobDef == null)
            {
                Log.Error("Could not find JobDriver_Resurrect");
                return;
            }

            if (jobDef != null && user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                currentProps = Props;
                Job job = JobMaker.MakeJob(jobDef, target, parent);
                job.count = 1;
                user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }
    }

    public class JobDriver_Reanimate : JobDriver
    {
        private const TargetIndex CorpseInd = TargetIndex.A;

        private const TargetIndex ItemInd = TargetIndex.B;

        private const int DurationTicks = 600;

        private Mote warmupMote;

        private Corpse Corpse => (Corpse)job.GetTarget(TargetIndex.A).Thing;

        private Thing Item => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(Corpse, job, 1, -1, null, errorOnFailed))
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
                CompUsable compUsable = Item.TryGetComp<CompUsable>();
                if (compUsable != null && warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    warmupMote = MoteMaker.MakeAttachedOverlay(Corpse, compUsable.Props.warmupMote, Vector3.zero);
                }
                warmupMote?.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(ReanimateToil);
        }

        private void ReanimateToil()
        {
            var props = CompTargetEffect_Reanimate.currentProps;

            Pawn innerPawn = Corpse.InnerPawn;
            ReanimatePawn(innerPawn, props.xenoTypeDef);
            //ResurrectionUtility.ResurrectWithSideEffects(innerPawn);
            SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(innerPawn));
            Messages.Message("MessagePawnResurrected".Translate(innerPawn), innerPawn, MessageTypeDefOf.PositiveEvent);
            ThingDef thingDef = Item?.TryGetComp<CompTargetEffect_Resurrect>()?.Props.moteDef;
            if (thingDef != null)
            {
                MoteMaker.MakeAttachedOverlay(innerPawn, thingDef, Vector3.zero);
            }
            Item.SplitOff(1).Destroy();
        }

        public static void ReanimatePawn(Pawn innerPawn, XenotypeDef xenotype)
        {
            if (innerPawn?.RaceProps?.Animal == true)
            {
                var animalHediff = HediffMaker.MakeHediff(HediffDef.Named("VU_AnimalReturned"), innerPawn);
                innerPawn.health.AddHediff(animalHediff);
            }
            else if (innerPawn.genes != null)
            {
                GeneHelpers.AddAllXenotypeGenes(innerPawn, xenotype, name: "Returned " + innerPawn.genes.XenotypeLabel);
            }
            GameUtils.UnhealingRessurection(innerPawn);
        }
    }

    //CompProperties_TargetEffectResurrect
}
