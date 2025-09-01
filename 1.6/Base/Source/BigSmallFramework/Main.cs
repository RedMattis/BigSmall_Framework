﻿
using BigAndSmall.FilteredLists;
using BigAndSmall.SimpleCustomRaces;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using Verse;
using BigAndSmall.Utilities;
//using VariedBodySizes;

namespace BigAndSmall
{
    [StaticConstructorOnStartup]
    public static class BSCore
    {
		private static readonly Type patchType;
        public static Harmony harmony = new("RedMattis.BetterPrerequisites");
        static BSCore()
		{
			DebugLog.Message("Harmony patching.");

			patchType = typeof(BSCore);
			harmony.PatchAll();

			PregnancyPatches.ApplyPatches();
			//RunDefPatchesWithHotReload(hotReload: false);

			if (NalsToggles.FALoaded)
			{
				NalsToggles.ApplyNLPatches(harmony);
			}

			DebugLog.Message("Finished harmony patching.");
		}

        public static void RunBeforeGenerateImpliedDefs(bool hotReload)
		{
			DebugLog.Message("Generate Defs.");

			if (!hotReload)
            {
                GlobalSettings.Initialize();
            }
            DefAltNamer.Initialize();
            HARCompat.SetupHARThingsIfHARIsActive();
            NewFoodCategory.SetupFoodCategories();
            HumanPatcher.MechanicalSetup();
            RaceFuser.PreHotreload();
            RaceFuser.CreateMergedBodyTypes(hotReload);
            HumanlikeAnimalGenerator.GenerateHumanlikeAnimals(hotReload);
            FlagStringData.Setup(force: true); // For hotreload.
            DebugLog.Message("Generate defs complete.");
		}

        public static void RunDuringGenerateImpliedDefs(bool hotReload)
		{
			DebugLog.Message("Patch defs.");

			GeneDefPatcher.PatchExistingDefs();
            RaceFuser.GenerateCorpses(hotReload);
            if (!hotReload) HumanPatcher.MechanicalCorpseSetup();
            XenotypeDefPatcher.PatchDefs();
            ModDefPatcher.PatchDefs();
            HumanPatcher.PatchRecipes();
            ThoughtDefPatcher.PatchDefs();
            if (BigSmallMod.settings.experimental)
            {
				// Put the animal stuff here maybe?
			}

			DebugLog.Message("Patch defs finished.");
		}

		public static void RunAfterGenerateImpliedDefs(bool hotReload)
		{
			// Replace sapient animal corpses thing category.
			foreach (ThingDef sapientAnimal in HumanlikeAnimalGenerator.humanlikeAnimals.Keys)
			{
				ThingDef corpse = sapientAnimal.race.corpseDef;
				if (corpse != null)
				{
					corpse.thingCategories.Clear();
					corpse.thingCategories.Add(BSDefs.BS_CorpsesHumanlikeAnimals);
				}
			}

			// Replace hybrid corpses thing category.
			foreach (ThingDef hybridDef in FusedBody.FusedBodyByThing.Keys)
			{
				ThingDef corpse = hybridDef.race.corpseDef;
				if (corpse != null)
				{
					corpse.thingCategories.Clear();
					corpse.thingCategories.Add(BSDefs.BS_CorpsesHumanlikeHybrids);
				}
			}
        }
    }

    public class DefAltNamer : Def
    {
        public List<RenameGene> geneRenames = [];
        private static Dictionary<GeneDef, RenameGene> allGeneRenames = [];
        public abstract class Rename
        {
            public string labelMechanoid = null;
            public string labelBloodfeeder = null;
            public string labelFantasy = null;
        }
        public class RenameGene : Rename { public GeneDef def = null; }

        public static Dictionary<GeneDef, RenameGene> AllGeneRenames => allGeneRenames ??= SetupDict();

        public static void Initialize()
        {
            allGeneRenames = SetupDict();
        }

        public static Dictionary<GeneDef, RenameGene> SetupDict()
        {
            var dansInDataBase = DefDatabase<DefAltNamer>.AllDefs;
            if (!dansInDataBase.Any())
            {
                return [];
            }
            var items = DefDatabase<DefAltNamer>.AllDefs
                .SelectMany(x => x.geneRenames
                    .Select(y => (y?.def, y)));

            // Remove any null items.
            items = items.Where(x => x.y != null && x.def != null);

            return items.ToDictionary(x => x.def, x => x.y);
        }
    }
    public class InfiltratorData
    {
        public FilterListSet<FactionDef> factionFilter = null;
        public List<XenotypeChance> doubleXenotypes = [];
        public FilterListSet<XenotypeDef> xenoFilter = null;
        public FilterListSet<ThingDef> thingFilter = null;
        public bool canFactionSwap = true;
        public bool canSwapXeno = false;
        public bool disguised = false;
        public FactionDef ideologyOf = null;
        public bool canBeFullRaid = false;
        public bool canOnlyBeFullRaid = false;
        public float? chanceOverride = null;
        public float TotalChance => chanceOverride ?? doubleXenotypes.Sum(x => x.chance);
    }

