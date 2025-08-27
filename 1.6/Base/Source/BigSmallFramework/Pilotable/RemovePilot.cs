using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    public class CompProperties_RemovePilot : CompProperties_AbilityEffect
    {
        public CompProperties_RemovePilot()
        {
            compClass = typeof(RemovePilotComp);
        }
    }

    public class RemovePilotComp : CompAbilityEffect
    {

        // When the ability is activated remove the piloted Hediff.
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            RemovePilotedHediff(parent.pawn);
        }

        // Remove the piloted Hediff.
        public void RemovePilotedHediff(Pawn pawn)
        {
            // Get first hediff matching name BS_Piloted
            var pilotedHediffs = pawn.health.hediffSet.hediffs.Where(x => x is Piloted);
            foreach (var pilotedHediff in pilotedHediffs.ToArray())
            {
                // Removed the pilot from the hediff.
                if (pilotedHediff is Piloted piloted)
                {
                    piloted.RemovePilots();
                    return;
                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return true;
        }
    }
}
