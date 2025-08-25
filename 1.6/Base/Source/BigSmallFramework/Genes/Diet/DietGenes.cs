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
    public static class FoodHelper
    {
        public static FilterResult GetFilterForFoodThingDef(this ThingDef foodDef, BSCache cache)
        {
            var catForFood = NewFoodCategory.foodCatagoryForFood.TryGetValue(foodDef);
            FilterResult result = FilterResult.None;
            if (catForFood != null)
            {
                if (cache.newFoodCatDeny?.Contains(catForFood) == true)
                {
                    result = result.Fuse(FilterResult.Deny);
                }
                else if (cache.newFoodCatAllow?.Contains(catForFood) == true)
                {
                    result = result.Fuse(FilterResult.Allow);
                }
                else
                {
                    result = result.Fuse(catForFood.allowByDefault ? FilterResult.Allow : FilterResult.Deny);
                }
            }
            return result;
        }

        public static FilterResult FilterForFoodThing(this Thing food, BSCache cache)
        {
            var result = FilterResult.None;
            if (cache.pawnDiet.NullOrEmpty() == false)
            {
                foreach (var diet in cache.pawnDiet)
                {
                    result = result.Fuse(diet.FilterForFood(food));
                }
            }

            return result;
        }
    }


    /// <summary>
    /// This is a list of food categories that a pawn may or may not be able to eat.
    /// The default assumption is that they can not.
    /// </summary>
    public class NewFoodCategory : Def
    {
        public static Dictionary<ThingDef, NewFoodCategory> foodCatagoryForFood = [];
        public static NewFoodCategory FoodCatagoryForThingDef(ThingDef foodDef) => foodCatagoryForFood.TryGetValue(foodDef, out NewFoodCategory foodCatagory) ? foodCatagory : null;

        public static void SetupFoodCategories()
        {
            var allFoodCategories = DefDatabase<NewFoodCategory>.AllDefsListForReading;
            foreach (var foodCategory in allFoodCategories)
            {
                foreach (var foodDef in foodCategory.foodDefs)
                {
                    foodCatagoryForFood[foodDef] = foodCategory;
                }
            }
        }

        // If none of these are true it will be assumed that the food is not acceptable.
        public class FilterListFor
        {
            public FilterListSet<FleshTypeDef> fleshTypes = new();
            public FilterListSet<ThingDef> pawnThingsDefs = new();
            public FilterListSet<GeneDef> geneDefs = new();  // Mostly for other modders to patch into. We use DietFilter for our own purposes.
        }
        public bool allowByDefault = false;  // If false this is a blacklist instead.
        public List<ThingDef> foodDefs = [];
        public FilterListFor filterListFor = new();
        public FilterResult DefaultAcceptPawn(Pawn pawn, ICollection<GeneDef> activeGenes, List<PawnDiet> diets)
        {
            FilterResult result = FilterResult.None ;
            if (!diets.NullOrEmpty())
            {
                result = result.Fuse(diets.Where(x => x.newFoodCategoryFilters != null).SelectMany(x => x.newFoodCategoryFilters.Items).GetFilterResult(this));
            }
            var fleshType = pawn.RaceProps?.FleshType;
            if(filterListFor.fleshTypes?.Items.GetFilterResult(fleshType) is FilterResult fleshResult && fleshResult != FilterResult.None)
            {
                result = result.Fuse(fleshResult);
            }
            if (filterListFor?.pawnThingsDefs?.Items.GetFilterResult(pawn.def) is FilterResult fleshWhiteListed && fleshWhiteListed != FilterResult.None)
            {
                result = result.Fuse(fleshWhiteListed);
            }
            if (filterListFor?.geneDefs?.Items.GetFilterResultFromItemList([.. activeGenes]) is FilterResult geneWhiteListed && geneWhiteListed != FilterResult.None)
            {
                result = result.Fuse(geneWhiteListed);
            }
            if (result != FilterResult.None)
            {
                return result;
            }

            return allowByDefault ? FilterResult.Allow : FilterResult.Neutral; // Neutral gets discarded, because we demand explicit acceptance.
        }
    }

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

    [HarmonyPatch]
    public static class DietPatch
    {
        public static bool ShouldSkipDietChecks(Pawn p)
        {
            if (p == null) return true;
            if (p.IsWildMan()) return true;
            if (p.IsBloodfeeder()) { return true; }
            if (p.IsMutant) { return true; }
            if (p.DevelopmentalStage == DevelopmentalStage.Baby) { return true; }
            return false;
        }

        // Iirc. WillEat ThingDef has troubles with caravans and stuff. Rimworld assumes humans are hardcoded so they can eat anything there.
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillEat), new Type[]
        {
            typeof(Pawn),
            typeof(ThingDef),
            typeof(Pawn),
            typeof(bool),
            typeof(bool)
        })]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.VeryHigh)]
        public static bool WillEatDef_Prefix(ref bool __result, Pawn p, ThingDef food, Pawn getter, bool careIfNotAcceptableForTitle, bool allowVenerated)
        {
            if (food == null)
            {
                return true;
            }
            if (ShouldSkipDietChecks(p))
            {
                return true;
            }
            if (p.GetCachePrepatched() is BSCache cache)
            {
                if (cache.willEatDef.TryGetValue(food, out bool cachedResult))
                {
                    if (cachedResult == false)
                    {
                        __result = false;
                        return false;
                    }
                    return true;
                }
                
                FilterResult result = food.GetFilterForFoodThingDef(cache);

                if (result.Denied())
                {
                    cache.willEatDef[food] = false;
                    __result = false;
                    return false;
                }
                else
                {
                    cache.willEatDef[food] = true;
                }

                return true;
            }
            return true;
        }

        
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillEat), new Type[]
        {
            typeof(Pawn),
            typeof(Thing),
            typeof(Pawn),
            typeof(bool),
            typeof(bool)
        })]
        [HarmonyPrefix]
        [HarmonyPriority(10000)]
        public static bool WillDietPermitEatingThing(ref bool __result, Pawn p, Thing food, Pawn getter, bool careIfNotAcceptableForTitle, bool allowVenerated)
        {
            if (food == null)
            {
                return true;
            }
            if (ShouldSkipDietChecks(p))
            {
                return true;
            }

            // Ignore unspawned pawns, it just gets messy because of Ludeon hardcoding.
            if (p.Spawned == true && p.GetCachePrepatched() is BSCache cache && cache.isHumanlike)
            {
                FilterResult result = food.FilterForFoodThing(cache);
                if (result.Denied())
                {
                    __result = false;
                    return false;
                }
                if (cache.willEatDef.TryGetValue(food.def, out bool cachedResult))
                {
                    if (cachedResult == false)
                    {
                        __result = false;
                        return false;
                    }
                    return true;
                }
                else
                {
                    var foodDef = food.def;
                    FilterResult filterResult = foodDef.GetFilterForFoodThingDef(cache);

                    if (filterResult.Denied())
                    {
                        cache.willEatDef[foodDef] = false;
                        __result = false;
                        return false;
                    }
                    else
                    {
                        cache.willEatDef[foodDef] = true;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// This is a patch that fixes so small characters don't overdose so easily.
        /// 
        /// It will also make cats and whatever not overdose on beer all the time, but arguably that's a good thing, because it... was stupid.
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch(typeof(CompDrug), nameof(CompDrug.PostIngested))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PostIngested_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                // Find the instruction that loads the body size
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo method && method.Name == "get_BodySize")
                {
                    yield return codes[i];
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0.8f);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Mathf).GetMethod(nameof(Mathf.Max), new Type[] { typeof(float), typeof(float) }));
                }
                else
                {
                    yield return codes[i]; // Yield the original instruction
                }
            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested),
        [
            typeof(Pawn),
            typeof(float),
        ])]
        [HarmonyPostfix]
        public static void Ingested_Postfix(Thing __instance, ref float __result, Pawn ingester, float nutritionWanted)
        {
            bool canEatThing = true;
            WillDietPermitEatingThing(ref canEatThing, ingester, __instance, null, false, false);
            // We're skipping the postfix if the character isn't spawned. Why you might ask? Because caravans don't check the food item's
            // Thing, only the ThingDef. This means we can't easily check if the item contains meat/vegtables. So we just skip it for everyone's sanity's sake.
            if (ingester?.Spawned == true && ingester.GetCachePrepatched() is BSCache cache && cache.isHumanlike)
            {
                if (!canEatThing && ingester.Faction == Faction.OfPlayerSilentFail)
                {
                    __result = 0;
                    Log.Warning($"[BigAndSmall] {ingester?.Name} ate {__instance?.def?.defName} which their gene-diet requirements does not permit" +
                        $"\nIf this was not due to the player forcing them to then something went wrong.");
                    if (ingester.Spawned)
                    {
                        ingester.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Vomit), JobCondition.InterruptForced, resumeCurJobAfterwards: false);
                    }
                    else
                    {
                        ingester.needs.food.CurLevel = -0.25f;
                    }
                }
            }

        }

        
    }
}
