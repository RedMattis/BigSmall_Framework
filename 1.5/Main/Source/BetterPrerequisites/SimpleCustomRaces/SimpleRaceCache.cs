using BetterPrerequisites;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public partial class BSCache : IExposable, ICacheable
    {
        private void SimpleRaceUpdate(List<PawnExtension> raceProps, List<PawnExtension> pawnExtensions, CompProperties_Race raceCompProps)
        {
            Metamorphosis.HandleMetamorph(pawn, pawnExtensions);
            List<PawnExtension> allExt = [..raceProps, ..pawnExtensions];
            foreach (var pawnExt in allExt)
            {
                ProcessRaceGeneRequirements(pawnExt);
                ProcessRaceTraitRequirements(pawnExt);
                ProcessRaceHediffRequirements(pawnExt);
            }
            raceCompProps.EnsureCorrectBodyType(pawn);
            raceCompProps.EnsureCorrectHeadType(pawn);
        }

        private void ProcessRaceGeneRequirements(PawnExtension raceProps)
        {
            if (pawn.genes != null)
            {
                var endoGenesToRestore = endogenesRemovedByRace.Where(raceProps.IsGeneLegal).ToList();
                RestoreGenes(raceProps, endoGenesToRestore, false);
                var xenogenesToRestore = xenoenesRemovedByRace.Where(raceProps.IsGeneLegal).ToList();
                RestoreGenes(raceProps, xenogenesToRestore, true);

                int geneCount = pawn.genes.GenesListForReading.Count;
                raceProps.ForcedEndogenes.Where(g => !pawn.HasGene(g)).ToList().ForEach(g => pawn.genes.AddGene(g, false));
                raceProps.forcedXenogenes?.Where(g => !pawn.HasGene(g)).ToList().ForEach(g => pawn.genes.AddGene(g, true));

                var xenoGenesToRemove = pawn.genes.Xenogenes.Where(g => !raceProps.IsGeneLegal(g.def)).ToList();
                if (xenoGenesToRemove.Count > 0)
                {
                    for (int idx = xenoGenesToRemove.Count - 1; idx >= 0; idx--)
                    {
                        Gene gene = xenoGenesToRemove[idx];
                        xenoenesRemovedByRace.Add(gene.def);
                        pawn.genes.RemoveGene(gene);
                    }
                }
                var endogenesToRemove = pawn.genes.Endogenes.Where(g => !raceProps.IsGeneLegal(g.def)).ToList();
                if (endogenesToRemove.Count > 0)
                {
                    for (int idx = endogenesToRemove.Count - 1; idx >= 0; idx--)
                    {
                        Gene gene = endogenesToRemove[idx];
                        endogenesRemovedByRace.Add(gene.def);
                        pawn.genes.RemoveGene(gene);
                    }
                }
            }

            List<GeneDef> RestoreGenes(PawnExtension raceProps, List<GeneDef> genesToRestore, bool xeno)
            {
                if (genesToRestore.Count > 0)
                {
                    genesToRestore.ForEach(g => pawn.genes.AddGene(g, xeno));
                    Log.Message($"Big & Small: Restored {genesToRestore.Count} genes to {pawn.Name} that had been removed by their race.");

                    endogenesRemovedByRace.RemoveAll(g => genesToRestore.Contains(g));
                    xenoenesRemovedByRace.RemoveAll(g => genesToRestore.Contains(g));
                }

                return genesToRestore;
            }
        }

        private void ProcessRaceTraitRequirements(PawnExtension props)
        {
            if (pawn.story?.traits is TraitSet traits)
            {
                var traitsToRemove = traits.allTraits.Where(t => !props.IsTraitLegal(t.def)).ToList();
                if (traitsToRemove.Count > 0)
                {
                    for (int idx = traitsToRemove.Count - 1; idx >= 0; idx--)
                    {
                        Trait trait = traitsToRemove[idx];
                        traits.allTraits.Remove(trait);
                    }
                }

                props.forcedTraits?.Where(t => !traits.HasTrait(t)).ToList().ForEach(t => traits.GainTrait(new Trait(t, 0, true)));
            }
        }

        private void ProcessRaceHediffRequirements(PawnExtension props)
        {
            if (pawn.health?.hediffSet != null)
            {
                // Ensure forcedHediffs are present.
                props.forcedHediffs?.Where(h => !pawn.health.hediffSet.HasHediff(h)).ToList().ForEach(h => pawn.health.AddHediff(h));

                List<Hediff> hediffsToRemove = pawn.health.hediffSet.hediffs.Where(h => !props.IsHediffLegal(h.def)).ToList();
                if (hediffsToRemove.Count > 0)
                {
                    for (int idx = hediffsToRemove.Count - 1; idx >= 0; idx--)
                    {
                        Hediff hediff = hediffsToRemove[idx];
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }
        }
        private void ProcessHairFilters(PawnExtension props)
        {
            PawnStyleItemChooser.RandomHairFor(pawn);
        }
    }
}
