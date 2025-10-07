using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Verse.AI.Group;
using Verse;

namespace BigAndSmall
{
    [StaticConstructorOnStartup]
    public class UsableDownedAbility : Ability
    {
        public UsableDownedAbility() : base() { }

        public UsableDownedAbility(Pawn pawn) : base(pawn) { }

        public UsableDownedAbility(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool CanQueueCast => pawn.Downed || base.CanQueueCast;

        public override void QueueCastingJob(LocalTargetInfo target, LocalTargetInfo destination)
        {
            if (!CanQueueCast || !CanApplyOn(target))
            {
                return;
            }
            if (pawn.Downed  && verb.verbProps.targetParams?.canTargetSelf == true)
            {
                verb.TryStartCastOn(verb.Caster);
                return;
            }
            base.QueueCastingJob(target, destination);
        }


        public override bool GizmoDisabled(out string reason)
        {
            if (CanCooldown && OnCooldown && (!def.cooldownPerCharge || charges == 0))
            {
                reason = "AbilityOnCooldown".Translate(CooldownTicksRemaining.ToStringTicksToPeriod()).Resolve();
                return true;
            }
            if (UsesCharges && charges <= 0)
            {
                reason = "AbilityNoCharges".Translate();
                return true;
            }
            if (!comps.NullOrEmpty())
            {
                for (int i = 0; i < comps.Count; i++)
                {
                    if (comps[i].GizmoDisabled(out reason))
                    {
                        return true;
                    }
                }
            }
            AcceptanceReport canCast = CanCast;
            if (!canCast.Accepted)
            {
                reason = canCast.Reason;
                return true;
            }
            Lord lord = pawn.GetLord();
            if (lord != null)
            {
                AcceptanceReport acceptanceReport = lord.AbilityAllowed(this);
                if (!acceptanceReport)
                {
                    reason = acceptanceReport.Reason;
                    return true;
                }
            }
            if (!pawn.Drafted && def.disableGizmoWhileUndrafted && pawn.GetCaravan() == null && !DebugSettings.ShowDevGizmos)
            {
                reason = "AbilityDisabledUndrafted".Translate();
                return true;
            }
            if (def.casterMustBeCapableOfViolence && pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                reason = "IsIncapableOfViolence".Translate(pawn.LabelShort, pawn);
                return true;
            }
            if (!CanQueueCast)
            {
                reason = "AbilityAlreadyQueued".Translate();
                return true;
            }
            reason = null;
            return false;
        }

    }
}
