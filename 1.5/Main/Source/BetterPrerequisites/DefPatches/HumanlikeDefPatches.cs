using BigAndSmall.FilteredLists;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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
        public bool? isSurgery = null;
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

        //public static void UnpatchHARFromNonHAR()
        //{
        //    DefDatabase<ThingDef>.AllDefs.Where(x => !x.GetType().Name.Contains("ThingDef_AlienRace") && x.race != null).Do(x =>
        //    {
        //        Log.Message($"[Big and Small] Checking for unpatching need in {x.defName}");
        //        foreach (var comp in x.comps)
        //        {
        //            Log.Message($"[Big and Small] Checking for unpatching need in {x.defName} - {comp.GetType().Name}");
        //        }
        //        var removedCount = x.comps.RemoveAll((CompProperties compProperties) => compProperties.GetType().Name.Contains("AlienPartGenerator"));
        //        if (removedCount > 0)
        //        {
        //            Log.Message($"[Big and Small] Removed {removedCount} AlienPartGenerators from {x.defName}");
        //        }
        //        Log.Clear();
        //    });
        //}

        private static List<RecipeDef> AllHumanRecipes()
        {
            var tDef = ThingDefOf.Human;
            List<RecipeDef> allHumanRecipes = [];

            if (tDef.recipes != null)
            {
                for (int i = 0; i < tDef.recipes.Count; i++)
                {
                    allHumanRecipes.AddDistinct(tDef.recipes[i]);
                }
            }

            List<RecipeDef> allDefsListForReading = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int j = 0; j < allDefsListForReading.Count; j++)
            {
                if (allDefsListForReading[j].recipeUsers != null && allDefsListForReading[j].recipeUsers.Contains(tDef))
                {
                    allHumanRecipes.AddDistinct(allDefsListForReading[j]);
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
                    thing.recipes ??= [];
                    thing.recipes.AddDistinct(recipe);
                }
            }

            var allRecipes = DefDatabase<RecipeDef>.AllDefs;
            foreach (var recipe in allRecipes)
            {
                var recipeExtensions = recipe.ExtensionsOnDef<RecipeExtension, RecipeDef>();
                if (recipeExtensions.Any(x => x.isSurgery == true))
                {
                    recipe.isSurgeryCached = true;
                }
            }

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
                        foreach(var recipe in recipesFromRecipeExtension)
                        {
                            thing.recipes ??= [];
                            thing.recipes.AddDistinct(recipe);
                            //recipe.recipeUsers ??= [];
                            //recipe.recipeUsers.AddDistinct(thing);
                            recipe.isSurgeryCached = true;
                        }
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
            foreach (var recipe in humanRecipes)
            {
                foreach (var thing in humanLikes.Where(x=>!x.recipes.Contains(recipe)))
                {
                    thing.recipes ??= [];
                    thing.recipes.AddDistinct(recipe);
                }
            }

            foreach (var thing in raceThingList.Distinct())
            {
                if (thing.IsMechanicalDef())
                {
                    if (thing.race?.corpseDef != null)
                    {
                        var corpse = thing.race.corpseDef;
                        BSDefs.BS_SmashRobot.fixedIngredientFilter.SetAllow(corpse,true);
                        BSDefs.BS_ShredRobot.fixedIngredientFilter.SetAllow(corpse, true);
                    }
                }
            }
            //BSDefs.BS_SmashRobot.fixedIngredientFilter.ResolveReferences();
            //BSDefs.BS_ShredRobot.fixedIngredientFilter.ResolveReferences();
        }

        public static void MechanicalSetup()
        {
            //raceThingList = DefDatabase<ThingDef>.AllDefs.Where(x => x.race is RaceProperties rp).ToList();
            //foreach (var thing in raceThingList.Distinct())
            //{
            //    if (thing.IsMechanicalDef())
            //    {
       
            //    }
            //}
        }

        public static void MechanicalCorpseSetup()
        {
            raceThingList = DefDatabase<ThingDef>.AllDefs.Where(x => x.race is RaceProperties rp).ToList();
            foreach (var thing in raceThingList.Distinct())
            {
                if (thing.IsMechanicalDef())
                {
                    if (thing.race?.corpseDef != null)
                    {
                        var corpse = thing.race.corpseDef;
                        corpse.thingCategories ??= [];
                        //if (corpse.thingCategories.Contains(BSDefs.BS_RobotCorpses) == false)
                        //{
                        //    corpse.thingCategories.Add(BSDefs.BS_RobotCorpses);
                        //}
                        if (corpse.ingestible is IngestibleProperties ingestible)
                        {
                            ingestible.preferability = FoodPreferability.NeverForNutrition;
                        }

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
            foreach (var recipe in allRecipes.Where(x => !x.ExtensionsOnDef<RecipeExtension, RecipeDef>().NullOrEmpty()))
            {
                var recipeExtensions = recipe.ExtensionsOnDef<RecipeExtension, RecipeDef>();
                if (recipeExtensions.Any(x => x.isSurgery == true))
                {
                    recipe.isSurgeryCached = true;
                }

                foreach (var raceThing in raceThingListWithoutExt)
                {
                    bool change = false;

                    if (recipeExtensions.Any(x => x.ShouldAddToRace(raceThing)))
                    {
                        raceThing.recipes ??= [];
                        raceThing.recipes.AddDistinct(recipe);
                        //recipe.recipeUsers ??= [];
                        //recipe.recipeUsers.AddDistinct(raceThing);
                        //Log.Message($"Patched recipe {recipe.defName} to include {raceThing.defName}");
                    }
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

                        recipe.appliedOnFixedBodyParts.AddDistinct(part);
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
                    list.AddDistinct(kvp.Key);
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
