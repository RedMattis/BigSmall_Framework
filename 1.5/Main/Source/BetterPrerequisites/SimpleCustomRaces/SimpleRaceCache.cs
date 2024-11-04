using BetterPrerequisites;
using BigAndSmall.FilteredLists;
using HarmonyLib;
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
        private void SimpleRaceUpdate(List<PawnExtension> raceExts, List<PawnExtension> otherPawnExt, CompProperties_Race raceCompProps)
        {

            List<PawnExtension> allExt = [.. raceExts, .. otherPawnExt];
            Metamorphosis.HandleMetamorph(pawn, allExt);
            ProcessRaceGeneRequirements(raceExts);
            ProcessRaceTraitRequirements(raceExts);
            ProcessRaceHediffRequirements(raceExts);
            ProcessHediffsToRemove(allExt);
            raceCompProps.EnsureCorrectBodyType(pawn);
            raceCompProps.EnsureCorrectHeadType(pawn);
        }

        private void ProcessRaceGeneRequirements(List<PawnExtension> raceExts)
        {
            if (pawn.genes != null)
            {
                // Ensure they are initialized. They could have been scribed an old value.
                endogenesRemovedByRace ??= [];
                xenoenesRemovedByRace ??= [];


                var endoGenesToRestore = endogenesRemovedByRace.Where(g=> raceExts.All(ext => ext.IsGeneLegal(g))).ToList();
                RestoreGenes(endoGenesToRestore, false);
                var xenogenesToRestore = xenoenesRemovedByRace.Where(g => raceExts.All(ext => ext.IsGeneLegal(g))).ToList();
                RestoreGenes(xenogenesToRestore, true);

                raceExts.ForEach(ext => ext.ForcedEndogenes.Where(g => !pawn.HasGene(g)).ToList().ForEach(g => pawn.genes.AddGene(g, false)));
                raceExts.ForEach(ext => ext.forcedXenogenes?.Where(g => !pawn.HasGene(g)).ToList().ForEach(g => pawn.genes.AddGene(g, true)));

                var xenoGenesToRemove = pawn.genes.Xenogenes.Where(g => raceExts.Any(ext => !ext.IsGeneLegal(g.def))).ToList();
                if (xenoGenesToRemove.Count > 0)
                {
                    for (int idx = xenoGenesToRemove.Count - 1; idx >= 0; idx--)
                    {
                        Gene gene = xenoGenesToRemove[idx];
                        xenoenesRemovedByRace.Add(gene.def);
                        pawn.genes.RemoveGene(gene);
                    }
                }
                var endogenesToRemove = pawn.genes.Endogenes.Where(g => raceExts.Any(ext => !ext.IsGeneLegal(g.def))).ToList();
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

            List<GeneDef> RestoreGenes(List<GeneDef> genesToRestore, bool xeno)
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

        private void ProcessRaceTraitRequirements(List<PawnExtension> raceExts)
        {
            if (pawn.story?.traits is TraitSet traits)
            {
                var forcedTraits = raceExts.SelectMany(ext => ext.forcedTraits?.Where(t => !traits.HasTrait(t))).ToList();
                FilterListSet <TraitDef> traitFilter = raceExts.SelectWhere(x => x.traitFilters).MergeFilters();
                var traitsToRemove = traits.allTraits.Where(t => traitFilter != null && !forcedTraits.Any(ft => ft == t.def) && traitFilter.GetFilterResult(t.def).Denied()).ToList();
                if (traitsToRemove.Count > 0)
                {
                    for (int idx = traitsToRemove.Count - 1; idx >= 0; idx--)
                    {
                        Trait trait = traitsToRemove[idx];
                        traits.allTraits.Remove(trait);
                    }
                }

                if (forcedTraits.Count > 0)
                {
                    forcedTraits.Where(t => !traits.HasTrait(t)).ToList().ForEach(t => traits.GainTrait(new Trait(t, 0, true)));
                }
            }
        }

        private void ProcessRaceHediffRequirements(List<PawnExtension> raceExts)
        {
            if (pawn.health?.hediffSet != null)
            {
                FilterListSet<HediffDef> hediffFilter = raceExts.SelectWhere(x => x.hediffFilters).MergeFilters();
                HashSet<HediffDef> forcedHediffs = raceExts.SelectManyWhere(x => x.forcedHediffs).ToHashSet();
                forcedHediffs.Where(h => !pawn.health.hediffSet.HasHediff(h) &&
                    hediffFilter == null || hediffFilter.GetFilterResult(h).Accepted()).ToList().ForEach(h => pawn.health.AddHediff(h));
                
            }
        }

        private void ProcessHediffsToRemove(List<PawnExtension> pawnExts)
        {
            FilterListSet<HediffDef> hediffFilter = pawnExts.SelectWhere(x => x.hediffFilters).MergeFilters();
            List<Hediff> hediffsToRemove = pawn.health.hediffSet.hediffs.Where(h => hediffFilter != null && hediffFilter.GetFilterResult(h.def).Denied()).ToList();
            if (hediffsToRemove.Count > 0)
            {
                for (int idx = hediffsToRemove.Count - 1; idx >= 0; idx--)
                {
                    Hediff hediff = hediffsToRemove[idx];
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        private void ProcessHairFilters(PawnExtension props)
        {
            PawnStyleItemChooser.RandomHairFor(pawn);
        }
    }
}
