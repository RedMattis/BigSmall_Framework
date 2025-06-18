using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class BodyDefFusionsHelper
    {
        private static List<BodyDefFusion> instances = null;
        public static List<BodyDefFusion> Instances => instances ??= DefDatabase<BodyDefFusion>.AllDefs.ToList();
        public static List<MergableBody> MergableBodies => Instances.SelectMany(x => x.mergableBody).ToList();

        public static List<MergableBody> MergabeWithSetBodies = MergableBodies.Where(x => x.fuseSet).ToList();
        public static List<SimilarParts> PartSets => Instances.SelectMany(x => x.similarParts).ToList();
        public static List<BodyPartDef> PartsToSkip => Instances.SelectMany(x => x.bodyPartToSkip).ToList();
        public static List<Substitutions> Substitutions => Instances.SelectMany(x => x.substitutions).ToList();
        public static List<RetainableTrackers> RetainableTrackers => Instances.SelectMany(x => x.retainableTrackers).ToList();

        private static Dictionary<ThingDef, List<HediffDef>> _racesAndTrackers = null;
        public static Dictionary<ThingDef, List<HediffDef>> RacesAndTrackers => _racesAndTrackers ??= DefDatabase<ThingDef>.AllDefsListForReading
            .Where(x => x.GetRaceExtensions().Any())
            .Select(x => (x, x.GetRaceExtensions().SelectMany(y => y.RaceHediffs).ToList()))
            .ToDictionary(x => x.x, x => x.Item2);

        private static List<HashSet<HediffDef>> substitutableTrackers = null;

        public static List<HashSet<HediffDef>> GetSubstitutableTrackers(HediffDef trackerOne)
        {
            if (substitutableTrackers == null)
            {
                SetupSubstitutableTrackers();
                Log.Warning("Substitutable trackers not set up. This should be done before this runs. It will not perform as expected.");
            }
            return substitutableTrackers.Where(x => x.Contains(trackerOne)).ToList();
        }

        public static void SetupSubstitutableTrackers()
        {
            Dictionary<BodyDef, HashSet<HediffDef>> trackersUsingTheSameBodyDef = [];
            var retainableTrackers = RetainableTrackers;
            foreach (var retainable in retainableTrackers)
            {
                trackersUsingTheSameBodyDef.Add(retainable.target, [.. retainable.raceTrackers]);
            }

            foreach ((var thing, var hediffList) in RacesAndTrackers.Select(x => (x.Key, x.Value)))
            {
                var body = thing.race.body;
                if (!trackersUsingTheSameBodyDef.ContainsKey(body))
                {
                    trackersUsingTheSameBodyDef[body] = [];
                }
                foreach (var hediff in hediffList)
                {
                    trackersUsingTheSameBodyDef[body].Add(hediff);
                }
            }
            substitutableTrackers = [.. trackersUsingTheSameBodyDef.Values];

            //Print entire list of substitutable trackers with indentation and key.
            //foreach (var kvp in trackersUsingTheSameBodyDef)
            //{
            //    Log.Message($"Key: {kvp.Key.defName}");
            //    foreach (var hediff in kvp.Value)
            //    {
            //        Log.Message($"    Hediff: {hediff.defName}");
            //    }
            //}
        }

        public static float? Equavalence(BodyPartDef partOne, BodyPartDef partTwo)
        {
            //Log.Message($"Checking for equivalence between {partOne.defName} and {partTwo.defName}");
            float? highestValueFound = null;
            foreach (var partSet in PartSets)
            {
                if (partSet.Parts.Contains(partOne) && partSet.Parts.Contains(partTwo))
                {
                    highestValueFound = Math.Max(highestValueFound ?? 0, partSet.similarity);
                    //Log.Message($"Found similarity between {partOne.defName} and {partTwo.defName} of {partSet.similarity}");
                }
            }
            return highestValueFound;
        }
    }

}
