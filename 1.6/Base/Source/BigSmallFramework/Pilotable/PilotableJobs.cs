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
    public class JobDriver_EnterPilotable : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(150).WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    // Get the Target as a Pawn
                    Pawn pilotable = TargetA.Thing as Pawn;
                    if (pilotable != null)
                    {
                        var pilotedHediff = pilotable?.health?.hediffSet?.hediffs?.Where(x => x is Piloted).FirstOrDefault();
                        if (pilotedHediff != null && pilotedHediff is Piloted piloted)
                        {
                            piloted.AddPilot(pawn);
                        }
                    }
                }
            };
        }
    }

    public class JobDriver_EjectPilotable : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(150).WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    // Get the Target as a Pawn
                    Pawn pilotable = TargetA.Thing as Pawn;
                    if (pilotable != null)
                    {
                        var pilotedHediff = pilotable?.health?.hediffSet?.hediffs?.Where(x => x is Piloted).FirstOrDefault();
                        if (pilotedHediff != null && pilotedHediff is Piloted piloted)
                        {
                            piloted.RemovePilots();
                        }
                    }
                }
            };
        }
    }
}
