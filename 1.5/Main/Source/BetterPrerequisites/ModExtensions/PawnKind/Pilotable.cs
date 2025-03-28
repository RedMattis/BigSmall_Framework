using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class PilotExtension : DefModExtension
    {
        public List<XenotypeChance> xenotypeChances = [];
        public List<PawnKindDef> pilotPawnkind = [];
        public GeneDef pilotableGene = null;
        public Hediff pilotableHediff = null;

        public void GeneratePilot(Pawn pPawn)
        {
            if (ModsConfig.BiotechActive && pilotableGene != null)
            {
                pPawn.genes.AddGene(pilotableGene, false);
            }

            // Find the first hediff of type Piloted
            var pilotedHediff = pPawn.health.hediffSet.hediffs.Where(x => x is Piloted).FirstOrDefault();
            if (pilotedHediff == null)
            {
                string pilotedHediffString = pilotableHediff?.def.defName ?? "BS_Piloted";
                // Generate hediff if it doesn't exist.
                pilotedHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed(pilotedHediffString), pPawn);
                pPawn.health.AddHediff(pilotedHediff);
            }

            if (pilotedHediff is Piloted piloted)
            {
                // Grab the faction of the pawn.
                var faction = pPawn.Faction;

                PawnKindDef pawnKind = null;
                if (pilotPawnkind.Any())
                {
                    pawnKind = pilotPawnkind.RandomElement();
                }
                else
                {
                    //  Get a random pawn kind that is not already a pilotable pawnkind.
                    pawnKind = pPawn.Faction.def.pawnGroupMakers
                        .SelectMany(x => x.options)
                        .Select(x => x.kind)
                        .Where(x =>
                            x.isFighter && (x.modExtensions.NullOrEmpty() || !x.modExtensions.Any(x => x is PilotExtension)))
                        .RandomElement();
                }

                // Get a random xenotype from the list of valid xenotypes.
                var xenotype = xenotypeChances.RandomElementByWeight(x => x.chance).xenotype;
                var allValid = xenotypeChances.Select(x => x.xenotype).ToList();

                var request = new PawnGenerationRequest(pawnKind, faction, PawnGenerationContext.NonPlayer, allowedXenotypes: allValid, forcedXenotype: xenotype,
                    forceGenerateNewPawn: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0, canGeneratePawnRelations: false, relationWithExtraPawnChanceFactor: 0);

                // Spawn a pawn of the appropriate kind and xenotype.
                var pilot = PawnGenerator.GeneratePawn(request);

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
                    Log.Error("BigAndSmall: Error equipping and adding pilot for " + pPawn.Name);
                }
            }
            else
            {
                Log.Error($"BigAndSmall: Error generating pilotedHediff for {pPawn.Name}. The Pilot Hediff could not be generated");
            }
        }
    }
}
