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

    [HarmonyPatch(typeof(FloatMenuMakerMap),
    nameof(FloatMenuMakerMap.GetOptions),
    [
        typeof(List<Pawn>),
        typeof(Vector3),
        typeof(FloatMenuContext),
    ],
    [
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Out
    ]
    )]
    public static class FloatMenuMakerMap_AddHumanlikeOrders_Patch
    {
        public static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref FloatMenuContext context, ref List<FloatMenuOption> __result)
        {
            // Quickfix for 1.6. Needs a proper rewrite to use the real system.
            if (selectedPawns.Count != 1) return;
            Pawn pawn = selectedPawns[0];

            List<Thing> thingList = IntVec3.FromVector3(clickPos).GetThingList(pawn.Map);
            var pawnList = thingList.Where(x => x is Pawn).Select(x => (Pawn)x);
            foreach (var pilotable in pawnList)
            {
                // Check if pawn has a piloted hediff
                var pilotedHediff = pilotable?.health?.hediffSet?.hediffs?.OfType<Piloted>()?.FirstOrDefault();
                if (!pilotedHediff?.defaultEnterable == true)
                {
                    continue;
                }
                if (pilotedHediff != null && pilotedHediff is Piloted piloted)
                {
                    string errorMsg = "";
                    if (piloted.pawn.Faction != pawn.Faction && !piloted.Props.pilotRequired)
                    {
                        errorMsg = $"{pawn.Label} {"BS_CannotEnterEnemyAsOperator".Translate()}";
                    }
                    else if (piloted.pawn.Faction != pawn.Faction && !piloted.pawn.Downed && piloted.InnerContainer.Count > 0)
                    {
                        errorMsg = $"{pawn.Label} {"BS_CannotPilotNonDownedEnemy".Translate()}";
                    }
                    else if (piloted.PilotCount + 1 > piloted.PilotCapacity)
                    {
                        errorMsg = $"{pawn.Label} {"BS_PilotCapReached".Translate()}";
                    }
                    else if (piloted.MaxCapacity < pawn.BodySize)
                    {
                        errorMsg = $"{pawn.Label} {"BS_TooLargeToPilot".Translate()}";
                    }
                    else if (piloted.TotalMass + pawn.BodySize > piloted.MaxCapacity)
                    {
                        errorMsg = $"{pawn.Label} {"BS_NotEnoughRoomForPilot".Translate()}";
                    }

                    var pilotJobDef = DefDatabase<JobDef>.AllDefsListForReading.Where(x => x.defName == "BS_EnteringPilotablePawn").FirstOrDefault();
                    if (pilotJobDef != null)
                    {
                        var action = new FloatMenuOption("BS_EnterPilotable".Translate(), delegate
                        {
                            Job job = JobMaker.MakeJob(pilotJobDef, pilotable);
                            //job.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
                        });
                        if (errorMsg != "")
                        {
                            action.Disabled = true;
                            action.Label = errorMsg;
                        }

                        __result.Add(action);
                    }

                    //string errorMsg2 = "";
                    if (piloted.pawn.Downed && piloted.PilotCount > 0)
                    {
                        var ejectJobDef = DefDatabase<JobDef>.AllDefsListForReading.Where(x => x.defName == "BS_EjectPilotablePawn").FirstOrDefault();
                        if (ejectJobDef != null)
                        {
                            var action = new FloatMenuOption("BS_EjectPilots".Translate(), delegate
                            {
                                Job job = JobMaker.MakeJob(ejectJobDef, pilotable);
                                //job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
                            });

                            __result.Add(action);
                        }
                    }
                }
            }
        }
    }


}
