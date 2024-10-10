using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
   
    [HarmonyPatch]
    public static class DietPatch
    {
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillEat), new Type[]
        {
            typeof(Pawn),
            typeof(Thing),
            typeof(Pawn),
            typeof(bool),
            typeof(bool)
        })]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        public static bool WillEat_Prefix(ref bool __result, Pawn p, Thing food, Pawn getter, bool careIfNotAcceptableForTitle, bool allowVenerated)
        {
            __result = WillEat(__result, p, food);
            return __result;
        }

        private static bool WillEat(bool __result, Pawn p, Thing food)
        {
            if (p.RaceProps.Humanlike && p.genes != null)
            {
                // Allow nutrient paste. It is too much of a hassle to make people have seperate meat/veg networks.
                // Er. I mean the nutrient paste is so nutritionally perfected that it is acceptable. Ofc.
                if (food?.def?.defName?.Contains("NutrientPaste") == true)
                {
                    return true;
                }
                if (p?.DevelopmentalStage == DevelopmentalStage.Baby)
                {
                    return true;
                }
                if (HumanoidPawnScaler.GetCache(p) is BSCache cache)
                {
                    if (cache.diet == FoodKind.Any)
                        return true;
                    if (cache.diet == FoodKind.NonMeat && !FoodUtility.AcceptableVegetarian(food))
                    {
                        return false;
                    }
                    if (cache.diet == FoodKind.Meat && !FoodUtility.AcceptableCarnivore(food))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        //[HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillEat), new Type[]
        //{
        //    typeof(Pawn),
        //    typeof(ThingDef),
        //    typeof(Pawn),
        //    typeof(bool),
        //    typeof(bool)
        //})]
        //[HarmonyPrefix]
        //public static bool WillEat_Postfix2(ref bool __result, Pawn p, ThingDef food, Pawn getter, bool careIfNotAcceptableForTitle, bool allowVenerated)
        //{
        //    __result = WillEat(__result, p, food);
        //    return __result;
        //}


        [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested), new Type[]
        {
            typeof(Pawn),
            typeof(float),
        })]
        [HarmonyPostfix]
        public static void Ingested_Postfix(Thing __instance, ref float __result, Pawn ingester, float nutritionWanted)
        {
            // Literally we're skipping the postfix if the character isn't spawned. Why you might ask? Because caravans don't check the food item's
            // Thing, only the ThingDef. This means we can't easily check if the item contains meat/vegtables. So we just skip it for everyone's sanity's sake.

            if (ingester?.Spawned == true && ingester?.RaceProps?.Humanlike == true && ingester.genes != null)
            {
                var cache = HumanoidPawnScaler.GetCache(ingester);
                if (cache != null)
                {
                    bool ateInedible = false;
                    if (__instance?.def?.defName?.Contains("NutrientPaste") == true)
                    {
                        return;
                    }
                    if (ingester?.DevelopmentalStage == DevelopmentalStage.Baby)
                    {
                        return;
                    }


                    if (cache.diet == FoodKind.NonMeat && !RimWorld.FoodUtility.AcceptableVegetarian(__instance))
                    {
                        ateInedible = true;
                    }

                    if (cache.diet == FoodKind.Meat && !RimWorld.FoodUtility.AcceptableCarnivore(__instance))
                    {
                        ateInedible = true;
                    }

                    if (ateInedible)
                    {
                        //var need = ingester.needs.TryGetNeed(NeedDefOf.Food);
                        //if(need != null)
                        //    need.CurLevel = 0;

                        __result = 0;
                        // Vomit

                        Log.Warning($"[BigAndSmall] {ingester.Name} ate {__instance.def.defName} which their gene-diet requirements does not permit" +
                            $"\nIf this was not due to the player forcing them to then something went wrong.");
                        if (ingester.Spawned)
                        {
                            ingester.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Vomit), JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
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
}
