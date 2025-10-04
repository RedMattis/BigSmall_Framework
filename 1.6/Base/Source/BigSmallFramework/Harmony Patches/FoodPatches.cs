using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace BigAndSmall
{
    
    [HarmonyPatch(typeof(Need_Food), nameof(Need_Food.FoodFallPerTickAssumingCategory))]
    public static class Need_Food_FoodFallPerTickAssumingCategory
    {
        public static void Prefix(ref Pawn ___pawn, out float __state)
        {
            __state = ___pawn.def.race.baseHungerRate;
            if (___pawn.GetCachePrepatched() is BSCache sizeCache && ___pawn.DevelopmentalStage > DevelopmentalStage.Baby)
            {
                float hungerRate = __state * Mathf.Max(sizeCache.scaleMultiplier.linear, sizeCache.scaleMultiplier.DoubleMaxLinear);
                float finalHungerRate = Mathf.Lerp(__state, hungerRate, BigSmallMod.settings.hungerRate);

                ___pawn.def.race.baseHungerRate = finalHungerRate;
            }
        }

        public static void Postfix(ref float __result, Pawn ___pawn, float __state)
        {
            ___pawn.def.race.baseHungerRate = __state;
        }
    }


}
