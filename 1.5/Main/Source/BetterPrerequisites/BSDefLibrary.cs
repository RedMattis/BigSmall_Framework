using System.Collections.Generic;
using Verse;

namespace BigAndSmall
{
    /// <summary>
    /// Just copy pasting this from the "Femboy" mod for the simple reason that "Femboy" is a too controversial term to have as a dependency.
    /// </summary>
    public static class BSDefLibrary
    {
        private static List<NewFoodCategory> foodCategoryDefs;
        public static List<NewFoodCategory> FoodCategoryDefs => foodCategoryDefs ??= DefDatabase<NewFoodCategory>.AllDefsListForReading;
    }
}
