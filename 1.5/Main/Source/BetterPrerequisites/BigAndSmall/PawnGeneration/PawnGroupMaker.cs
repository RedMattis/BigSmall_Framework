using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    public class PilotExtension : DefModExtension
    {
        public List<XenotypeChance> xenotypeChances = new List<XenotypeChance>();
        public List<PawnKindDef> pilotPawnkind = new List<PawnKindDef>();

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

                    HumanoidPawnScaler.GetBSDict(pilot, forceRefresh: true);
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
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnGroupMakerUtility), "GeneratePawns")]
        public static void GeneratePawnsPatch(PawnGroupMakerParms parms, bool warnOnZeroResults, ref IEnumerable<Pawn> __result)
        {
            bool changed = false;
            var modifiedPawn = __result.ToList();
            foreach (var member in modifiedPawn)
            {
                if (member == null) continue;
                try
                {
                    // Force refresh the pawn so they get correct body-size and stuff.
                    var cache = FastAcccess.GetCache(member, true);
                    changed = true;

                    if (cache != null)
                    {
                        RemoveInvalidThings(member);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"BigAndSmall: Failed to pregenerate pawn cache for {member.Name} ({member.Label}): + {e.Message}");
                }

                try
                {
                    changed = GeneratePilots(changed, member);
                }
                catch (Exception e)
                {
                    Log.Error("BigAndSmall: Error generating pilot for " + member.Name + ": " + e.Message);
                }
            }
            if (changed)
            {
                __result = modifiedPawn;
            }
        }

        private static bool GeneratePilots(bool changed, Pawn member)
        {
            // Check if the pawnkind has the PilotExtension mod extension.
            if (member.kindDef.HasModExtension<PilotExtension>())
            {
                try
                {
                    // Grab the PilotExtension mod extension.
                    var pilotExtension = member.kindDef.GetModExtension<PilotExtension>();
                    // Generate a pilot for the pawn.
                    pilotExtension.GeneratePilot(member);
                    changed = true;
                }
                catch (Exception e)
                {
                    Log.Error("BigAndSmall: Error generating pilot for " + member.Name + ": " + e.Message);
                }
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
        }

        private static void RemoveInvalidThings(Pawn member)
        {
            // Iterate thought all apparels and remove invalid ones.
            var wornApparel = member.apparel.WornApparel;
            foreach (var apparel in wornApparel.ToList())
            {
                bool canEquip = true;
                string cantReason = "";
                canEquip = GiantTraitPatches.CanEquipThing(canEquip, apparel.def, member, ref cantReason);
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
                canEquip = GiantTraitPatches.CanEquipThing(canEquip, equip.def, member, ref cantReason);
                if (!canEquip)
                {
                    member.equipment.Remove(equip);
                }
            }
        }
    }
}
