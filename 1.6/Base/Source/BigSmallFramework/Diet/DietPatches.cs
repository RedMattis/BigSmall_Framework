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
