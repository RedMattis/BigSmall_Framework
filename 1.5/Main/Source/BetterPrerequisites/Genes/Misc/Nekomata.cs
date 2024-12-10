using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class RiseReturned_AbilityEffect : CompProperties_AbilityEffect
    {

        public RiseReturned_AbilityEffect()
        {
            compClass = typeof(RiseReturned);
        }

    }

    public class RiseReturned : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                RiseDead(pawn);
            }
            if (pawn == null && target.Thing is Corpse corpse)
            {
                RiseDead(corpse.InnerPawn);
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var result = base.Valid(target, throwMessages);

            if (target.Pawn.IsUndead())
            {
                Messages.Message("BS_TargetAlreadyUndead".Translate(target.Label), target.Pawn, MessageTypeDefOf.RejectInput, historical: false);
                result = false;
            }
            return result;
        }

        public void RiseDead(Pawn pawn)
        {
            //JobDriver_Reanimate.ReanimatePawn(pawn);

            // 50% chance of zombie apocalypse
            if (Rand.Chance(0.5f))
            {
                OnKillPatch.TriggerZombieApocalypse(pawn?.Map, sendMessage:false);
            }
            try
            {
                if (Rand.Chance(0.75f))
                {
                    Hediff returnedHediff = HediffMaker.MakeHediff(HediffDef.Named("BS_ReturnedReanimation"), pawn);
                    pawn.health.AddHediff(returnedHediff);
                }
                else
                {
                    GameUtils.UnhealingRessurection(pawn);
                }
            }
            catch (Exception ex)
            {
                //Log.Warning("BS_ReturnedReanimation failed to apply hediff to " + pawn.LabelShort + ": " + ex.Message);
            }

            bool incidentTriggered = false;
            // 50% chance of random major incident
            if (Rand.Chance(0.5f))
            {
                // Spawn a random major incident
                IncidentDef incidentDef = DefDatabase<IncidentDef>.AllDefs.Where(x => x.category == IncidentCategoryDefOf.ThreatBig).RandomElement();
                IncidentParms incidentParms = new IncidentParms
                {
                    target = parent.pawn.Map,
                    forced = true,
                    points = StorytellerUtility.DefaultThreatPointsNow(parent.pawn.Map) * Mathf.Lerp(0.5f, 1.2f, Rand.Value)
                };
                incidentDef.Worker.TryExecute(incidentParms);
                incidentTriggered = true;
            }

            // 50% chance of raid
            if (Rand.Chance(0.5f))
            {
                float maxPoints = incidentTriggered ? 1f : 1.2f;
                // Spawn a raid incident
                IncidentParms incidentParms = new IncidentParms
                {
                    target = parent.pawn.Map,
                    forced = true,
                    points = StorytellerUtility.DefaultThreatPointsNow(parent.pawn.Map) * Mathf.Lerp(0.5f, maxPoints, Rand.Value)
                };
                IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);
            }
            
        }
    }
}