    public class GlobalSettings : Def
    {
        public static Dictionary<string, GlobalSettings> globalSettings = DefDatabase<GlobalSettings>.AllDefs.ToDictionary(x => x.defName);

        public List<string> enabledFeatures = [];
        
        public List<List<string>> alienGeneGroups = [];
        public List<XenotypeChance> returnedXenotypes = [];
        public List<XenotypeChance> returnedXenotypesColonist = [];
        public List<InfiltratorData> infiltratorTypes = [];

        [Unsaved(false)]
        private static List<List<GeneDef>> alienGeneGroupsDefs = null;

        public static XenotypeDef GetRandomReturnedXenotype => globalSettings
            .SelectMany(x => x.Value.returnedXenotypes)
            .TryRandomElementByWeight(x => x.chance, out var result) ? result.xenotype : null;

        public static XenotypeDef GetRandomReturnedColonistXenotype => globalSettings
            .SelectMany(x => x.Value.returnedXenotypesColonist)
            .TryRandomElementByWeight(x => x.chance, out var result) ? result.xenotype : null;

        public static bool IsFeatureEnabled(string featureName) => globalSettings.Values
            .Any(x => x.enabledFeatures.Contains(featureName, StringComparer.OrdinalIgnoreCase));


        public static void Initialize()
        {
            globalSettings = DefDatabase<GlobalSettings>.AllDefs.ToDictionary(x => x.defName);
        }

        public static (XenotypeDef def, InfiltratorData data) GetRandomInfiltratorReplacementXenotype(Pawn pawn, int seed, bool forceNeeded, bool isFullRaid)
        {
            List<InfiltratorData> allValidInfiltratorData = globalSettings.Values.SelectMany(x => x.infiltratorTypes).ToList();
            if (pawn.Faction != null)
            {
                allValidInfiltratorData = [.. allValidInfiltratorData.Where(x =>
                    x.doubleXenotypes.Any() &&
                    (!x.canOnlyBeFullRaid || (x.canOnlyBeFullRaid && isFullRaid)) &&
                    (!isFullRaid || x.canBeFullRaid) &&
                    (!forceNeeded || x.canSwapXeno) &&
                    (x.factionFilter == null || x.factionFilter.GetFilterResult(pawn.Faction.def).Accepted()) &&
                    (x.thingFilter == null || x.thingFilter.GetFilterResult(pawn.def).Accepted()) &&
                    (x.xenoFilter == null || (pawn.genes?.Xenotype is XenotypeDef pXDef && x.xenoFilter.GetFilterResult(pXDef).Accepted()))
                    )];
            }
            if (allValidInfiltratorData.Count == 0 || allValidInfiltratorData.All(x=>x.doubleXenotypes?.Count == 0)) return (null, null);
            // Return xenotype based on chance.

            InfiltratorData data;
            // Ensure we're getting infiltrators from the same "group" if doing full infiltrator raid.
            // Mostly to avoid stupid results like succubi mixed with synths.
            using (new RandBlock(seed)) 
            {
                data = allValidInfiltratorData.RandomElementByWeight(x => x.TotalChance);
            }
            XenotypeDef resultXeno = allValidInfiltratorData.SelectMany(x => x.doubleXenotypes).ToList().RandomElementByWeight(x => x.chance).xenotype;

            return (resultXeno, allValidInfiltratorData.First(x => x.doubleXenotypes.Any(y => y.xenotype == resultXeno)));
        }

        public static List<List<GeneDef>> GetAlienGeneGroups()
        {
            if (alienGeneGroupsDefs == null)
            {
                alienGeneGroupsDefs = [];
                foreach (var settings in globalSettings.Values.Where(x=>x.alienGeneGroups != null))
                {
                    foreach (var group in settings.alienGeneGroups)
                    {
                        if (group.NullOrEmpty())
                        {
                            continue;
                        }
                        var geneGroup = new List<GeneDef>();
                        foreach (var geneDef in group.Select(x=> DefDatabase<GeneDef>.GetNamed(x, false)))
                        {
                            if (geneDef != null)
                            {
                                geneGroup.Add(geneDef);
                            }
                        }
                        if (geneGroup.Count > 0)
                        {
                            alienGeneGroupsDefs.Add(geneGroup);
                        }
                    }
                }
            }
            return alienGeneGroupsDefs;
        }
    }
}
