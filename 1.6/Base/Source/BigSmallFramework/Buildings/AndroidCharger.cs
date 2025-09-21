using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace BigAndSmall
{
    public interface IRobotCharger
    {
        public bool PawnCanUse(Pawn pawn, bool isNew);
    }

    public class Building_AndroidCharger : Building, IRobotCharger
    {
        // At 100% WorkTableEfficiencyFactor we will drain 800W to provide 1 food. 200% efficiency is 400W per food, etc.

        // Wd = WattDays
        private const int StandardBatteryWd = 600;
        private const int BaseWdPer1Food = 400;
        private const int TicksPerHour = 2500;
        private const float MinimumBatteryChargeToUse = StandardBatteryWd * 0.49f;

        public const float BaseTransferSpeed = 4.0f;

        public const float BasePowerPerDay = BaseWdPer1Food * 24;

        private Pawn user;
        private float userChargingEfficiency = 1f;
        private float extraSpeedFactor = 1f;
        private float pawnFoodFallRate = 0f;
        private int lastTick = -1;

        public CompPowerTrader Power => this.TryGetComp<CompPowerTrader>();
        public bool IsPowered => Power.PowerOn;

        public float GetPowerPerDay(float factor) => BasePowerPerDay * factor
            / this.GetStatValue(StatDefOf.WorkTableEfficiencyFactor, applyPostProcess: false, cacheStaleAfterTicks: 1000)
            / userChargingEfficiency;

        public float GetWorkSpeedFactor() => BaseTransferSpeed * userChargingEfficiency * extraSpeedFactor
            * this.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor, applyPostProcess:false, cacheStaleAfterTicks: 1000);
        public bool PawnCanUse(Pawn pawn, bool isNew)
        {
            if (Power?.PowerNet == null || !IsPowered)
            {
                return false;
            }
            if (!(user == pawn || CanUseChargers(pawn)))
            {
                return false;
            }
            if (user == null || user == pawn)
            {
                return isNew ? PowerGridSufficientPowerToStart : PowerGridSufficientPowerToContinue;
            }
            return false;
        }

        protected bool PowerGridSufficientPowerToStart => Power.PowerNet.CurrentStoredEnergy() >= MinimumBatteryChargeToUse;
        protected bool PowerGridSufficientPowerToContinue => Power.PowerNet.CurrentStoredEnergy() >= 10;

        public virtual bool CanUseChargers(Pawn pawn) => pawn.GetCachePrepatched().canUseChargers;

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
                user = null;
                Power.PowerOutput = 0;
            }
            lastTick = thisTick;
        }

        public void StartCharging(Pawn pawn)
        {
            user = pawn;
            userChargingEfficiency = pawn.GetStatValue(BSDefs.BS_BatteryCharging, applyPostProcess: true, cacheStaleAfterTicks: 500);
            extraSpeedFactor = 1f;
            if (pawn.BodySize is float size && size > 1f)
            {
                extraSpeedFactor = size;
            }
            try
            {
                pawnFoodFallRate = pawn.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Hungry, ignoreMalnutrition: true);
            }
            catch
            {
                Log.ErrorOnce($"Error getting food fall rate for {pawn}. Setting to 0.", 81410);
                pawnFoodFallRate = 0f;
            }
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

            float transferRate = GetWorkSpeedFactor();
            Power.PowerOutput = -GetPowerPerDay(transferRate);  // Note: not tick-rate dependent.

            if (pawn.needs.food is Need_Food food && food?.CurLevelPercentage < 1f)
            {
                float hoursPassed = (ticksPassed / (float)TicksPerHour);

                food.CurLevel += transferRate * hoursPassed;
                food.CurLevel += pawnFoodFallRate * ticksPassed; // Reverse the natural decay.
                if (food.CurLevelPercentage >= 1f)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption2 in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption2;
            }
            if (selPawn.Faction == Faction.OfPlayer && CanUseChargers(selPawn) && selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                if (user == null)
                {
                    yield return new FloatMenuOption("BS_UseAndroidCharger".Translate().CapitalizeFirst(), delegate
                    {
                        var job = JobMaker.MakeJob(BSDefs.BS_UseCharger, this);
                        selPawn.Reserve(this,job, ignoreOtherReservations:true);
                        selPawn.jobs.TryTakeOrderedJob(job);
                    });
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref user, "userOfBuilding");
            Scribe_Values.Look(ref userChargingEfficiency, "currentUserChargingEfficiency", 1f);
            Scribe_Values.Look(ref extraSpeedFactor, "extraSpeedFactor", 1f);
            Scribe_Values.Look(ref pawnFoodFallRate, "pawnFoodFallRate", 0f);
        }
    }
}
