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
    /// <summary>
    /// Patch defs in other mods. And things related to defs.
    /// </summary>
    public class HumanLikes : Def
    {
        public List<ThingDef> thingList = [];
    }
    public static class HumanPatcher
    {
        public static void PatchRecipes()
        {
            MigrateHumanRecipes();
            PatchCustomBodyPartDefs();
        }

        private static void MigrateHumanRecipes()
        {
            List<(ThingDef thing, RaceExtension raceExt)> thingsWithRaceExtension = DefDatabase<ThingDef>.AllDefs
                .Where(x => x.modExtensions != null && x.modExtensions.Any(y => y is RaceExtension))
                .Select(td => (td, td.modExtensions.FirstOrDefault(x => x is RaceExtension) as RaceExtension)).ToList();

            var humanLikes = DefDatabase<HumanLikes>.AllDefs;

            // Migrate all recipes for stuff without a PawnExtension and Tracking Hediff instructing on what to transfer.
            List<ThingDef> allHumanlikeThings = [.. humanLikes.SelectMany(x => x.thingList), 
                .. thingsWithRaceExtension
                    .Where(x => x.raceExt?.raceHediff?.GetModExtension<PawnExtension>()?.surgeryRecipes == null).Select(x => x.thing)];
            var humanRecipes = ThingDefOf.Human.recipes;
            foreach (var thing in allHumanlikeThings)
            {
                foreach (var recipe in humanRecipes.Where(x => !thing.recipes.Contains(x)))
                {
                    thing.recipes.Add(recipe);
                    //Log.Message($"Patched recipe {recipe.defName} to include {thing.defName}");
                }
            }

            var allRecipes = DefDatabase<RecipeDef>.AllDefs;
            
            foreach((ThingDef thing, RaceExtension raceExt) in thingsWithRaceExtension.Where(x=>x.raceExt.raceHediff?.GetModExtension<PawnExtension>()?.surgeryRecipes != null))
            {
                var rFilter = raceExt.raceHediff.GetModExtension<PawnExtension>()?.surgeryRecipes;
                if (rFilter == null) continue;
                var recipesFromHuman = humanRecipes.Where(x => rFilter.GetFilterResult(x).Accepted());
                var recipesFromAll = allRecipes.Where(x => rFilter.GetFilterResult(x).ExplicitlyAllowed());  
                thing.recipes.AddRange(recipesFromHuman);
                thing.recipes.AddRange(recipesFromAll);
                thing.recipes = thing.recipes.Distinct().ToList();
            }
        }

        private static void PatchCustomBodyPartDefs()
        {
            var partDefs = DefDatabase<BodyPartDef>.AllDefs;
            List<(BodyPartDef part, List<BodyPartDef> sources)> partExts = partDefs.Where(x => x.modExtensions != null && x.modExtensions.Any(y => y is BodyPartExtension)).Select(bd =>
            {
                // We want to add ALL since someone might be patching in multiple of the same extension.
                HashSet<BodyPartDef> defs = [];
                foreach(var ext in bd.modExtensions.Where(x=>x is BodyPartExtension).Select(bpe => bpe as BodyPartExtension))
                {
                    defs.AddRange(ext.importAllRecipesFrom);
                }
                return (bd, defs.ToList());
            }).ToList();
            var allRecipes = DefDatabase<RecipeDef>.AllDefs;

            foreach ((BodyPartDef part, IEnumerable<BodyPartDef> partsToCopyFrom) in partExts)
            {
                foreach (var recipe in allRecipes.Where(x => x.appliedOnFixedBodyParts.Any(y => partsToCopyFrom.Contains(y))))
                {
                    if (!recipe.appliedOnFixedBodyParts.Contains(part))
                    {
                        recipe.appliedOnFixedBodyParts.Add(part);
                        //Log.Message($"Patched recipe {recipe.defName} to include {part.defName}");
                    }
                }
            }
        }
    }

    public class BodyPartExtension : DefModExtension
    {
        public List<BodyPartDef> importAllRecipesFrom = [];
    }

}
