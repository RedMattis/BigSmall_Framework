using BigAndSmall.FilteredLists;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public partial class BSCache : IExposable, ICacheable
    {
        private void SimpleRaceUpdate(List<PawnExtension> raceExts, List<PawnExtension> otherPawnExt, List<CompProperties_Race> raceCompProps)
        {
            List<PawnExtension> allExt = [.. raceExts, .. otherPawnExt];
            UpdateGeneOverrideStates();
            Metamorphosis.HandleMetamorph(pawn, allExt);
            ProcessRaceGeneRequirements(raceExts);
            ProcessRaceTraitRequirements(raceExts);
            ProcessRaceHediffRequirements(raceExts);
            ProcessHediffsToRemove(allExt);
            UpdateGeneOverrideStates();  // Run again here in case Metamorph etc. changed the state.
            raceCompProps.EnsureValidBodyType(this);
            raceCompProps.EnsureValidHeadType(this);
        }

        private void ProcessRaceGeneRequirements(List<PawnExtension> raceExts)
        {
            if (pawn.genes != null)
            {
                // Ensure they are initialized. They could have been scribed an old value.
                endogenesRemovedByRace ??= [];
                xenoenesRemovedByRace ??= [];


                //List<GeneDef> endoGenesToRestore = endogenesRemovedByRace.Where(g => raceExts
                //    .Select(ext => ext.IsGeneLegal(g))
                //    .Aggregate((a, b) => a.Fuse(b)).Accepted()).ToList();

                //RestoreGenes(endoGenesToRestore, false);
                //List<GeneDef> xenogenesToRestore = xenoenesRemovedByRace.Where(g => raceExts
                //    .Select(ext => ext.IsGeneLegal(g))
                //    .Aggregate((a, b) => a.Fuse(b)).Accepted()).ToList();
                //RestoreGenes(xenogenesToRestore, true);

                raceExts.ForEach(ext => ext.ForcedEndogenes.Where(g => !pawn.HasGene(g)).ToList().ForEach(g => pawn.genes.AddGene(g, false)));
                raceExts.ForEach(ext => ext.forcedXenogenes?.Where(g => !pawn.HasGene(g)).ToList().ForEach(g => pawn.genes.AddGene(g, true)));

                List<Gene> xenoGenesToRemove = pawn.genes.Xenogenes.Where(g => raceExts.Select(ext => ext.IsGeneLegal(g.def))
                    .Aggregate((a, b) => a.Fuse(b)).Denied()).ToList();
                if (xenoGenesToRemove.Count > 0)
                {
                    for (int idx = xenoGenesToRemove.Count - 1; idx >= 0; idx--)
                    {
                        Gene gene = xenoGenesToRemove[idx];
                        xenoenesRemovedByRace.Add(gene.def);
                        pawn.genes.RemoveGene(gene);
                    }
                }
                List<Gene> endogenesToRemove = pawn.genes.Endogenes.Where(g => raceExts.Select(ext => ext.IsGeneLegal(g.def))
                    .Aggregate((a, b) => a.Fuse(b)).Denied()).ToList();
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
                FilterListSet <TraitDef> traitFilter = raceExts.Where(x => x.traitFilters != null).Select(x => x.traitFilters).MergeFilters();
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
                FilterListSet<HediffDef> hediffFilter = raceExts.Where(x => x.hediffFilters != null).Select(x=>x.hediffFilters).MergeFilters();
                HashSet<HediffDef> forcedHediffs = raceExts.SelectMany(x => x.forcedHediffs ?? []).ToHashSet();
                forcedHediffs.Where(h => !pawn.health.hediffSet.HasHediff(h) &&
                    hediffFilter == null || hediffFilter.GetFilterResult(h).Accepted()).ToList().ForEach(h => pawn.health.AddHediff(h));
                
            }
        }

        private void ProcessHediffsToRemove(List<PawnExtension> pawnExts)
        {
            FilterListSet<HediffDef> hediffFilter = pawnExts.Where(x=>x.hediffFilters != null).Select(x => x.hediffFilters).MergeFilters();

            List<Hediff> hediffsToRemove = [];
            if (hediffFilter == null || hediffFilter.IsEmpty())
            {
               if (banAddictions)
                {
                    hediffsToRemove = pawn.health.hediffSet.hediffs.Where(h => h is Hediff_Addiction).ToList();
                }
            }
            else
            {
                hediffsToRemove = pawn.health.hediffSet.hediffs.Where(h => 
                    hediffFilter.GetFilterResult(h.def).Denied() ||
                    // If a chemical dependency and not whitelisted, remove it.
                    (h is Hediff_Addiction hd && banAddictions && hediffFilter.GetFilterResult(h.def).NotExplicitlyAllowed())

                ).ToList();
            }
            
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
