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
				int count = cache.pawnDiet.Count;
				for (int i = 0; i < count; i++)
                {
					PawnDiet diet = cache.pawnDiet[i];
					FilterResult filterResult = diet.FilterForFood(food);
					if (filterResult.Denied())
						return filterResult;

					if (filterResult > result)
						result = filterResult;
                }
            }

            return result;
        }
    }



}
