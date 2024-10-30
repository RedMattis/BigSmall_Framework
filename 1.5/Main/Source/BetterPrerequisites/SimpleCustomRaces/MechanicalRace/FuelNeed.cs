//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse.AI;
//using Verse;
//using RimWorld.Planet;

//namespace BigAndSmall
//{
//    public class Need_Fuel : Need
//    {
//        [MayRequireBiotech]
//        public Building_MechCharger currentCharger;

//        public const float BaseFallPerDayActive = 10f;

//        public const float BaseFallPerDayIdle = 3f;

//        public int ticksSpentStarved = 0;
//        public const int ticksToStarve = 50000;

//        public override float MaxLevel
//        {
//            get
//            {
//                if (Current.ProgramState != ProgramState.Playing)
//                {
//                    return pawn.BodySize * pawn.ageTracker.CurLifeStage.foodMaxFactor;
//                }
//                return pawn.GetStatValue(StatDefOf.MaxNutrition, applyPostProcess: true, 5);
//            }
//        }

//        private float BaseFallPerDay
//        {
//            get
//            {
//                if (pawn.mindState != null && !pawn.mindState.IsIdle)
//                {
//                    return 10f;
//                }
//                return 3f;
//            }
//        }

//        public float FallPerDay
//        {
//            get
//            {
//                if (pawn.Downed)
//                {
//                    return 0f;
//                }
//                if (!pawn.Awake())
//                {
//                    return 0f;
//                }
//                if (currentCharger != null)
//                {
//                    return 0f;
//                }
//                if (pawn.IsCaravanMember())
//                {
//                    return 0f;
//                }
//                return BaseFallPerDay * pawn.GetStatValue(StatDefOf.MechEnergyUsageFactor);
//            }
//        }

//        public Need_Fuel(Pawn pawn)
//            : base(pawn)
//        {
//        }

//        public override void NeedInterval()
//        {
//            float num = 400f;
//            CurLevel -= FallPerDay / num;
//            if (CurLevel <= 0f)
//            {
//                ticksSpentStarved++;
//            }
//            else
//            {
//                ticksSpentStarved = 0;
//            }
//            Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.SelfShutdown);
//            if (ticksSpentStarved > ticksToStarve)
//            {
//                if (firstHediffOfDef == null)
//                {
//                    pawn.health.AddHediff(BSDefs.BS_CriticalShutdown);
//                }
//            }
//            else if (firstHediffOfDef != null)
//            {
//                pawn.health.RemoveHediff(firstHediffOfDef);
//            }
//        }

//        public override void ExposeData()
//        {
//            base.ExposeData();
//            Scribe_Values.Look(ref ticksSpentStarved, "ticksSpentStarved", 0);
//            Scribe_References.Look(ref currentCharger, "currentCharger");
//        }

//        public override string GetTipString()
//        {
//            StringBuilder stringBuilder = new StringBuilder(base.GetTipString());
//            stringBuilder.AppendInNewLine("BS_FuelFallPerDay".Translate() + ": " + (FallPerDay / 100f).ToStringPercent());
//            return stringBuilder.ToString();
//        }
//    }

//}
