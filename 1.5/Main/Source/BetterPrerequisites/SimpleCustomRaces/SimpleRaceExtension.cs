using BetterPrerequisites;
using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class RaceExtension : DefModExtension
    {
        // Can be used to whitelist/blacklist recipes.
        // Note that they still need to have the valid parts.


        public HediffDef raceHediff = null;
        //public SimpleRaceProperties properties = new();
        //public List<Aptitude> aptitudes = [];
        //public List<PawnRenderNodeProperties> renderNodeProperties = [];

        public void ApplyTrackerIfMissing(Pawn pawn)
        {
            if (TrackerMissing(pawn))
            {
                ApplyHediffToPawn(pawn);
            }
        }
        public bool TrackerMissing(Pawn pawn)
        {
            // Check if the raceHediff is not null and if the pawn has the raceHediff.
            return raceHediff != null && !pawn.health.hediffSet.HasHediff(raceHediff);
        }
        private void ApplyHediffToPawn(Pawn pawn)
        {
            if (raceHediff != null)
            {
                // Remove all other RaceTracker Hediffs
                RemoveOldRaceTrackers(pawn);

                // Ensure the raceDef is of the "RaceTracker" class or a subclass thereof.
                if (raceHediff.hediffClass == typeof(RaceTracker))
                {
                    pawn.health.AddHediff(raceHediff);
                }
                else
                {
                    Log.Error($"{pawn}'s raceDef needs to be a {nameof(RaceTracker)} or subclass thereof.");
                }

                // Ensure the Hediff has a RaceCompProps component
                if (raceHediff.HasComp(typeof(HediffComp_Race))) { }
                else { Log.Error($"{pawn}'s raceDef needs to have a {nameof(HediffComp_Race)} component."); }

                pawn.health.AddHediff(raceHediff);
            }
            else
            {
                Log.Error($"{pawn} has a BigAndSmall.RaceExtension without an associated raceDef!");
            }
        }

        public void SwapToThisRace(Pawn pawn, bool force = false)
        {
            if (raceHediff != null)
            {
                RaceMorpher.SwapThingDef(pawn, pawn.def, false, force: force);
            }
            else { Log.Error($"{pawn} has a BigAndSmall.RaceExtension without an associated raceDef!"); }
        }

        public static void RemoveOldRaceTrackers(Pawn pawn)
        {
            var oldRaceTrackers = pawn.health?.hediffSet?.hediffs?.Where(h => h is RaceTracker);
            if (oldRaceTrackers == null) return;

            var extensions = ModExtHelper.GetHediffExtensions<PawnExtension>(pawn, parentWhitelist: [typeof(RaceTracker)]);

            var ort = oldRaceTrackers.ToList();
            for (int idx = ort.Count - 1; idx >= 0; idx--)
            {
                Hediff hediff = ort[idx];
                if (hediff is RaceTracker)
                {
                    pawn.health.hediffSet.hediffs.Remove(hediff);
                }
            }

            // Remove all forced traits, hediffs and genes.
            foreach (var ext in extensions)
            {
                if (ext.forcedHediffs != null)
                {
                    foreach (var hediff in ext.forcedHediffs)
                    {
                        if (pawn.health.hediffSet.HasHediff(hediff))
                        {
                            pawn.health.hediffSet.hediffs.Remove(pawn.health.hediffSet.GetFirstHediffOfDef(hediff));
                        }
                    }
                }
                if (ext.forcedEndogenes != null)
                {
                    HashSet<GeneDef> genesToRemove = [.. ext.forcedEndogenes ?? ([]), .. ext.forcedXenogenes ?? ([]), .. ext.immutableEndogenes ?? ([])];

                    foreach (var gene in genesToRemove)
                    {
                        if (pawn.genes.GenesListForReading.Any(g => g.def == gene))
                        {
                            pawn.genes.RemoveGene(pawn.genes.GenesListForReading.First(g => g.def == gene));
                        }
                    }
                }
                if (ext.forcedTraits != null)
                {
                    foreach (var trait in ext.forcedTraits)
                    {
                        if (pawn.story.traits.HasTrait(trait))
                        {
                            pawn.story.traits.allTraits.Remove(pawn.story.traits.GetTrait(trait));
                        }
                    }
                }
            }
        }
    }

}
