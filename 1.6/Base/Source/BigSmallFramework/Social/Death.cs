using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    [HarmonyPatch]
    public class DeathThoughtPatches
    {
        [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_Relations")]
        [HarmonyPrefix]
        public static bool AppendThoughts_RelationsPrefix(Pawn victim, DamageInfo? dinfo, PawnDiedOrDownedThoughtsKind thoughtsKind, List<IndividualThoughtToAdd> outIndividualThoughts, List<ThoughtToAddToAll> outAllColonistsThoughts)
        {
            if (victim != null && thoughtsKind == PawnDiedOrDownedThoughtsKind.Died)
            {
                if (FastAcccess.GetCache(victim) is BSCache cache)
                {
                    if (cache.isDrone)
                    {
                        try
                        {
                            foreach (Pawn item in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive)
                            {
                                if (item == victim || item.needs == null || item.needs.mood == null || !PawnUtility.ShouldGetThoughtAbout(item, victim) || (item.MentalStateDef == MentalStateDefOf.SocialFighting && ((MentalState_SocialFighting)item.MentalState).otherPawn == victim))
                                {
                                    continue;
                                }
                                if (victim.Faction == Faction.OfPlayerSilentFail && victim.HostFaction != item.Faction && !victim.IsQuestLodger() && !victim.IsMutant && !victim.IsSlave)
                                {
                                    outIndividualThoughts.Add(new IndividualThoughtToAdd(BSDefs.BS_DroneDied, item, victim));
                                }
                            }
                        }
                        catch { } // Really don't care much if this fails.
                        return false;
                        }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory), 
            new Type[]
            {
                typeof(Thought_Memory),
                typeof(Pawn),
            })]
        [HarmonyPrefix]
        public static void TryGainMemoryPrefix(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn = null)
        {
            var pawn = __instance?.pawn;
            if (pawn != null && newThought != null && otherPawn != null)
            {
                if (FastAcccess.GetCache(pawn) is BSCache cache)
                {
                    if (cache.isDrone)
                    {
                        newThought.durationTicksOverride = newThought.DurationTicks / 5;
                    }
                }
            }
        }
    }
}
