﻿using HarmonyLib;
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
        [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillEat_NewTemp), new Type[]
        {
            typeof(Pawn),
            typeof(Thing),
            typeof(Pawn),
            typeof(bool),
            typeof(bool)
        })]
        [HarmonyPostfix]
        public static bool WillEat_Postfix(bool __result, Pawn p, Thing food, Pawn getter, bool careIfNotAcceptableForTitle, bool allowVenerated)
        {
            if (__result == false) return false;

            if (p.RaceProps.Humanlike && p.genes != null)
            {
                var cache = HumanoidPawnScaler.GetBSDict(p);
                if (cache != null)
                {
                    if (cache.diet == FoodKind.Any)
                        return true;
                    if (cache.diet == FoodKind.NonMeat && !RimWorld.FoodUtility.AcceptableVegetarian(food))
                        return false;

                    if (cache.diet == FoodKind.Meat && !RimWorld.FoodUtility.AcceptableCarnivore(food))
                        return false;
                }
            }
            return true;
        }


        [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested), new Type[]
        {
            typeof(Pawn),
            typeof(float),
        })]
        [HarmonyPostfix]
        public static void Ingested_Postfix(Thing __instance, ref float __result, Pawn ingester, float nutritionWanted)
        {
            if (ingester?.RaceProps?.Humanlike == true && ingester.genes != null)
            {
                var cache = HumanoidPawnScaler.GetBSDict(ingester);
                if (cache != null)
                {
                    bool ateInedible = false;
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
                        ingester.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Vomit), JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
                    }
                }
            }
        }

    }
}
