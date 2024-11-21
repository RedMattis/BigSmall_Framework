using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static BigAndSmall.BodyDefFusionsHelper;
using static HarmonyLib.AccessTools;
using static UnityEngine.GraphicsBuffer;

namespace BigAndSmall
{
    public class SimilarParts : Def
    {
        public string groupName;
        /// <summary>
        /// Avoid very low values unless you don't want them to merge.
        /// </summary>
        public float similarity = 1;
        protected List<string> parts = [];

        private List<BodyPartDef> _partsCache = null;
        public List<BodyPartDef> Parts => _partsCache ??= parts.Select(x => DefDatabase<BodyPartDef>.GetNamed(x, errorOnFail: false)).ToList();
    }

    public class MergableBody
    {
        public BodyDef bodyDef;
        public ThingDef thingDef;

        [NoTranslate]
        public string overrideDefNamer = null;
        public string prefixLabel = null;
        public string suffixLabel = null;
        private bool fuse = true;
        public bool fuseAll = false;
        public bool fuseSet = false;
        public bool isMechanical = false;
        public bool defaultMechanical = false;
        public bool canBeFusionOne = true;
        public bool canMakeRobotVersion = true;
        public List<string> exclusionTags = [];



        public List<SimilarParts> removesParts = [];
        /// <summary>
        /// Which order this will be merged in. Put weird stuff with a higher priority.
        /// 
        /// It is likely better that weird bodies are bodyOne so that a snake-hybrid starts with a snake body rather than trying to replace the legs.
        /// </summary>
        public float priority = 0;

        public bool Fuse { get => fuse && !fuseSet; }// && !fuseAll; }

