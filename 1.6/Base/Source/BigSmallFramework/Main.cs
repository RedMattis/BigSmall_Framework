
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
using BigAndSmall.Settings;
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

			ModFeatures.ParseEnabledFeatures();

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

			LongEventHandler.ExecuteWhenFinished(ModFeatures.ProcessConditionalFeatureDefs);
			RenderNodePatcher.TryPatchPawnRenderNodeDefs();
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
    

}
