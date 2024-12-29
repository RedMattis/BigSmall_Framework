using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using static BigAndSmall.BodyDefFusionsHelper;
using static HarmonyLib.AccessTools;

namespace BigAndSmall
{
    

    public static partial class RaceFuser
    {
        public const string MESH_DEF = "Mech";
        public const string MESH_LABEL = "BS_Mech";
        public static bool doDebug = false;

        public static HashSet<BodyDef> bodyDefsAdded = [];
        public static HashSet<ThingDef> thingDefsAdded = [];

        public static void PreHotreload()
        {
            FusedBody.FusedBodies.Clear();
            FusedBody.FusedBodyByThing.Clear();
            bodyDefsAdded.ToList().ForEach(bodyDef =>
            {
                bodyDef.AllParts.Clear();
                //DefDatabase<BodyDef>.AllDefsListForReading.Remove(bodyDef);
            });
            //thingDefsAdded.ToList().ForEach(thingDef => DefDatabase<ThingDef>.AllDefsListForReading.Remove(thingDef));
            //bodyDefsAdded.Clear();
            //thingDefsAdded.Clear();
        }
        /// <summary>
        /// Merge body parts.
        /// </summary>
        public static void CreateMergedBodyTypes(bool hotReload)
        {
            SetupSubstitutableTrackers();
            if (doDebug) { Log.Message($"Found Mergable Bodies: {MergableBodies.Select(x => x.bodyDef.defName).ToCommaList()}"); }

            List<MergableBody> bodyDefsToMerge = DefDatabase<BodyDef>.AllDefsListForReading
                .Where(x => MergableBodies.Any(y => y.bodyDef == x))
                .Select(x => MergableBodies.First(y => y.bodyDef == x)).ToList();

            if (doDebug)
            {
                Log.Message($"Merging {bodyDefsToMerge.Count} body types.");
                Log.Message($"Merging {bodyDefsToMerge.Select(x => x.bodyDef.defName).ToCommaList()}");
            }

            if (bodyDefsToMerge.Count < 2)
            {
                return;
            }

            bodyDefsToMerge = bodyDefsToMerge.OrderByDescending(x => x.priority).ToList();

            //HashSet<(MergableBody, MergableBody), BodyDef> generatedBodyDefs = [];

            List<MergableBody> fuseAllBodies = MergableBodies.Where(x => x.fuseAll).ToList();

            // To store all fusion combinations
            List<List<MergableBody>> allFusionCombinations = [];
            int n = fuseAllBodies.Count;
            // Generate all possible subsets (power set)
            for (int i = 0; i < (1 << n); i++) // 2^n combinations
            {
                List<MergableBody> combination = [];
                for (int j = 0; j < n; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        combination.Add(fuseAllBodies[j]);
                    }
                }
                allFusionCombinations.Add(combination);
            }
            var fuseSetBodyDefs = MergableBodies.Where(x => x.fuseSet).ToList();

            // Perform standard fusions.
            RunStandardFusions(bodyDefsToMerge, allFusionCombinations);
            RunFuseSetsOnFused(fuseSetBodyDefs);
            RunFuseSetsOnSources(bodyDefsToMerge, fuseSetBodyDefs);
            GenerateAndRegisterRaceDefs(hotReload);

            if (doDebug)
            {
                Log.Message("------------------------------------------------------\n" +
                            "Sources:\n" +
                            "------------------------------------------------------");
                foreach (var body in bodyDefsToMerge)
                {
                    Log.Message($"* {body.bodyDef.LabelCap}");
                }


                Log.Message("------------------------------------------------------\n" +
                            "Results:\n" +
                            "------------------------------------------------------");
                foreach (var fusedBody in FusedBody.FusedBodies.Values)
                {
                    string srcbodies = string.Join(", ", fusedBody.mergableBodies.Select(x => x.bodyDef.LabelCap)); ;
                    string logMessage = $"{fusedBody.generatedBody.LabelCap,-45} (bp: {fusedBody.generatedBody.AllParts.Count}, src: {srcbodies})";
                    Log.Message(logMessage);
                }
            }
        }