        public bool ShouldRemovePart(BodyPartDef part)
        {
            foreach (var partSet in removesParts)
            {
                if (partSet.Parts.Contains(part))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Substitutions
    {
        public List<BodyDef> bodyDefs = [];
        public BodyDef target = null;  // If this is null we simply remove the requirement.
    }

    public class BodyDefFusion : Def
    {
        public List<MergableBody> mergableBody = [];
        public List<Substitutions> substitutions = [];
        public List<SimilarParts> similarParts = [];
        public List<BodyPartDef> bodyPartToSkip = [];
    }

    public static class BodyDefFusionsHelper
    {
        private static List<BodyDefFusion> instances = null;
        public static List<BodyDefFusion> Instances => instances ??= DefDatabase<BodyDefFusion>.AllDefs.ToList();
        public static List<MergableBody> MergableBodies => Instances.SelectMany(x => x.mergableBody).ToList();

        public static List<MergableBody> MergabeWithSetBodies = MergableBodies.Where(x => x.fuseSet).ToList();
        public static List<SimilarParts> PartSets => Instances.SelectMany(x => x.similarParts).ToList();
        public static List<BodyPartDef> PartsToSkip => Instances.SelectMany(x => x.bodyPartToSkip).ToList();
        public static List<Substitutions> Substitutions => Instances.SelectMany(x => x.substitutions).ToList();

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
    

    public class FusedBody
    {
        public readonly static Dictionary<string, FusedBody> FusedBodies = [];
        public BodyDef generatedBody = null;
        public MergableBody[] mergableBodies = null;
        public ThingDef thing = null;
        public MergableBody fuseSetBody = null;
        public bool fake = false;
        public bool isMechanical = false;

        public FusedBody(BodyDef generatedBody, MergableBody fusetSetBody, bool mechanical, params MergableBody[] mergableBodies)
        {
            this.isMechanical = mechanical;
            this.generatedBody = generatedBody;
            this.mergableBodies = mergableBodies;
            this.fuseSetBody = fusetSetBody;
            FusedBodies[GetKey(mechanical, mergableBodies.Select(x=>x.bodyDef).ToArray())] = this;
        }

        public MergableBody SourceBody => mergableBodies[0];

        private static string GetKey(bool mechanical, BodyDef[] bodyDefs)
        {
            var key = string.Join("|", bodyDefs.OrderBy(x => x.defName));
            //Log.Message($"{key} Generated from {mechanical} and {bodyDefs.Select(x => x.defName).ToCommaList()}");
            return string.Join("|", bodyDefs.OrderBy(x => x.defName));
        }

        public static FusedBody TryGetBody(bool mechanical, params BodyDef[] bodyDefs)
        {
            string mString = mechanical ? "mechanical" : "biological";
            if (false) Log.Message($"[Initial]: Fetching {mString} for and {string.Join(", ", bodyDefs.Select(x => x.defName))}");
            if (FusedBodies.TryGetValue(GetKey(mechanical, bodyDefs), out var body)) return body;
            if (bodyDefs.Count() > 1)
            {
                // Try substitute only first.
                //Log.Message($"[No_Match]: Trying substite of primary {bodyDefs[0].defName}");
                if (FusedBodies.TryGetValue(GetKey(mechanical, [GetSubstituted(bodyDefs).First(), .. bodyDefs.Skip(1)]), out var body2)) return body2;
                // Try substitute other.
                //Log.Message($"[No_Match]: Trying substite of secondaries {string.Join(", ", bodyDefs.Skip(1).Select(x => x.defName))}");
                if (FusedBodies.TryGetValue(GetKey(mechanical, [GetSubstituted(bodyDefs).First(), .. GetSubstituted([.. bodyDefs.Skip(1)])]), out var body3)) return body3;
                // Try substitute all.
            }
            //Log.Message($"[No_Match]: Trying substite of all {string.Join(", ", bodyDefs.Select(x => x.defName))}");
            return FusedBodies.TryGetValue(GetKey(mechanical, [.. GetSubstituted(bodyDefs)]), out var body4) ? body4 : null;
        }

        private static List<BodyDef> GetSubstituted(BodyDef[] bodyDefs)
        {
            var substitutedBodies = bodyDefs.ToList();
            var allSubs = BodyDefFusionsHelper.Substitutions;
            foreach (var inBody in bodyDefs)
            {
                var sub = allSubs.FirstOrDefault(x => x.bodyDefs.Contains(inBody));
                if (sub != null)
                {
                    substitutedBodies.Remove(inBody);
                    if (sub.target != null) substitutedBodies.Add(sub.target);
                }
            }
            return substitutedBodies;
        }

        public static BodyDef TryGetNonFused(params BodyDef[] bodyDefs)
        {
            if (GetSubstituted(bodyDefs).Count == 1)
            {
                return GetSubstituted(bodyDefs).First();
            }
            return null;
        }

        public static bool HasKey(bool mechanical, params BodyDef[] bodyDefs)
        {
            return FusedBodies.ContainsKey(GetKey(mechanical, bodyDefs));
        }
    }

    public static class RaceFuser
    {
        public const string MESH_DEF = "Mech";
        public const string MESH_LABEL = "BS_Mech";
        public static bool doDebug = false;
        /// <summary>
        /// Merge body parts.
        /// </summary>
        public static void CreateMergedBodyTypes(bool hotReload)
        {
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

            if (hotReload)
            {
                foreach (var vb in FusedBody.FusedBodies.Values.Where(x => !x.fake))
                {
                    DefGenerator.AddImpliedDef(vb.generatedBody, hotReload: true);
                    DefGenerator.AddImpliedDef(vb.thing, hotReload: true);
                    //DefGenerator.AddImpliedDef(vb.thing.race.body, hotReload: true);
                }
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

        private static void GenerateAndRegisterRaceDefs(bool hotReload)
        {
            // Generate ThingDefs for all BodyDets as long as they don't have "discardNonRobotFusions"
            foreach (var fusedBody in FusedBody.FusedBodies.Values)
            //.Select(x => (x.generatedBody, x.SourceBody, x)))
            {
                var body = fusedBody.generatedBody;
                var source = fusedBody.SourceBody;
                var fSetBody = fusedBody.fuseSetBody;
                body.generated = true;
                body.ResolveReferences();
                if (doDebug) Log.Message(GetPartsStringRecursive(body.corePart));

                var sThing = source.thingDef;
                var sRace = sThing.race;
                var fSetRace = fSetBody != null ? fSetBody.thingDef.race : sRace;

                // Make the new Thing using reflection, in case it is a subclass. (Har Har Har... HAR!)
                var newThing = sThing.GetType().GetConstructor([]).Invoke([]) as ThingDef;
                var newRace = new RaceProperties();

                var allThingDefSources = fusedBody.mergableBodies.Select(x => x.thingDef).ToList();
                var raceExtensions = allThingDefSources.SelectMany(x => x.ExtensionsOnDef<RaceExtension, ThingDef>()).ToList();

                var newRaceExtension = new RaceExtension(raceExtensions)
                {
                    isFusionOf = fusedBody.mergableBodies.Select(x => x.thingDef).ToList()
                };

                // Use reflection to copy all fields from the source ThingDef to the new ThingDef.
                foreach (var field in sThing.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                {
                    try
                    {
                        field.SetValue(newThing, field.GetValue(sThing));
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to copy field {field.Name} from {sThing.defName} to {newThing.defName}.");
                        Log.Error(e.ToString());
                    }
                }
                // So we don't append to the original lists...
                newThing.modExtensions = sThing.modExtensions != null ? [.. sThing.modExtensions] : [];
                newThing.comps = sThing.comps != null ? [.. sThing.comps] : null;

                // Same for Race
                foreach (var field in fSetRace.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                {
                    try
                    {
                        field.SetValue(newRace, field.GetValue(sRace));
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to copy field {field.Name} from race.");
                        Log.Error(e.ToString());
                    }
                }
                // Set the body of the base race rather than that of the Fuse Set.
                newRace.body = sRace.body;
                newRace.renderTree = sRace.renderTree;
                newRace.corpseDef = sRace.corpseDef;

                bool makeCorpse = newRace.hasCorpse && newRace.linkedCorpseKind == null;
                if (makeCorpse)
                {
                    // And Corpse
                    var newCorpse = newRace.corpseDef.GetType().GetConstructor([]).Invoke([]) as ThingDef;
                    foreach (var field in sThing.race.corpseDef.GetType().GetFields().Where(x => !x.IsLiteral && !x.IsStatic))
                    {
                        try
                        {
                            field.SetValue(newCorpse, field.GetValue(newRace.corpseDef));
                        }
                        catch (Exception e)
                        {
                            Log.Error($"Failed to copy field {field.Name} from corpseDef.");
                            Log.Error(e.ToString());
                        }
                    }
                    newRace.corpseDef = newCorpse;
                    newCorpse.defName = $"{body.defName}_Corpse";
                    newCorpse.label = $"Corpse of {body.LabelCap}";
                    newCorpse.race = newRace;
                    //newCorpse.modExtensions ??= [];
                    //newCorpse.modExtensions.RemoveAll(x => x is RaceExtension);
                    //newCorpse.modExtensions.Add(newRaceExtension);
                }


                // Lets not generate a bunch of unnatural corpses.
                newRace.hasUnnaturalCorpse = false;

                newThing.generated = true;
                newThing.defName = $"{body.defName}";
                newThing.label = body.LabelCap;
                newThing.race = newRace;
                newRace.body = body;

                newThing.modExtensions ??= [];
                newThing.modExtensions.RemoveAll(x => x is RaceExtension);
                newThing.modExtensions.Add(newRaceExtension);

                if (fusedBody.isMechanical)
                {
                }
                if (fusedBody.fuseSetBody is MergableBody fSBody)
                {
                    var fsThing = fSBody.thingDef;
                    var fsRace = fsThing.race;
                    newThing.statBases = [.. fsThing.statBases];
                    foreach(var statBase in fsThing.statBases)
                    {
                        newThing.SetStatBaseValue(statBase.stat, statBase.value);
                    }
                    newThing.butcherProducts = [.. fsThing.butcherProducts];
                    newThing.ingredient = fsThing.ingredient;
                    // Averaged
                    newThing.SetStatBaseValue(StatDefOf.PsychicSensitivity, (sThing.GetStatValueAbstract(StatDefOf.PsychicSensitivity) + fsThing.GetStatValueAbstract(StatDefOf.PsychicSensitivity)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.DeepDrillingSpeed, (sThing.GetStatValueAbstract(StatDefOf.DeepDrillingSpeed) + fsThing.GetStatValueAbstract(StatDefOf.DeepDrillingSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.MiningSpeed, (sThing.GetStatValueAbstract(StatDefOf.MiningSpeed) + fsThing.GetStatValueAbstract(StatDefOf.MiningSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.MiningYield, (sThing.GetStatValueAbstract(StatDefOf.MiningYield) + fsThing.GetStatValueAbstract(StatDefOf.MiningYield)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.ConstructionSpeed, (sThing.GetStatValueAbstract(StatDefOf.ConstructionSpeed) + fsThing.GetStatValueAbstract(StatDefOf.ConstructionSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.SmoothingSpeed, (sThing.GetStatValueAbstract(StatDefOf.SmoothingSpeed) + fsThing.GetStatValueAbstract(StatDefOf.SmoothingSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.PlantHarvestYield, (sThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield) + fsThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.PlantWorkSpeed, (sThing.GetStatValueAbstract(StatDefOf.PlantWorkSpeed) + fsThing.GetStatValueAbstract(StatDefOf.PlantWorkSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.PlantHarvestYield, (sThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield) + fsThing.GetStatValueAbstract(StatDefOf.PlantHarvestYield)) / 2);

                    newThing.SetStatBaseValue(StatDefOf.MoveSpeed, (sThing.GetStatValueAbstract(StatDefOf.MoveSpeed) + fsThing.GetStatValueAbstract(StatDefOf.MoveSpeed)) / 2);
                    newThing.SetStatBaseValue(StatDefOf.IncomingDamageFactor, (sThing.GetStatValueAbstract(StatDefOf.IncomingDamageFactor) + fsThing.GetStatValueAbstract(StatDefOf.IncomingDamageFactor)) / 2);

                    // Averaged Ceil
                    newThing.SetStatBaseValue(StatDefOf.PawnBeauty, Mathf.Ceil((sThing.GetStatValueAbstract(StatDefOf.PawnBeauty) + fsThing.GetStatValueAbstract(StatDefOf.PawnBeauty)) / 2));

                    // Max
                    newThing.SetStatBaseValue(StatDefOf.MarketValue, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.MarketValue), fsThing.GetStatValueAbstract(StatDefOf.MarketValue)));
                    newThing.SetStatBaseValue(StatDefOf.Nutrition, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.Nutrition), fsThing.GetStatValueAbstract(StatDefOf.Nutrition)));
                    newThing.SetStatBaseValue(StatDefOf.Mass, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.Mass), fsThing.GetStatValueAbstract(StatDefOf.Mass)));
                    newThing.SetStatBaseValue(StatDefOf.ToxicResistance, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.ToxicResistance), fsThing.GetStatValueAbstract(StatDefOf.ToxicResistance)));
                    newThing.SetStatBaseValue(StatDefOf.ToxicEnvironmentResistance, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.ToxicEnvironmentResistance), fsThing.GetStatValueAbstract(StatDefOf.ToxicEnvironmentResistance)));
                    newThing.SetStatBaseValue(StatDefOf.MeleeDodgeChance, Mathf.Max(sThing.GetStatValueAbstract(StatDefOf.MeleeDodgeChance), fsThing.GetStatValueAbstract(StatDefOf.MeleeDodgeChance)));



                }

                // Copy over the recipes, styles, and categories, ...
                newThing.recipes = [.. allThingDefSources.Where(x => x.recipes != null).SelectMany(x => x?.recipes).Where(x => x != null).ToList().Distinct()];
                newThing.thingCategories = [.. allThingDefSources.Where(x => x.thingCategories != null).SelectMany(x => x?.thingCategories).Where(x => x != null).ToList().Distinct()];

                // Hmm... doesn't seem worth the mess.
                //newThing.tools = [.. allThingDefSources.Where(x => x.tools != null).SelectMany(x => x?.tools).Where(x => x != null).ToList().Distinct()];

                fusedBody.thing = newThing;

                // Add the things to the game.
                DefGenerator.AddImpliedDef(newThing, hotReload: hotReload);
                DefGenerator.AddImpliedDef(newRace.body, hotReload: hotReload);
                //DefDatabase<ThingDef>.Add(newThing);
                //DefDatabase<BodyDef>.Add(body);

                // Make sure we generate new Hashes for the new ThingDefs.
                GenerateShortHashes(hotReload, newThing, newRace);
                //if (makeCorpse)
                //{
                //    ShortHashWrapper.GiveShortHash(newThing.race.corpseDef);
                //}
            }
        }

        private static void GenerateShortHashes(bool hotReload, ThingDef newThing, RaceProperties newRace)
        {
            if (!hotReload)
            {
                newThing.shortHash = 0;
                ShortHashWrapper.GiveShortHash(newRace.body);
                ShortHashWrapper.GiveShortHash(newThing);
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
                foreach (var baseMergable in bodyDefsToMerge.Where(x => fusedSetBody.isMechanical == false || x.canMakeRobotVersion))
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
                        new FusedBody(genBody, fusetSetBody:null, mechanical: bodyOne.isMechanical, [bodyOne, .. secondaryBodies]);
                    }
                }
            }
        }

        internal static class ShortHashWrapper
        {
            private static Action<Def, Type, HashSet<ushort>> giveHashDelegate;
            private static FieldRef<Dictionary<Type, HashSet<ushort>>> takenHashesFieldRef;

            static ShortHashWrapper()
            {
                giveHashDelegate = MethodDelegate<Action<Def, Type, HashSet<ushort>>>(Method(typeof(ShortHashGiver), "GiveShortHash",
                [ typeof(Def), typeof(Type), typeof(HashSet<ushort>) ], null), null, true);
                takenHashesFieldRef = StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(Field(typeof(ShortHashGiver), "takenHashesPerDeftype"));
            }
            internal static void GiveShortHash<T>(T def) where T : Def
            {
                Dictionary<Type, HashSet<ushort>> dictionary = takenHashesFieldRef.Invoke();
                if (!dictionary.ContainsKey(typeof(T)))
                {
                    dictionary[typeof(T)] = [];
                }
                HashSet<ushort> arg = dictionary[typeof(T)];
                giveHashDelegate(def, null, arg);
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

            var genBody = new BodyDef
            {
                defName = defNameFull,
                description = bodyOne.bodyDef.description,
                label = fullLabel // 
            };
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


        private static Dictionary<BodyPartDef, List<BodyPartDef>> _mechanicalVersionOf = null;

        private static Dictionary<BodyPartDef, List<BodyPartDef>> GetMechanicalVersionsOf()
        {
            if (_mechanicalVersionOf != null) return _mechanicalVersionOf;
            _mechanicalVersionOf = new Dictionary<BodyPartDef, List<BodyPartDef>>();

            List<(BodyPartDef, BodyPartExtension)> parts = [];
            foreach (var def in DefDatabase<BodyPartDef>.AllDefsListForReading)
            {
                var extensions = def.ExtensionsOnDef<BodyPartExtension, BodyPartDef>();
                if (extensions != null)
                {
                    foreach (var extension in extensions)
                    {
                        if (extension.mechanicalVersionOf != null && extension.mechanicalVersionOf.Any())
                        {
                            parts.Add((def, extension));
                            break;
                        }
                    }
                }
            }

            foreach (var part in parts)
            {
                foreach (var mechanicalPart in part.Item2.mechanicalVersionOf)
                {
                    if (!_mechanicalVersionOf.ContainsKey(mechanicalPart))
                    {
                        _mechanicalVersionOf[mechanicalPart] = new List<BodyPartDef>();
                    }
                    _mechanicalVersionOf[mechanicalPart].Add(part.Item1);
                }
            }
            return _mechanicalVersionOf;
        }


        private static BodyPartRecord ClonePartsRecursive(BodyPartRecord genPartParent, BodyPartRecord source, BodyDef genBody, MergableBody bodyOne, List<BodyPartRecord> unTransfereredParts, bool makeMechanical)
        {
            if (bodyOne.ShouldRemovePart(source.def))
            {
                //Log.Message($"Skipping {source.def.defName} as it should be removed.");
                return null;
            }

            var partDef = source.def;
            string customLabel = source.customLabel;
            if (makeMechanical && GetMechanicalVersionsOf() is Dictionary<BodyPartDef, List<BodyPartDef>> mechVersionList &&
                mechVersionList.TryGetValue(source.def, out var mechVersions) && !mechVersions.NullOrEmpty())
            {
                partDef = mechVersions.First();
                customLabel = customLabel ?? (source.def.IsMirroredPart ? (source.flipGraphic  ? "BS_Left".Translate() : "BS_Right".Translate() + " " + partDef.label) : partDef.label);
            }

            var nGenPart = new BodyPartRecord
            {
                body = genBody,
                parent = genPartParent,
                def = partDef,
                customLabel = customLabel,
                coverage = source.coverage,
                depth = source.depth,
                height = source.height,
                woundAnchorTag = source.woundAnchorTag,
                flipGraphic = source.flipGraphic,
                groups = source.groups == null ? null : [.. source.groups],
                visibleHediffRots = source.visibleHediffRots == null ? null : [.. source.visibleHediffRots],
            };
            //if (doDebug) Log.Message($"{genPartParent?.LabelCap}->{nGenPart?.LabelCap}, " +
            //            $"({nGenPart.coverage * 100:f0}%)");

            if (unTransfereredParts.Contains(source)) unTransfereredParts.Remove(source);
            if (genPartParent == null)
            {
                genBody.corePart = nGenPart;
            }
            else
            {
                genPartParent.parts.Add(nGenPart);
            }
            foreach (var child in source.parts)
            {
                ClonePartsRecursive(nGenPart, child, genBody, bodyOne, unTransfereredParts, makeMechanical);
            }
            return nGenPart;
        }

        private static float? Similarity(BodyPartRecord partOne, BodyPartRecord partTwo)
        {
            float similarity = 0;
            if (partOne.def == partTwo.def)
            {
                similarity += 1000000;
            }
            else if (Equavalence(partOne.def, partTwo.def) is float simMult)
            {
                similarity += 1000000 * simMult;
            }
            else return null; // Parts are not similar.
            //if (ImportsRecipesFromOrSame(partOne.def, partTwo.def))
            //{
            //    similarity += 20000;
            //}
            if (partOne.groups == partTwo.groups)
            {
                similarity += 10000;
            }
            if (partOne.flipGraphic == partTwo.flipGraphic)
            {
                similarity += 1000;
            }
            if (partOne.height == partTwo.height)
            {
                similarity += 100;
            }
            if (partOne.customLabel == partTwo.customLabel)
            {
                similarity += 10;
            }
            if (partOne.coverage == partTwo.coverage)
            {
                similarity += 1;
            }
            if (partOne.customLabel?.Split(' ').Intersect(partTwo.customLabel?.Split(' ')).Any() == true)
            {
                similarity += 0.5f;
            }
            if (partOne.depth == partTwo.depth)
            {
                similarity += 0.1f;
            }
            if (partOne.woundAnchorTag == partTwo.woundAnchorTag)
            {
                similarity += 0.05f;
            }
            if (partOne.visibleHediffRots == partTwo.visibleHediffRots)
            {
                similarity += 0.01f;
            }
            return similarity;
        }
        private static void MergeRecursively(BodyPartRecord genPart, BodyPartRecord partTwo, List<BodyPartRecord> unTransfereredParts, MergableBody mergeOne)
        {
            var partTwoParts = partTwo.parts.Where(x=>!(mergeOne.ShouldRemovePart(x.def))).ToList();
            genPart.parts = genPart.parts.Where(x=>!(mergeOne.ShouldRemovePart(x.def))).ToList();

            foreach (var child in genPart.parts)
            {
                var similarPart = partTwoParts
                    .Where(x => Similarity(x, child) != null)?
                    .Select(x => (x, Similarity(x, child)))?
                    .OrderByDescending(x => x.Item2).FirstOrDefault();

                if (similarPart.HasValue && similarPart.Value.x != null)
                {
                    var similarBodyRec = similarPart.Value.x;
                    MergeRecursively(child, similarBodyRec, unTransfereredParts, mergeOne);
                    unTransfereredParts.Remove(similarBodyRec);
                    partTwoParts.Remove(similarBodyRec);
                }
                else
                {
                    //Log.Message($"DEBUG: Could not find a similar part for {child.def.defName} in {partTwo.def.defName}");
                }
            }

            // Total Coverage of the body parts.
            float genTotalCoverage = genPart.parts.Any() ? genPart.parts.Sum(x => x.coverage) : 0;
            float partTwoCoverage = partTwoParts.Any() ? partTwoParts.Sum(x => x.coverage) : 0;
            float coverageMultiplier = 1;
            if (genTotalCoverage != 0)
            {
                coverageMultiplier = genTotalCoverage / (genTotalCoverage + partTwoCoverage);
                foreach (var part in genPart.parts)
                {
                    part.coverage *= coverageMultiplier;
                }
            }

            //if (partTwoParts.Any())
            //{
            //    Log.Message($"There are {partTwoParts.Count} parts left to merge into {genPart.def.defName}.");
            //}

            foreach (var part in partTwoParts)
            {
                if (unTransfereredParts.Contains(part))
                {
                    unTransfereredParts.Remove(part);

                    if (PartsToSkip.Contains(part.def))
                    {
                        continue;
                    }

                    //Log.Message($"\nAdding From SECOND Body");

                    //Log.Message($"Adding {genPart.LabelCap}->{part.LabelCap}");
                    var newPart = ClonePartsRecursive(genPart, part, genPart.body, mergeOne, unTransfereredParts, makeMechanical:false);
                    newPart.coverage *= coverageMultiplier;
                    newPart.parent = genPart;
                }
                //else
                //{
                //    Log.Message($"Part {part.LabelCap} was already transferred. Skipped.");
                //}
            }
        }
    }
}
