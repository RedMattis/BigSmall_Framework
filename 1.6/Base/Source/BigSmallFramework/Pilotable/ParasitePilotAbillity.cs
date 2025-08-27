using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_ParasitePilotAbillity : CompProperties_AbilityEffect
    {
        public HediffDef pilotHediff;
        public CompProperties_ParasitePilotAbillity()
        {
            compClass = typeof(ParasitePilotAbillity);
        }
    }

    public class ParasitePilotAbillity : CompAbilityEffect
    {
        new CompProperties_ParasitePilotAbillity Props => base.Props as CompProperties_ParasitePilotAbillity;

        // When the ability is activated remove the piloted Hediff.
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.Pawn != null)
            {
                var attacker = parent.pawn;
                var victim = target.Pawn;
                // Check if hostile to self.
                if (victim.Faction != parent.pawn.Faction && !victim.Faction.HostileTo(parent.pawn.Faction))
                {
                    // Rep loss
                    if (victim.Faction != null && attacker.Faction != null)
                    {
                        victim.Faction.TryAffectGoodwillWith(attacker.Faction, -35);
                    }
                }
                // Add the piloted hediff if not already present.
                if (victim.health.hediffSet.GetFirstHediffOfDef(Props.pilotHediff) == null)
                {
                    var hediff = HediffMaker.MakeHediff(Props.pilotHediff, victim);
                    victim.health.AddHediff(hediff);
                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null) return false;
            if (pawn.Downed
                || pawn.stances?.stunner?.Stunned == true
                ||
                (
                    pawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness)
                    && pawn.health?.capacities?.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.8f
                )
                || pawn.health?.hediffSet?.PainTotal > 0.35f
                )
            {
                return true;
            }
            return false;
        }
    }
}