        private static void RunFuseSetsOnFused(List<MergableBody> fuseSets)
        {
            var targets = FusedBody.FusedBodies.Values.ToList();
            if (doDebug) Log.Message($"Running FuseSets on Fused. There are {fuseSets.Count} sets to fuse and {targets.Count} targets to fuse them with");
            foreach (var fusedSetBody in fuseSets.Where(x => x.fuseSet))
            {
                foreach (var fusedBody in targets.Where(x => !x.fake))
                {
                    string defName = fusedSetBody.overrideDefNamer ?? fusedSetBody.bodyDef.defName;
                    var genBody = MakeBodyDef(fusedSetBody, defName, mechanicalAlt: true, fusedBody.mergableBodies);
                    ClonePartsRecursive(null, fusedBody.generatedBody.corePart, genBody, fusedBody.mergableBodies[0], [], makeMechanical: fusedSetBody.isMechanical);
                    new FusedBody(genBody, fusetSetBody:fusedSetBody, mechanical: fusedSetBody.isMechanical, [fusedSetBody, ..fusedBody.mergableBodies]);
                    if (doDebug) Log.Message($"FuseSets->Fused: {genBody.defName} from {fusedBody.generatedBody.defName} and {fusedSetBody.bodyDef.defName}");
                }
            }
        }

        private static void RunFuseSetsOnSources(List<MergableBody> bodyDefsToMerge, List<MergableBody> fuseSets)
        {
            if (doDebug) Log.Message($"Running FuseSets on Sources. There are {fuseSets.Count} sets to fuse, and {bodyDefsToMerge.Count} sources to fuse them with.");
            foreach (var fusedSetBody in fuseSets.Where(x => x.fuseSet))
            {
                foreach (var baseMergable in bodyDefsToMerge.Where(x => x != fusedSetBody && (fusedSetBody.isMechanical == false || x.canMakeRobotVersion)))
                {
                    //var existing = DefDatabase<BodyDef>.AllDefs.FirstOrDefault(x => x.defName == newDefName);
                    string defName = fusedSetBody.overrideDefNamer ?? fusedSetBody.bodyDef.defName;
                    var genBody = MakeBodyDef(fusedSetBody, defName, mechanicalAlt: fusedSetBody.isMechanical, [baseMergable]);
                    ClonePartsRecursive(null, baseMergable.bodyDef.corePart, genBody, baseMergable, [], makeMechanical: fusedSetBody.isMechanical);
                    new FusedBody(genBody, fusetSetBody: fusedSetBody, mechanical: fusedSetBody.isMechanical, fusedSetBody, baseMergable);
                    if (doDebug) Log.Message($"FuseSets->Source: {genBody.defName} from {baseMergable.bodyDef.defName} and {fusedSetBody.bodyDef.defName}");
                }
            }
        }

