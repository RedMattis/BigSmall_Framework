using BigAndSmall.FilteredLists;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    /// <summary>
    /// Patch defs in other mods. And things related to defs.
    /// </summary>
    public class HumanLikes : Def
    {
        private static List<ThingDef> _humanlikes = null;
        public static List<ThingDef> Humanlikes => _humanlikes ??= DefDatabase<HumanLikes>.AllDefs.SelectMany(x=>x.thingList).ToList();
        public List<ThingDef> thingList = [];
    }

    public class GeneratedRecipeUser
    {
        public List<FleshTypeDef> validfleshTypes = null;
        public List<ThingDef> overrideRecipeUsers = null;
        public bool addToCorpses = false;
        public bool addToLivingThing = true;
    }

    public class RecipeExtension : DefModExtension
    {
        public PawnKindDef pawnKindDef = null;
        public GeneratedRecipeUser conditionalRecipe;

        public bool ShouldAddToRace(ThingDef thing, bool forceMechanical = false)
        {
            if (conditionalRecipe == null) return false;
            var userFleshType = thing?.race?.FleshType;
            if (userFleshType == null) return false;

            if (forceMechanical) userFleshType = FleshTypeDefOf.Mechanoid;
            var ru = conditionalRecipe;
            if (thing.IsCorpse && ru.addToCorpses == false) return false;
            if (thing.IsCorpse == false && ru.addToLivingThing == false) return false;

            if (ru?.overrideRecipeUsers != null && ru.overrideRecipeUsers.Contains(thing) == false) return false;

            if (ru.validfleshTypes.NullOrEmpty()) return true;

            if (ru.validfleshTypes.Contains(FleshTypeDefOf.Mechanoid))
            {
                // Bit hacky, but we don't want to set flesh to metal because of Ludeon hardcoding AI behaviour.
                if (thing?.race?.BloodDef == BSDefs.Filth_MachineBits || thing?.race?.IsMechanoid == true)
                {
                    return true;
                }
            }
            if (ru.validfleshTypes.Contains(userFleshType))
            {
                return true;
            }
            return false;
        }
    }

    public static class HumanPatcher
    {
        public static void PatchRecipes()
        {
            MigrateThingDefLinks();
            PatchCustomBodyPartDefs();
        }

        private static List<RecipeDef> AllHumanRecipes()
        {
            var tDef = ThingDefOf.Human;
            List<RecipeDef> allHumanRecipes = [];

            if (tDef.recipes != null)
            {
                for (int i = 0; i < tDef.recipes.Count; i++)
                {
                    allHumanRecipes.Add(tDef.recipes[i]);
                }
            }

            List<RecipeDef> allDefsListForReading = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int j = 0; j < allDefsListForReading.Count; j++)
            {
                if (allDefsListForReading[j].recipeUsers != null && allDefsListForReading[j].recipeUsers.Contains(tDef))
                {
                    allHumanRecipes.Add(allDefsListForReading[j]);
                }
            }
            return allHumanRecipes;
        }

        private static List<ThingDef> raceThingList = [];
        private static List<(ThingDef thing, List<RaceExtension> raceExts)> thingsWithRaceExtension = [];
        private static void MigrateThingDefLinks()
        {
            raceThingList = DefDatabase<ThingDef>.AllDefs.Where(x => x.race is RaceProperties rp).ToList();
            thingsWithRaceExtension = DefDatabase<ThingDef>.AllDefs
                .Where(x => x.GetRaceExtensions().Any()).Select(td => (td, td.GetRaceExtensions())).ToList();



            var humanLikes = HumanLikes.Humanlikes;


            // Migrate all recipes for stuff without a PawnExtension and Tracking Hediff instructing on what to transfer.
            List<ThingDef> defaultHumanlike = [.. thingsWithRaceExtension
                    .Where(x => x.raceExts.All(sr => sr.SurgeryRecipes.AnyItems() == false)).Select(x => x.thing)];

            defaultHumanlike = [.. defaultHumanlike, .. humanLikes];
            var humanRecipes = AllHumanRecipes();
            foreach (var thing in defaultHumanlike)
            {
                foreach (var recipe in humanRecipes.Where(x => !thing.recipes.Contains(x)))
                {
                    thing.recipes.Add(recipe);
                    //Log.Message($"Patched recipe {recipe.defName} to include {thing.defName}");
                }
            }

            var allRecipes = DefDatabase<RecipeDef>.AllDefs;

            foreach ((ThingDef thing, List<RaceExtension> raceExts) in thingsWithRaceExtension.Where(x => x.raceExts.Any(r => r.SurgeryRecipes.AnyItems())))
            {
                foreach (var raceExt in raceExts)
                {
                    var rFilter = raceExt.SurgeryRecipes;
                    if (rFilter == null) continue;
                    bool forceMechanical = raceExt.RaceHediffs.Any(x => x.GetAllPawnExtensionsOnHediff().Any(y => y.isMechanical));
                    var recipesFromHuman = humanRecipes.Where(x => rFilter.GetFilterResult(x).Accepted());
                    var recipesFromRecipeExtension = allRecipes
                        .Where(x => x.GetModExtension<RecipeExtension>()?.ShouldAddToRace(thing, forceMechanical: forceMechanical) == true);
                    if (recipesFromRecipeExtension.Any())
                    {
                        recipesFromRecipeExtension.Do(x => x.recipeUsers.Add(thing));
                    }

                    var recipesFromAll = allRecipes.Where(x => rFilter.GetFilterResult(x).ExplicitlyAllowed());
                    thing.recipes.AddRange(recipesFromHuman);
                    thing.recipes.AddRange(recipesFromRecipeExtension);
                    thing.recipes.AddRange(recipesFromAll);
                    thing.recipes = thing.recipes.Distinct().ToList();
                }
            }

            // Add recipes with complex conditions
            AddConditionalRecipes(allRecipes);

            foreach (var thing in humanLikes)
            {
                foreach (var recipe in humanRecipes.Where(x => !thing.recipes.Contains(x)))
                {
                    thing.recipes.Add(recipe);
                    //Log.Message($"Patched recipe {recipe.defName} to include {thing.defName}");
                }
            }

            foreach(var thing in raceThingList.Distinct())
            {
                if (thing.IsMechanicalDef())
                {
                    if (thing.race?.corpseDef != null)
                    {
                        Log.Message($"DEBUG: Patching {thing.defName} to allow butchering and smashing");
                        var corpse = thing.race.corpseDef;
                        //BSDefs.BS_ShredRobot.fixedIngredientFilter.SetAllow(corpse, true);
                        //BSDefs.BS_SmashRobot.fixedIngredientFilter.SetAllow(corpse, true);
                        //Log.Message($"DEBUG: Patched corse of {thing.defName}/{corpse.defName}: {BSDefs.BS_ShredRobot.fixedIngredientFilter.AllowedThingDefs.Any(x => x == corpse)}");

                        //DirectXmlCrossRefLoader.RegisterListWantsCrossRef(thing.thingCategories, thing.race.Humanlike ? ThingCategoryDefOf.CorpsesHumanlike.defName : thing.race.FleshType.corpseCategory.defName, thing);
                        //corpse.thingCategories ??= [];
                        //corpse.thingCategories.Add(BSDefs.BS_RobotCorpses);
                        DirectXmlCrossRefLoader.RegisterListWantsCrossRef(corpse.thingCategories, BSDefs.BS_RobotCorpses.defName, corpse);

                        corpse.comps.RemoveAll((CompProperties compProperties) => compProperties is CompProperties_SpawnerFilth
                            //|| compProperties?.compClass == typeof(CompHarbingerTreeConsumable)
                            || compProperties is CompProperties_Rottable);


                    }
                }
            }
        }

        private static void AddConditionalRecipes(IEnumerable<RecipeDef> allRecipes)
        {
            var raceThingListWithoutExt = raceThingList.Where(x => thingsWithRaceExtension.All(y => y.thing != x)).ToList();
            foreach (var raceThing in raceThingListWithoutExt)
            {
                bool change = false;
                foreach (var recipe in allRecipes.Where(x => !x.ExtensionsOnDef<RecipeExtension, RecipeDef>().NullOrEmpty()))
                {
                    if (recipe.ExtensionsOnDef<RecipeExtension, RecipeDef>().Any(x => x.ShouldAddToRace(raceThing)))
                    {
                        raceThing.recipes ??= [];
                        raceThing.recipes.Add(recipe);
                        change = true;
                        recipe.recipeUsers.Add(raceThing);
                        //Log.Message($"Patched recipe {recipe.defName} to include {raceThing.defName}");
                    }
                }
                if (change)
                {
                    raceThing.recipes = raceThing.recipes.Distinct().ToList();
                }
            }
        }

        public static Dictionary<BodyPartDef, List<BodyPartDef>> partImportsFromDict = [];

        // Used for GetPartsWithDef_Postfix to make parts act as the same part.
        public static Dictionary<BodyPartDef, List<BodyPartDef>> partImportsFromDictReverse = [];
        private static void PatchCustomBodyPartDefs()
        {
            var partDefs = DefDatabase<BodyPartDef>.AllDefs;
            List<(BodyPartDef part, List<BodyPartDef> sources)> partExts = partDefs.Where(x => x.modExtensions != null && x.modExtensions.Any(y => y is BodyPartExtension)).Select(bd =>
            {
                // We want to add ALL since someone might be patching in multiple of the same extension.
                HashSet<BodyPartDef> defs = [];
                foreach (var ext in bd.modExtensions.Where(x => x is BodyPartExtension).Select(bpe => bpe as BodyPartExtension))
                {
                    defs.AddRange(ext.importAllRecipesFrom);
                }
                return (bd, defs.ToList());
            }).ToList();
            var allRecipes = DefDatabase<RecipeDef>.AllDefs;

            foreach ((BodyPartDef part, IEnumerable<BodyPartDef> partsToCopyFrom) in partExts)
            {
                partImportsFromDict[part] = partsToCopyFrom.ToList();
                foreach (var recipe in allRecipes.Where(x => x.appliedOnFixedBodyParts.Any(y => partsToCopyFrom.Contains(y))))
                {
                    if (!recipe.appliedOnFixedBodyParts.Contains(part))
                    {

                        recipe.appliedOnFixedBodyParts.Add(part);
                        //Log.Message($"Patched recipe {recipe.defName} to include {part.defName}");
                    }
                }
            }
            foreach (var kvp in partImportsFromDict)
            {
                foreach (var part in kvp.Value)
                {
                    if (!partImportsFromDictReverse.TryGetValue(part, out var list))
                    {
                        list = [];
                        partImportsFromDictReverse[part] = list;
                    }
                    list.Add(kvp.Key);
                }
            }
        }
    }

    public class BodyPartExtension : DefModExtension
    {
        public List<BodyPartDef> importAllRecipesFrom = [];
        public List<BodyPartDef> mechanicalVersionOf = [];
    }

}
