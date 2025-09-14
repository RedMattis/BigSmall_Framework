using BigAndSmall;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class GeneratePawns_Patch
    {
        private static Pawn lastTouchedPawn = null;  // Make sure we're not editing the same pawn twice.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), [typeof(PawnKindDef), typeof(Faction), typeof(PlanetTile)])]
        public static void GeneratePawnPostfix(ref Pawn __result, PawnKindDef kindDef, Faction faction)
        {
            if (__result == null || lastTouchedPawn == __result) return;
            lastTouchedPawn = __result;
            ModifyGeneratedPawn(false, ref __result, singlePawn:true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), [typeof(PawnGenerationRequest)])]
        public static void GeneratePawnPostfix(ref Pawn __result, PawnGenerationRequest request)
        {
            if (__result == null || lastTouchedPawn == __result) return;
            lastTouchedPawn = __result;
            ModifyGeneratedPawn(false, ref __result, singlePawn: true);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns))]
        public static void GeneratePawnsPatch(PawnGroupMakerParms parms, bool warnOnZeroResults, ref IEnumerable<Pawn> __result)
        {
            bool changed = false;

            var modifiedPawn = __result.ToList();
            for (int idx = modifiedPawn.Count - 1; idx >= 0; idx--)
            {
                Pawn member = modifiedPawn[idx];
                if (member == null || lastTouchedPawn == member) continue;
                lastTouchedPawn = member;

                changed = ModifyGeneratedPawn(changed, ref member);
            }
            if (changed)
            {
                __result = modifiedPawn;
            }
        }

        private static bool ModifyGeneratedPawn(bool changed, ref Pawn member, bool singlePawn=false)
        {
            try
            {
                // Force refresh the pawn so they get correct body-size and stuff.
                if (HumanoidPawnScaler.GetCache(member, forceRefresh: true) is BSCache cache)
                {
                    changed = true;

                    try { member = TryModifyPawn(member, singlePawn: singlePawn); }
                    catch (Exception e) { Log.Warning($"BigAndSmall (GeneratePawns): Failed the TryModifyPawn for {member.Name} ({member.Label}): + {e.Message}\n{e.StackTrace}"); }
                    try { RemoveInvalidThings(member); }
                    catch (Exception e) { Log.Warning($"BigAndSmall (GeneratePawns): Failed to remove invalid apparel for {member.Name} ({member.Label}): + {e.Message}\n{e.StackTrace}"); }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"BigAndSmall (GeneratePawns): Failed to pregenerate pawn cache for {member.Name} ({member.Label}): {e.Message}\n{e.StackTrace}");
            }

            try
            {
                changed = GeneratePilots(changed, member);
            }
            catch (Exception e)
            {
                Log.Error($"BigAndSmall: Error in {nameof(ModifyGeneratedPawn)} generating pilot for {member.Name}:\n{e.Message}\n{e.StackTrace}");
            }

            return changed;
        }

        private static bool GeneratePilots(bool changed, Pawn member)
        {

            // Check if the pawnkind has the PilotExtension mod extension.
            if (member?.kindDef?.GetModExtension<PilotExtension>() is PilotExtension pilotExtension)
            {
                changed = GeneratePilot(changed, member, pilotExtension);
            }
            // Otherwise check if the Xenotype has one, as a fallback for stuff like the xenotype being added via mods.
            else if (member?.genes?.Xenotype?.GetModExtension<PilotExtension>() is PilotExtension xenotypePilotExtension)
            {
                changed = GeneratePilot(changed, member, xenotypePilotExtension);
            }

            return changed;

            static bool GeneratePilot(bool changed, Pawn member, PilotExtension pilotExtension)
            {
                try
                {
                    // Generate a pilot for the pawn.
                    pilotExtension.GeneratePilot(member);
                    changed = true;
                }
                catch (Exception e)
                {
                    Log.Error($"BigAndSmall: Error generating pilot for {member.Name}: {e.Message}\n{e.StackTrace}");
                }

                return changed;
            }
        }

        private static Pawn TryModifyPawn(Pawn member, bool singlePawn=false)
        {
            if (member.kindDef?.GetModExtension<PawnKindExtension>() is PawnKindExtension pawnKindExt)
            {
                member = pawnKindExt.Execute(member, singlePawn: singlePawn);
            }
            return TryModifyHumanlike(member);
        }

        private static Pawn TryModifyHumanlike(Pawn member)
        {
            if (member?.RaceProps?.Humanlike != true) return member;
            if (member.genes?.Xenotype == null) return member;
            if (member.genes.Xenotype.GetModExtension<XenotypeExtension>() is XenotypeExtension xenotypeExt)
            {
                if (xenotypeExt.setRace != null)
                {
                    try
                    {
                        RaceMorpher.SwapThingDef(member, xenotypeExt.setRace, true, targetPriority: -100);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"BigAndSmall: Error swapping thingdef for {member?.Name}: {e.Message}. Skipping.");
                    }
                }
            }
            TrySetInfiltrator(member);
            return member;
        }

        private static void TrySetInfiltrator(Pawn member)
        {
            if (HumanlikeAnimals.IsHumanlikeAnimal(member?.def) == true)
            {
                // Don't set infiltrators for humanlike animals.
                return;
            }
            float chance = BigSmallMod.settings.inflitratorChance;
            if (Rand.Chance(0.1f)) /// 10% chance to increase the chance of infiltrator in a raid.
            {
                chance = Mathf.Min(chance * Rand.Range(1f, 10f), 1f - (1f - chance) / 2);
            }
            bool soloInfiltrator = Rand.Chance(BigSmallMod.settings.inflitratorChance);

            // It can only be an infiltrator raid if it is hostile.
            bool infiltratorRaid = member?.Faction.HostileTo(Faction.OfPlayerSilentFail) == true
                && BigSmallMod.settings.inflitratorRaidChance > BigAndSmallCache.globalRandNum;
            if ((soloInfiltrator || infiltratorRaid) && member.IsMutant == false)
            {
                try
                {
                    bool isEndogeneHuman = (member.genes?.Xenotype?.inheritable == true || member.genes.Xenotype == XenotypeDefOf.Baseliner) &&
                        member.def == ThingDefOf.Human;

                    int seed = infiltratorRaid ? (int)(BigAndSmallCache.globalRandNum * 10000) : Rand.Range(0, 1000000);
                    (var xenotype, var infiltratorData) = GlobalSettings.GetRandomInfiltratorReplacementXenotype(member, seed, forceNeeded: !isEndogeneHuman, isFullRaid: !soloInfiltrator);
                    if (xenotype != null)
                    {
                        var prevXenotype = member.genes.Xenotype;
                        member.genes.SetXenotype(xenotype);
                        member.TrySwapToXenotypeThingDef();
                        if (infiltratorData.disguised && prevXenotype != null)
                        {
                            member.genes.iconDef = null;
                            member.genes.SetXenotypeDirect(prevXenotype);
                        }
                        if (infiltratorData.ideologyOf != null && ModsConfig.IdeologyActive && Find.FactionManager.AllFactions.Where(x => x.def == infiltratorData.ideologyOf).FirstOrDefault() is Faction firstMatchingFaction)
                        {
                            member.ideo?.SetIdeo(firstMatchingFaction.ideos.PrimaryIdeo);
                        }
                        // Doesn't work great since animals etc. won't swap with the current logic.
                        //if (infiltratorRaid
                        //    && infiltratorData.canFactionSwap
                        //    && member.Faction.HostileTo(Faction.OfPlayerSilentFail)
                        //    && infiltratorData.ideologyOf is FactionDef newFaction)
                        //{
                        //    var newFactionInstance = Find.FactionManager.AllFactions.Where(x => x.def == newFaction).FirstOrDefault();
                        //    if (newFactionInstance != null)
                        //    {
                        //        member.SetFaction(newFactionInstance);
                        //    }
                        //}
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"BigAndSmall: Error swapping {member?.Name} to infiltrator: {e.Message}. Skipping.");
                }
            }
        }

        private static void RemoveInvalidThings(Pawn member)
        {
            if (member?.RaceProps?.Humanlike != true) return;
            // Iterate thought all apparels and remove invalid ones.
            var wornApparel = member.apparel.WornApparel;
            foreach (var apparel in wornApparel.ToList())
            {
                bool canEquip = true;
                string cantReason = "";
                canEquip = CanEquipPatches.CanEquipThing(canEquip, apparel.def, member, ref cantReason);
                if (!canEquip)
                {
                    member.apparel.Remove(apparel);
                }
            }

            // Same for equipment.
            var equipment = member.equipment.AllEquipmentListForReading;
            foreach (var equip in equipment.ToList())
            {
                bool canEquip = true;
                string cantReason = "";
                canEquip = CanEquipPatches.CanEquipThing(canEquip, equip.def, member, ref cantReason);
                if (!canEquip)
                {
                    member.equipment.Remove(equip);
                }
            }
        }
    }
}
