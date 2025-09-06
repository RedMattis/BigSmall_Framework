using BigAndSmall.Abillities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_ParasiteAbility : CompProperties_AbilityJumpAndUseOn
    {
        public HediffDef pilotHediff;
        public CompProperties_ParasiteAbility()
        {
            compClass = typeof(ParasiteAbility);
        }
    }

    public class ParasiteAbility : CompAbilityEffect_JumpAndUseOn
    {
        new CompProperties_ParasiteAbility Props => base.Props as CompProperties_ParasiteAbility;

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return Valid(target);
        }

        // When the ability is activated remove the piloted Hediff.
        public override void ApplyEffect(IntVec3 origin, LocalTargetInfo target)
        {
            if (target.Pawn != null)
            {
                var attacker = parent.pawn;
                var victim = target.Pawn;

                // Remove from PawnFlyer.
                if (attacker.ParentHolder is PawnFlyer flyer)
                {
                    attacker.holdingOwner = null;
                    flyer.Destroy(DestroyMode.Vanish);
                }
                
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
                if (victim.health.hediffSet.GetFirstHediffOfDef(Props.pilotHediff) is Piloted piloted)
                {
                    piloted.AddPilot(attacker);
                }
                else
                {
                    var hediff = HediffMaker.MakeHediff(Props.pilotHediff, victim) as Piloted;
                    victim.health.AddHediff(hediff);
                    hediff.AddPilot(attacker);
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var pawn = target.Pawn;
            if (pawn == null) return false;
            if (pawn.BodySize * 0.8 < parent.pawn.BodySize)
            {
                if (throwMessages)
                {
                    Messages.Message("BS_ParasiteTargetTooSmall".Translate(pawn.Label, parent.pawn.Label), pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            if (pawn.RaceProps.Humanlike == false || pawn.IsMutant)
            {
                if (throwMessages)
                {
                    Messages.Message("BS_ParasiteTargetNotHumanlike".Translate(pawn.Label, parent.pawn.Label), pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            if (pawn.Downed
                || pawn.stances?.stunner?.Stunned == true
                ||
                (
                    pawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness)
                    && pawn.health?.capacities?.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.75f
                )
                || pawn.health?.hediffSet?.PainTotal > 0.35f
                || pawn.Awake() == false
                )
            {
                return true;
            }
            else
            {
                if (throwMessages)
                {
                    Messages.Message("BS_ParasiteTargetNotImpaired".Translate(pawn.Label, parent.pawn.Label), pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
                return false;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target);
        }
    }
}
