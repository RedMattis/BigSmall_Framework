using BetterPrerequisites;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class PilotExtension : DefModExtension
    {
        public List<XenotypeChance> xenotypeChances = [];
        public List<PawnKindDef> pilotPawnkind = [];

        public void GeneratePilot(Pawn pawn)
        {
            // Find the first hediff of type Piloted
            var pilotedHediff = pawn.health.hediffSet.hediffs.Where(x => x is Piloted).FirstOrDefault();
            if (pilotedHediff == null)
            {
                // Generate hediff if it doesn't exist.
                pilotedHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("BS_Piloted"), pawn);
                pawn.health.AddHediff(pilotedHediff);
            }

            if (pilotedHediff is Piloted piloted)
            {
                // Grab the faction of the pawn.
                var faction = pawn.Faction;

                var pawnKind = pilotPawnkind.RandomElement();

                // Get a random xenotype from the list of valid xenotypes.
                var xenotype = xenotypeChances.RandomElementByWeight(x => x.chance).xenotype;
                var allValid = xenotypeChances.Select(x => x.xenotype).ToList();

                // Spawn a pawn of the ppropriate kind and xenotype.
                var pilot = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, PawnGenerationContext.NonPlayer, allowedXenotypes: allValid, forcedXenotype:xenotype,
                    forceGenerateNewPawn: true, mustBeCapableOfViolence:true, colonistRelationChanceFactor:0, canGeneratePawnRelations:false, relationWithExtraPawnChanceFactor:0)); // , forcedXenotype: xenotype


                if (pilot != null)
                {
                    // If the pilot has a weapon that is of the giant type, remove it.
                    if (pilot.equipment.Primary != null && pilot.equipment.Primary.def.weaponTags.Contains("BS_GiantWeapon"))
                    {
                        pilot.equipment.Remove(pilot.equipment.Primary);
                    }

                    HumanoidPawnScaler.GetCache(pilot, forceRefresh: true);
                    // If the pilot would be too big, give it the dwarfism trait.
                    if (pilot.BodySize > piloted.MaxCapacity)
                    {
                        var dwarfism = DefDatabase<TraitDef>.GetNamedSilentFail("Dwarfism");
                        if (dwarfism != null)
                        {
                            pilot.story.traits.GainTrait(new Trait(dwarfism));
                        }
                    }

                    // Add the pilot to the piloted hediff.
                    piloted.AddPilot(pilot);
                    piloted.pawn.health.Notify_HediffChanged(piloted);
                }
                else
                {
                    Log.Error("BigAndSmall: Error generating pilot for " + pawn.Name);
                }
            }
            else
            {
                Log.Error("BigAndSmall: Error generating pilotedHediff for " + pawn.Name);
            }
        }
    }

    [HarmonyPatch]
    public static class GeneratePawns_Patch
    {
        private static Pawn lastTouchedPawn = null;  // Make sure we're not editing the same pawn twice.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), [typeof(PawnKindDef), typeof(Faction)])]
        public static void GeneratePawnPostfix(ref Pawn __result, PawnKindDef kindDef, Faction faction)
        {
            if (__result == null || lastTouchedPawn == __result) return;
            lastTouchedPawn = __result;
            ModifyGeneratedPawn(false, __result);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnGroupMakerUtility), "GeneratePawns")]
        public static void GeneratePawnsPatch(PawnGroupMakerParms parms, bool warnOnZeroResults, ref IEnumerable<Pawn> __result)
        {
            bool changed = false;
            //foreach(var pawn in __result)
            //{
            //    if (pawn.kindDef.defName == "BS_SatanGreat" || pawn.kindDef.defName == "BS_Metatron")
            //    {
            //        // Add BS_Immortal gene to the pawns.
            //        pawn.genes.AddGene(BSDefs.BS_Immortal, xenogene:false);
            //    }
            //}
            

            var modifiedPawn = __result.ToList();
            foreach (var member in modifiedPawn)
            {
                if (member == null || lastTouchedPawn == member) continue;
                lastTouchedPawn = member;
                //Log.Message("DEBUG: Running Generate Patch in Unsafe Mode.");

                changed = ModifyGeneratedPawn(changed, member);
            }
            if (changed)
            {
                __result = modifiedPawn;
            }
        }

        private static bool ModifyGeneratedPawn(bool changed, Pawn member)
        {
            try
            {
                // Force refresh the pawn so they get correct body-size and stuff.
                if (HumanoidPawnScaler.GetCache(member, forceRefresh: true) is BSCache cache)
                {
                    changed = true;
                    try { TryModifyPawn(member); }
                    catch (Exception e) { Log.Warning($"BigAndSmall (GeneratePawns): Failed the TryModifyPawn for {member.Name} ({member.Label}): + {e.Message}"); }
                    try { RemoveInvalidThings(member); }
                    catch (Exception e) { Log.Warning($"BigAndSmall (GeneratePawns): Failed to remove invalid apparel for {member.Name} ({member.Label}): + {e.Message}"); }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"BigAndSmall (GeneratePawns): Failed to pregenerate pawn cache for {member.Name} ({member.Label}): + {e.Message}");
            }

            try
            {
                changed = GeneratePilots(changed, member);
            }
            catch (Exception e)
            {
                Log.Error("BigAndSmall: Error generating pilot for " + member.Name + ": " + e.Message);
            }

            return changed;
        }

        private static bool GeneratePilots(bool changed, Pawn member)
        {
            // Check if the pawnkind has the PilotExtension mod extension.
            if (member.kindDef.GetModExtension<PilotExtension>() is PilotExtension pilotExtension)
            {
                changed = GeneratePilot(changed, member, pilotExtension);
            }
            // Otherwise check if the Xenotype has one, as a fallback for stuff like the xenotype being added via mods.
            else if (member.genes?.Xenotype?.GetModExtension<PilotExtension>() is PilotExtension xenotypePilotExtension)
            {
                changed = GeneratePilot(changed, member, xenotypePilotExtension);
            }

            //// As a fallback, generate pilots with random xenotypes for all pawns that have pilotable genes or hediffs.
            //else if (member.genes.GenesListForReading.Any(x => x.def.defName == "BS_Pilotable")
            //    || member.health.hediffSet.hediffs.Any(x => x.def.defName == "BS_Piloted" || x.def.defName == "BATR_Piloted"))
            //{
            //    try
            //    {
            //        // Get faction xenotypes
            //        var factionXenotypeSet = member.Faction.def.xenotypeSet;
            //        var allXenos = DefDatabase<XenotypeDef>.AllDefsListForReading;
            //        var allValid = allXenos.Where(x => factionXenotypeSet.Contains(x)).ToList();

            //        // Get all xenotypes that don't contain any gene with forced trait "BS_Giant"
            //        var validXenos = allValid.Where(x => !x.AllGenes.Any(y => y.forcedTraits.Any(t => t.def.defName == "BS_Giant"))).ToList();

            //        var xenotypeChancesFromValid = validXenos.Select(x => new XenotypeChance(x, 1f)).ToList();

            //        if (xenotypeChancesFromValid.Count == 0)
            //        {
            //            return changed;
            //        }

            //        PilotExtension.GeneratePilot(member, xenotypeChancesFromValid);
            //    }
            //    catch (Exception e)
            //    {
            //        Log.Error("BigAndSmall: Error generating pilot (fallback due to missing pilot defs in pawnkind) for " + member.Name + ": " + e.Message);
            //    }
            //}

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
                    Log.Error("BigAndSmall: Error generating pilot for " + member.Name + ": " + e.Message);
                }

                return changed;
            }
        }

        private static void TryModifyPawn(Pawn member)
        {
            if (member.kindDef.GetModExtension<PawnKindExtension>() is PawnKindExtension pawnKindExt)
            {
                if (pawnKindExt.ageCurve != null)
                {
                    member.ageTracker.AgeBiologicalTicks = (long)pawnKindExt.ageCurve.Evaluate(Rand.Value) * 3600000;
                }
                if (pawnKindExt.psylinkLevels is SimpleCurve psyLinkCurve && ModsConfig.RoyaltyActive)
                {
                    int countToSet = (int)psyLinkCurve.Evaluate(Rand.Value);

                    if (countToSet > 0)
                    {
                        Hediff_Level hediff_Level = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, member, member.health.hediffSet.GetBrain()) as Hediff_Level;
                        member.health.AddHediff(hediff_Level);
                        hediff_Level.SetLevelTo(countToSet);
                    }
                }
            }
            if (member?.RaceProps?.Humanlike != true) return;
            if (member.genes?.Xenotype == null) return;
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
            bool soloInfiltrator = Rand.Chance(BigSmallMod.settings.inflitratorChance);
            bool infiltratorRaid = BigSmallMod.settings.inflitratorRaidChance > BigAndSmallCache.globalRandNum;
            if ((soloInfiltrator || infiltratorRaid) && member.IsMutant == false)
            {
                try
                {
                    bool isEndogeneHuman = (member.genes?.Xenotype?.inheritable == true || member.genes.Xenotype == XenotypeDefOf.Baseliner) && 
                        member.def == ThingDefOf.Human;

                    int seed = infiltratorRaid ? (int)(BigAndSmallCache.globalRandNum*10000) : Rand.Range(0, 1000000);
                    (var xenotype, var infiltratorData) = GlobalSettings.GetRandomInfiltratorReplacementXenotype(member, seed, forceNeeded:!isEndogeneHuman, isFullRaid: !soloInfiltrator);
                    if (xenotype != null)
                    {
                        var prevXenotype = member.genes.Xenotype;
                        member.genes.SetXenotype(xenotype);
                        member.TrySwapToXenotypeThingDef();
                        if (infiltratorData.disguised)
                        {
                            member.genes.SetXenotype(prevXenotype);  // So they still show up as the previous xenotype.
                        }
                        if (infiltratorData.ideologyOf != null && ModsConfig.IdeologyActive && Find.FactionManager.AllFactions.Where(x => x.def == infiltratorData.ideologyOf).FirstOrDefault() is Faction firstMatchingFaction)
                        {
                            member.ideo?.SetIdeo(firstMatchingFaction.ideos.PrimaryIdeo);
                        }
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
