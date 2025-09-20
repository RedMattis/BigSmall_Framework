using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Noise;
using static UnityEngine.GridBrushBase;

namespace BigAndSmall
{
    public interface IRobotCharger
    {
        public bool PawnCanUse(Pawn pawn);
    }

    public class Building_AndroidCharger : Building, IRobotCharger
    {
        private const int SmallBatteryCapacity = 800;
        private const int ticksPerHour = 2500;
        private const float PowerToFoodRatioDefault = SmallBatteryCapacity * 0.5f;

        private Pawn user;
        private int lastTick = -1;

        public CompPowerTrader Power => this.TryGetComp<CompPowerTrader>();
        public bool IsPowered => Power.PowerOn;
        
        public virtual float FoodPerHour => 4.0f;
        public float FoodPerDay => FoodPerHour * 24;
        public float PowerPerDay => FoodPerDay * PowerToFoodRatio;
        public virtual float PowerToFoodRatio => PowerToFoodRatioDefault; // A bit less than 1 battery per full charge.
        public bool PawnCanUse(Pawn pawn)
        {
            if (Power?.PowerNet == null || !IsPowered)
            {
                return false;
            }
            if (user == pawn) // Early out.
            {
                return true;
            }
            if (!CanUseCharger(pawn))
            {
                return false;
            }
            if (user == null)
            {
                return true;
            }
            return false;
        }

        public virtual bool CanUseCharger(Pawn pawn) => pawn.GetCachePrepatched().canUseChargers;

        protected override void TickInterval(int delta)
        {
            if (lastTick < 0)
            {
                lastTick = Find.TickManager.TicksGame;
            }
            if (user is not Pawn pawn)
            {
                return;
            }

            int thisTick = Find.TickManager.TicksGame;
            if (pawn.CurJobDef == BSDefs.BS_UseCharger)
            {
                int timePassed = thisTick - lastTick;
                DoCharge(pawn, timePassed);
            }
            else
            {
                Log.Message("Stopping use since user is null or not using charger job.");
                Power.PowerOutput = 0;
            }
            lastTick = thisTick;
        }

        public void StartCharging(Pawn pawn)
        {
            user = pawn;
            lastTick = Find.TickManager.TicksGame;
        }

        public void StopCharging()
        {
            user = null;
            Power.PowerOutput = 0;
        }

        protected virtual void DoCharge(Pawn pawn, int ticksPassed)
        {
            if (!IsPowered)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            }
            float factor = ticksPassed / (float)ticksPerHour;
            Power.PowerOutput = -PowerPerDay;
            Need_Food food = pawn.needs.food;
            if (food?.CurLevelPercentage < 1f)
            {
                food.CurLevel += FoodPerHour * factor;
                food.CurLevel += pawn.needs.food.FoodFallPerTick * ticksPassed; // Reverse the natural decay.
                if (food.CurLevelPercentage >= 1f)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref user, "userOfBuilding");
        }
    }

    public class JobDriver_UseCharger : JobDriver
    {
        public Building_AndroidCharger Charger => (Building_AndroidCharger)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Charger, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Charger.CanUseCharger(pawn));
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.InteractionCell).FailOnForbidden(TargetIndex.A);

            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = delegate
            {
                Charger.StartCharging(pawn);
            };
            toil.handlingFacing = true;
            toil.tickIntervalAction = (Action<int>)Delegate.Combine(toil.tickIntervalAction, (Action<int>)delegate
            {
                pawn.rotationTracker.FaceTarget(Charger.Position);
                if (pawn.needs.food.CurLevelPercentage >= 1.0)
                {
                    Charger.StopCharging();
                    ReadyForNextToil();
                }
            });
            yield return toil;
        }
    }

    public class JobGiver_UseCharger : ThinkNode_JobGiver
    {
        private const float maxLevelPercentage = 1f;
        public override float GetPriority(Pawn pawn)
        {
            Need_Food food = pawn.needs.food;
            if (food == null)
            {
                return 0;
            }
            if (food.CurLevelPercentage > 0.45f)
            {
                return 0; 
            }
            if (pawn.GetCachePrepatched()?.canUseChargers == true)
            {
                return 9.51f;
            }
            return 0;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            bool predicate(Thing t) => t is IRobotCharger charger && charger.PawnCanUse(pawn);

            Need_Food food = pawn.needs.food;
            if (food == null || food.CurLevelPercentage > maxLevelPercentage)
            {
                return null;
            }
            float searchRange = food.CurCategory switch
            {
                HungerCategory.Hungry => 24f,
                HungerCategory.UrgentlyHungry => 48f,
                HungerCategory.Starving => 99999,
                _ => 0f,
            };

            Thing recharger = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(pawn),
                maxDistance: searchRange, validator: predicate);

            if (recharger != null)
            {
                return JobMaker.MakeJob(BSDefs.BS_UseCharger, recharger);
            }
            return null;
        }
    }
}
