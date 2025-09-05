using BigAndSmall.FilteredLists;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall
{

    public class PawnDiet : Def
    {
        [Flags]
        public enum GeneralFoodCategory
        {
            Ignore,
            Carnivore,
            Herbivore,
            ExclusiveCarnivore,
            ExclusiveHerbivore,
            Nothing,
        }
        public FilterListSet<ThingDef> foodFilters = null;
        public GeneralFoodCategory foodCategory = GeneralFoodCategory.Ignore;
        public FilterListSet<NewFoodCategory> newFoodCategoryFilters = null;
        public bool alwaysAcceptProcessed = true;
        public bool alwaysAcceptNutrientPaste = true;
        public bool alwaysAcceptNonIngestible = true;

        public Dictionary<ThingDef, FilterResult> willAcceptCacheThingless = [];
        public Dictionary<ThingDef, FilterResult> willAcceptCache = [];

        public static bool IsNutrientPaste(ThingDef foodDef) => foodDef.defName.Contains("NutrientPaste");

        public FilterResult AcceptFoodCategory(NewFoodCategory foodCategory)
        {
            if (newFoodCategoryFilters != null)
            {
                return newFoodCategoryFilters.GetFilterResult(foodCategory);
            }
            return foodCategory.allowByDefault ? FilterResult.Allow : FilterResult.Neutral;
        }

        // Method unused. Look into if this has a purpose later.
        public FilterResult FilterForFoodWithoutThing(ThingDef foodDef)
        {
            if (willAcceptCacheThingless.TryGetValue(foodDef, out FilterResult cachedResult)) return cachedResult;

            FilterResult result = FilterForDef(foodDef);
            if (result.PriorityResult() || result.Denied()) return result;
            if (foodCategory != GeneralFoodCategory.Ignore && foodDef.IsIngestible && !foodDef.IsProcessedFood) // No point checking this on actual food items.
            {
                bool foodCatagoryMatch = foodCategory switch
                {
                    GeneralFoodCategory.Carnivore => FoodUtility.GetFoodKind(foodDef) != FoodKind.NonMeat,
                    GeneralFoodCategory.Herbivore => FoodUtility.GetFoodKind(foodDef) != FoodKind.Meat,
                    GeneralFoodCategory.Nothing => false,
                    _ => true
                };
                result = result.Fuse(foodCatagoryMatch ? FilterResult.Neutral : FilterResult.Deny);
            }
            willAcceptCacheThingless[foodDef] = result;
            return result;
        }

        public FilterResult FilterForFood(Thing food)
        {
            FilterResult result = FilterForDef(food.def);
            if (result.PriorityResult() || result.Denied()) return result;
            if (foodCategory != GeneralFoodCategory.Ignore && food.def.IsIngestible && !food.def.IsProcessedFood)
            {
                bool foodCatagoryMatch = foodCategory switch
                {
                    GeneralFoodCategory.Carnivore => FoodUtility.AcceptableCarnivore(food),
                    GeneralFoodCategory.Herbivore => FoodUtility.AcceptableVegetarian(food),
                    GeneralFoodCategory.ExclusiveCarnivore => FoodUtility.AcceptableCarnivore(food) && !FoodUtility.AcceptableVegetarian(food),
                    GeneralFoodCategory.ExclusiveHerbivore => FoodUtility.AcceptableVegetarian(food) && !FoodUtility.AcceptableCarnivore(food),
                    GeneralFoodCategory.Nothing => false,
                    _ => true
                };
                return result.Fuse(foodCatagoryMatch ? FilterResult.Neutral : FilterResult.Deny);
            }
            return result;
        }

        private FilterResult FilterForDef(ThingDef foodDef)
        {
            if (willAcceptCache.TryGetValue(foodDef, out FilterResult cachedResult)) return cachedResult;
            FilterResult result = FilterResult.Neutral;
            if (alwaysAcceptNonIngestible && !foodDef.IsIngestible) return FilterResult.ForceAllow; // Item is not food, likely a serum or the like.
            if (alwaysAcceptProcessed && foodDef.IsProcessedFood) return FilterResult.ForceAllow; // Processed food (e.g. drugs) is always acceptable.
            // Mostly to avoid forcing the hassle of setting up a seperate food network for nutrient paste onto people.
            if (alwaysAcceptNutrientPaste && IsNutrientPaste(foodDef)) return FilterResult.ForceAllow;

            if (foodFilters != null)
            {
                var filterResult = foodFilters.GetFilterResult(foodDef);
                result = result.Fuse(filterResult);
            }
            willAcceptCache[foodDef] = result;
            return result;
        }

        //public static void DebugTestAllowanceOnPawn(Pawn pawn)
        //{
        //    if (HumanoidPawnScaler.GetCache(pawn, forceRefresh: true) is BSCache cache)
        //    {
        //        var allThings = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.ingestible != null);
        //        var activeGenes = GeneHelpers.GetAllActiveGeneDefs(pawn);
        //        var pawnDiets = cache.pawnDiet;
        //        foreach (var foodDef in allThings)
        //        {
        //            var acceptReports = pawnDiets.Select(diet => diet.FilterForFood(foodDef));
        //            Log.Message($"{acceptReports.Fuse().ToString().CapitalizeFirst()}: {pawn.def.defName}->{foodDef.defName}. Diets are {pawnDiets.Select(diet => diet.defName).ToCommaList()}.");
        //        }
        //    }
        //    else
        //    {
        //        Log.Warning($"[BigAndSmall] {pawn.def.defName} could not generate a cache..");
        //    }
        //}
    }

}
