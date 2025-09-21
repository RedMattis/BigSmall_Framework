using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace BigAndSmall
{
    public class JobDriver_UseCharger : JobDriver
    {
        public Building_AndroidCharger Charger => (Building_AndroidCharger)job.targetA.Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Charger, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Charger.PawnCanUse(pawn, false));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A).FailOnForbidden(TargetIndex.A);

            Toil charge = ToilMaker.MakeToil("MakeNewToils");
            charge.defaultCompleteMode = ToilCompleteMode.Never;
            charge.initAction = delegate
            {
                Charger.StartCharging(pawn);
            };
            charge.handlingFacing = true;
            charge.tickIntervalAction = (Action<int>)Delegate.Combine(charge.tickIntervalAction, (Action<int>)delegate
            {
                pawn.rotationTracker.FaceTarget(Charger.Position);
                if (pawn.needs.food.CurLevelPercentage >= 1.0)
                {
                    Charger.StopCharging();
                    ReadyForNextToil();
                }
            });
            yield return charge;
        }
    }
}
