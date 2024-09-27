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
                if (HumanoidPawnScaler.GetBSDict(___pawn) is BSCache cache)
                {
                    float scale = cache.scaleMultiplier.linear;
                    float newFoodCapacity = __result * cache.foodNeedCapacityMult;
                    if (scale > 1f)
                    {
                        scale = Mathf.Clamp01((scale-1) / 3);
                        newFoodCapacity *= Mathf.Lerp(1, 3f, scale);
                    }
                    else if (scale < 1f) // Don't shrink the food bar too much or they will waste an unreasonably large amount of food from meals.
                    {
                        newFoodCapacity = (newFoodCapacity / scale + newFoodCapacity) / 2;
                    }
                    __result = newFoodCapacity;
                    if (cache.previousFoodCapacity is float prevFoodCap)
                    {
                        // if newMaxlevel is not approximately equals to previous max level....
                        if (!Mathf.Approximately(newFoodCapacity, prevFoodCap))
                        {
                            float foodLevel = ___curLevelInt;
                            float currentPercent = foodLevel / newFoodCapacity;
                            float previousPercent = foodLevel / prevFoodCap;

                            ___curLevelInt = foodLevel * (previousPercent / currentPercent);

                            // Check if the value is valid, if not set it to 50%
                            if (float.IsNaN(___curLevelInt) || float.IsInfinity(___curLevelInt))
                            {
                                ___curLevelInt = newFoodCapacity * 0.5f;
                            }
                            // Check if the value is more than 300% if so set it to 300%
                            else if (___curLevelInt > newFoodCapacity * 3)
                            {
                                ___curLevelInt = newFoodCapacity * 3;
                            }
                        }
                    }

                    cache.previousFoodCapacity = newFoodCapacity;
                }
            }
        }
    }
}
