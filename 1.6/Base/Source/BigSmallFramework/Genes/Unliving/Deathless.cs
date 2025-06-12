using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class LesserDeathless : TickdownGene
    {
        public override void PostRemove()
        {
            base.PostRemove();
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.BS_LesserDeathless_Death);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public override void TickEvent()
        {
            bool forcedDeathrest = SanguophageUtility.ShouldBeDeathrestingOrInComaInsteadOfDead(pawn);
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.BS_LesserDeathless_Death);

            if (forcedDeathrest)
            {
                if (hediff == null)
                {
                    Hediff hediff2 = HediffMaker.MakeHediff(BSDefs.BS_LesserDeathless_Death, pawn);
                    pawn.health.AddHediff(hediff2);
                }
            }
            else
            {
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        public override void ResetCountdown() => tickDown = 0;
    }


    public class GreaterDeathless : TickdownGene
    {
        public override void TickEvent()
        {

            HediffDef deathRefusal = DefDatabase<HediffDef>.GetNamed("DeathRefusal");
            var deathRefusalHediff = pawn.health.hediffSet.GetFirstHediffOfDef(deathRefusal);
            if (deathRefusalHediff == null)
            {
                pawn.health.AddHediff(deathRefusal);

            }
        }

        public override void ResetCountdown()
        {
            tickDown = Rand.Range(10000, 120000);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);

            HediffDef deathRefusal = DefDatabase<HediffDef>.GetNamed("DeathRefusal");
            var deathRefusalHediff = pawn.health.hediffSet.GetFirstHediffOfDef(deathRefusal);

            // Check if prisoner or slave.
            if (deathRefusalHediff != null && pawn.IsPrisoner || pawn.IsSlave)
            {
                // Remove slave status. Time to rampage.
                pawn.guest.SetGuestStatus(null);
            }
        }

        public override void PostAdd()
        {
            base.PostAdd();

            bool isColonistOrPrisoner = pawn.Faction == Faction.OfPlayerSilentFail || pawn.HostFaction == Faction.OfPlayerSilentFail;

            // Check if colonist or prisoner
            if (!isColonistOrPrisoner && ModLister.CheckAnomaly("Death refusal") && !pawn.Dead)
            {
                HediffDef deathRefusal = DefDatabase<HediffDef>.GetNamed("DeathRefusal");
                var deathRefusalHediff = pawn.health.hediffSet.GetFirstHediffOfDef(deathRefusal);
                if (deathRefusalHediff == null)
                {
                    pawn.health.AddHediff(deathRefusal);
                }
            }
        }
    }



}
