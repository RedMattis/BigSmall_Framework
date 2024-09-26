using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(Need_Food), "MaxLevel", MethodType.Getter)]
    public static class FoodNeedPatch
    {
        public static void Postfix(ref float __result, ref Need_Food __instance, ref Pawn ___pawn, ref float ___curLevelInt)
        {
            if (___pawn?.needs != null
                && !___pawn.AnimalOrWildMan())
            {
                var cache = HumanoidPawnScaler.GetBSDict(___pawn);
                if (cache != null)
                {
                    float newFoodCapacity = __result * cache.foodNeedCapacityMult * Mathf.Max(1, cache.scaleMultiplier.linear);
                    __result = newFoodCapacity;

                    // if newMaxlevel is not approximately equals to previous max level....
                    if (!Mathf.Approximately(newFoodCapacity, cache.previousFoodCapacity))
                    {
                        float foodLevel = ___curLevelInt;
                        float currentPercent = foodLevel / newFoodCapacity;
                        float previousPercent = foodLevel / cache.previousFoodCapacity;

                        // Multiply the food level so we retain the same percentage of food.
                        ___curLevelInt = foodLevel * (previousPercent / currentPercent);

                        // Check if the value is valid, if not set it to 50%
                        if (float.IsNaN(___curLevelInt) || float.IsInfinity(___curLevelInt))
                        {
                            ___curLevelInt = newFoodCapacity * 0.5f;
                        }
                    }

                    cache.previousFoodCapacity = newFoodCapacity;
                }
            }
        }
    }
}
