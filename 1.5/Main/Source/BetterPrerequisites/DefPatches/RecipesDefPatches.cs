using BetterPrerequisites;
using BigAndSmall.FilteredLists;
using HarmonyLib;
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

        private static void MigrateHumanRecipes()
        {
            List<(ThingDef thing, RaceExtension raceExt)> thingsWithRaceExtension = DefDatabase<ThingDef>.AllDefs
                .Where(x => x.modExtensions != null && x.modExtensions.Any(y => y is RaceExtension))
                .Select(td => (td, td.modExtensions.FirstOrDefault(x => x is RaceExtension) as RaceExtension)).ToList();

            var humanLikes = DefDatabase<HumanLikes>.AllDefs;


            // Migrate all recipes for stuff without a PawnExtension and Tracking Hediff instructing on what to transfer.
            List<ThingDef> allHumanlikeThings = [.. humanLikes.SelectMany(x => x.thingList), 
                .. thingsWithRaceExtension
                    .Where(x => x.raceExt?.SurgeryRecipes.AnyItems() != true).Select(x => x.thing)];
            var humanRecipes = AllHumanRecipes();
            foreach (var thing in allHumanlikeThings)
            {
                foreach (var recipe in humanRecipes.Where(x => !thing.recipes.Contains(x)))
                {
                    thing.recipes.Add(recipe);
                    //Log.Message($"Patched recipe {recipe.defName} to include {thing.defName}");
                }
            }

            var allRecipes = DefDatabase<RecipeDef>.AllDefs;
            
            foreach((ThingDef thing, RaceExtension raceExt) in thingsWithRaceExtension.Where(x=>x.raceExt.SurgeryRecipes.AnyItems()))
            {
                var rFilter = raceExt.SurgeryRecipes;
                if (rFilter == null) continue;
                var recipesFromHuman = humanRecipes.Where(x => rFilter.GetFilterResult(x).Accepted());
                var recipesFromAll = allRecipes.Where(x => rFilter.GetFilterResult(x).ExplicitlyAllowed());  
                thing.recipes.AddRange(recipesFromHuman);
                thing.recipes.AddRange(recipesFromAll);
                thing.recipes = thing.recipes.Distinct().ToList();
            }
        }

        public static Dictionary<BodyPartDef, List<BodyPartDef>> partImportsFromDict = [];
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
        }
    }

    public class BodyPartExtension : DefModExtension
    {
        public List<BodyPartDef> importAllRecipesFrom = [];
    }

}
