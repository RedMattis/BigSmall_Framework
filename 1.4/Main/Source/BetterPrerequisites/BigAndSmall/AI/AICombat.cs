using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    public class CombatBuff : DefModExtension
    {
    }

    // postfix Rimworld.Abillity AICanTargetNow using Harmony
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "GetAbilityJob")]
    public class GetAbillityJobPatch_CombatBuff
    {
        static int errorsSent = 0;
        public static void Postfix(ref Verse.AI.Job __result, JobGiver_AIFightEnemy __instance, Pawn pawn, Thing enemyTarget)
        {
            if (__result == null & pawn != null && pawn.Map != null && pawn.Map.pawnDestinationReservationManager != null)
            {
                try
                {
                    if (pawn.Position.Standable(pawn.Map) && pawn.Position != null && pawn.Map.pawnDestinationReservationManager.CanReserve(pawn.Position, pawn, pawn.Drafted) && pawn.Spawned && pawn.abilities != null)
                    {
                        foreach (var abillity in pawn.abilities
                            .AllAbilitiesForReading.Where(x => x != null && x.def.GetModExtension<CombatBuff>() != null && x.CanCast && x.verb != null && x.verb.CanHitTarget(pawn)))
                        {
                            __result = abillity.GetJob(pawn, pawn);
                        }
                    }
                }
                catch (NullReferenceException e)
                {
                    if (errorsSent < 10)
                    {
                        Log.Warning("Null Error in GetAbillityJobPatch_CombatBuff.\n" + e.Message);
                        errorsSent++;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
    public static class TryMeleeAttack_MeleeAbility_Patch
    {
        public static bool Prefix(Pawn_MeleeVerbs __instance, Thing target, Verb verbToUse = null, bool surpriseAttack = false)
        {
            var pawn = __instance.Pawn;
            try
            {
                if (pawn == null || pawn.Dead || pawn.Downed ||
                    // Abort if the pawn is player-controlled
                    (pawn.Faction != null && pawn.Faction.IsPlayer) ||
                    // Abort if busy or incapable of violence
                    pawn.stances.FullBodyBusy || pawn.WorkTagIsDisabled(WorkTags.Violent))
                {
                    return true;
                }
            }
            catch { return true; } // If there is something wrong with the pawn then just let the function run as normal. # NotMyProblem.

            // Get all abilities that can be used in melee
            if (pawn?.abilities != null)
            {
                var abilities = pawn.abilities.AllAbilitiesForReading.Where(x => x != null && x.CanCast && x.verb != null && x.verb.IsMeleeAttack || x.verb.verbProps?.range < 0 && x.def.aiCanUse);

                // For each ability which is not on cooldown.
                foreach (var abillity in abilities.Where(x=>x.CanCast))
                {
                    var tgInfo = new LocalTargetInfo(target);
                    if (abillity.CanApplyOn(tgInfo) && abillity.EffectComps.All(x => x.CanApplyOn(tgInfo, tgInfo)))
                    {
                        //// Check so the pawn doesn't already have this job
                        if (pawn.CurJob != null && pawn.CurJob.def.defName == abillity.def.defName)
                        {
                            return false;
                        }

                        pawn.jobs.StartJob(abillity.GetJob(tgInfo, tgInfo));
                        abillity.StartCooldown(abillity.def.cooldownTicksRange.max);
                        return false;
                    }
                }
            }

            // If there are no usable melee abilities, return true
            return true;
        }
    }

}