        private static void RunStandardFusions(List<MergableBody> bodyDefsToMerge, List<List<MergableBody>> allFusionCombinations)
        {
            foreach (var bodyOne in bodyDefsToMerge.Where(x => x.canBeFusionOne))
            {
                foreach (var bodyTwo in bodyDefsToMerge.Where(x => x != bodyOne &&
                !FusedBody.HasKey(mechanical: bodyOne.isMechanical, bodyOne.bodyDef, x.bodyDef) &&
                bodyOne.Fuse && x.Fuse))
                {
                    List<BodyDef> genBodies = [];
                    foreach (var fusionCombo in allFusionCombinations)
                    {
                        // Check so there are no duplicates.
                        if (fusionCombo.Contains(bodyOne) || fusionCombo.Contains(bodyTwo))
                        {
                            continue;
                        }

                        // Check if any exclusion tags match in any of the bodies. bodyOne, bodyTwo, and all fusionCombo bodies.
                        if (bodyOne.exclusionTags.Intersect(bodyTwo.exclusionTags).Any() ||
                            fusionCombo.Any(x =>
                                bodyOne.exclusionTags.Intersect(x.exclusionTags).Any() ||
                                bodyTwo.exclusionTags.Intersect(x.exclusionTags).Any() ||
                                fusionCombo.Any(y => y != x && x.exclusionTags.Intersect(y.exclusionTags).Any())))
                        {
                            continue;
                        }

                        var allBodyDef = fusionCombo.Select(x => x.bodyDef).Concat([bodyOne.bodyDef, bodyTwo.bodyDef]).ToArray();
                        if (FusedBody.HasKey(mechanical: false, allBodyDef))
                        {
                            continue;
                        }


                        var allPartsOne = bodyOne.bodyDef.AllParts.ToList();
                        var secondaryBodies = fusionCombo.ToList();
                        secondaryBodies.Insert(0, bodyTwo);

                        var newDefName = bodyOne.overrideDefNamer ?? bodyOne.bodyDef.defName;
                        var existing = DefDatabase<BodyDef>.AllDefs.FirstOrDefault(x => x.defName == newDefName);
                        BodyDef genBody;
                        if (existing != null)
                        {
                            genBody = existing;
                            //genBody.ClearCachedData();
                            //genBody.
                        }
                        else
                        {

                            genBody = MakeBodyDef(bodyOne, newDefName, mechanicalAlt: false, [.. secondaryBodies]);
                            ClonePartsRecursive(null, bodyOne.bodyDef.corePart, genBody, bodyOne, [], makeMechanical: false);

                            foreach (var fBody in secondaryBodies)
                            {

                                var fBodyCore = fBody.bodyDef.corePart;

                                //MergeBodies(mainBody, secondaryBody, generatedBody);

                                var allPartsTwo = fBody.bodyDef.AllParts.ToList();
                                var partsToTransfer = allPartsTwo.Where(x => !(bodyOne.ShouldRemovePart(x.def))).ToList();

                                var corePart = genBody.corePart;

                                MergeRecursively(genBody.corePart, fBodyCore, partsToTransfer, bodyOne);
                            }
                        }

                        //generatedBodyDefs[(bodyOne, bodyTwo)] = genBody;
                        new FusedBody(
                            genBody,
                            fusetSetBody:null,
                            mechanical: bodyOne.isMechanical || secondaryBodies.Any(x=>x.isMechanical),
                            [bodyOne, .. secondaryBodies]);
                    }
                }
            }
        }

        
        private static BodyDef MakeBodyDef(MergableBody bodyOne, string defName, bool mechanicalAlt, params MergableBody[] bodyTwoArray)
        {
            string defNameFull;
            string bodyOneLabel = bodyOne.prefixLabel ?? bodyOne.bodyDef.LabelCap;
            string fullLabel = "";
            string miniDefPrefix = mechanicalAlt ? "M" : "G";

            var labelOne = bodyOne.prefixLabel ?? bodyOne.bodyDef.LabelCap;
            string secondarySuffixLabels = "BS_With".Translate() + " " + string.Join($" {"BS_And".Translate()} ", bodyTwoArray.Select(x => x.suffixLabel ?? x.bodyDef.label));
            fullLabel = $"{bodyOne.prefixLabel} {secondarySuffixLabels}";
            List<string> secondaryDefNames = bodyTwoArray.Select(x => x.overrideDefNamer ?? x.bodyDef.defName).ToList();
            defNameFull = $"{miniDefPrefix}_{defName}{string.Join("", secondaryDefNames)}".Trim();

            BodyDef genBody = defNameFull.TryGetExistingDef<BodyDef>();
            genBody ??= new BodyDef();
            genBody.defName = defNameFull;
            genBody.description = bodyOne.bodyDef.description;
            genBody.label = fullLabel;
            if (doDebug) Log.Message($"Creating {genBody.defName} from {defNameFull}");
            return genBody;
        }

        private static void ReplacePartsWithMechanicalRecursive(BodyPartRecord source, BodyPartRecord genPartParen, BodyDef genBody)
        {

        }

        private static string GetPartsStringRecursive(BodyPartRecord source, string indent = "")
        {
            string result = $"{indent}{source.LabelCap} ({source.coverage * 100:f0}%)\n";
            foreach (var child in source.parts)
            {
                result += GetPartsStringRecursive(child, indent + "  ");
            }
            return result;
        }
    }
}
