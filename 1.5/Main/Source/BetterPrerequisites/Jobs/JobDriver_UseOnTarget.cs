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
	internal class JobDriver_UseOnTarget : JobDriver_UseItem
	{
		private int useDuration = -1;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref useDuration, "useDuration", 0);
		}

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			useDuration = job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompUsable>().Props.useDuration;
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			CompUseConditionQuantity quantityCondition = base.TargetThingA.TryGetComp<CompUseConditionQuantity>();
			if (quantityCondition != null)
				job.count = quantityCondition.Props.quantity;

			if (!pawn.Reserve(job.targetA, job, 1, job.count, null, errorOnFailed))
			{
				return false;
			}

			// If job is targeting a valid target.
			if (job.targetB.IsValid && !pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
			this.FailOn(() => !base.TargetThingA.TryGetComp<CompUsable>().CanBeUsedBy(pawn));
			yield return Toils_Goto.GotoThing(TargetIndex.A, base.TargetThingA.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);


			if (job.targetB.IsValid)
			{
				yield return Toils_Haul.StartCarryThing(TargetIndex.A);
				yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B);

				Toil setTarget = ToilMaker.MakeToil("SetTarget");
				setTarget.initAction = () => pawn.carryTracker.CarriedThing.TryGetComp<CompTargetable>().selectedTarget = TargetThingB;
				yield return setTarget;
			}

			yield return WaitDuration();
			yield return Use();
		}



        private Toil WaitDuration()
		{
			Thing thing = job.GetTarget(TargetIndex.A).Thing;

			TargetIndex targetIndex = job.targetB.IsValid ? TargetIndex.B : TargetIndex.A;
			LocalTargetInfo target = job.GetTarget(targetIndex);

			Toil toil = Toils_General.WaitWith(targetIndex, useDuration);
			toil.WithProgressBarToilDelay(targetIndex);
			toil.FailOnDespawnedNullOrForbidden(targetIndex);
			toil.FailOnCannotTouch(targetIndex, target.Thing.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
			toil.handlingFacing = true;

			List<CompUseEffect> useComps = (thing as ThingWithComps)?.GetComps<CompUseEffect>()?.ToList();
			CompUsable compUsable = thing.TryGetComp<CompUsable>();
			if (job.targetB.IsValid)
			{
				toil.FailOnDespawnedOrNull(TargetIndex.B);
				if (thing.TryGetComp<CompTargetable>()?.Props?.nonDownedPawnOnly ?? false)
				{
					toil.FailOnDestroyedOrNull(TargetIndex.B);
					toil.FailOnDowned(TargetIndex.B);
				}
			}

			Mote warmupMote = null;
			if (compUsable != null && compUsable.Props?.warmupMote != null)
				warmupMote = MoteMaker.MakeAttachedOverlay(target.Thing, compUsable.Props.warmupMote, Vector3.zero);


			toil.tickAction = delegate
			{
				if (useComps != null)
					for (int i = useComps.Count - 1; i >= 0; i--)
						useComps[i].PrepareTick();

				warmupMote?.Maintain();
				pawn.rotationTracker.FaceTarget(target);
			};

			return toil;
		}
	}
}
