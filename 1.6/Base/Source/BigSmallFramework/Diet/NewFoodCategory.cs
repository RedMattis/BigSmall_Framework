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
            FilterResult result = FilterResult.None;
            if (!diets.NullOrEmpty())
            {
                result = result.Fuse(diets.Where(x => x.newFoodCategoryFilters != null).SelectMany(x => x.newFoodCategoryFilters.Items).GetFilterResult(this));
            }
            var fleshType = pawn.RaceProps?.FleshType;
            if (filterListFor.fleshTypes?.Items.GetFilterResult(fleshType) is FilterResult fleshResult && fleshResult != FilterResult.None)
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


}
