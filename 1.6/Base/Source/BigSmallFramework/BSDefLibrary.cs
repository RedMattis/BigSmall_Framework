using System.Collections.Generic;
using Verse;

namespace BigAndSmall
{
    public static class BSDefLibrary
    {
        private static List<NewFoodCategory> foodCategoryDefs;
        public static List<NewFoodCategory> FoodCategoryDefs => foodCategoryDefs ??= DefDatabase<NewFoodCategory>.AllDefsListForReading;
    }
}
