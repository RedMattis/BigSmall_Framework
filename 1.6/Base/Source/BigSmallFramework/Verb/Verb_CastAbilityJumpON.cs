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
    public class Verb_CastAbilityJumpON : Verb_CastAbilityJump
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            DoJump(CasterPawn, target, this, EffectiveRange);
        }

        public static void DoJump(Pawn pawn, LocalTargetInfo target, Verb verb, float range)
        {
            Map map = pawn.Map;
            IntVec3 intVec = target.Cell;
            Job job = JobMaker.MakeJob(JobDefOf.CastJump, target);
            job.verbToUse = verb;
            if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                FleckMaker.Static(intVec, map, FleckDefOf.FeedbackGoto);
            }
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            for (int i = 0; i < ability.EffectComps.Count; i++)
            {
                if (!ability.EffectComps[i].Valid(target, showMessages))
                {
                    return false;
                }
            }

            return base.ValidateTarget(target, showMessages);
        }
    }
}
