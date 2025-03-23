using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;

namespace BigAndSmall
{
    public class CustomButcherProduct
    {
        public ThingDef thingDef;
        public int amount = 1;
        public EnumRange<QualityCategory>? itemQualityRange = null;
        public float chance = 1f;
        public bool scaleToBodySize = false;
        public bool scaleToBodySizeSquared = false;
        public bool scaleToButcherEfficiency = false;

        public bool TryMake(Pawn butcher, Pawn entity, out Thing thing)
        {
            thing = null;
            if (thingDef == null)
            {
                return false;
            }
            if (!Rand.Chance(chance))
            {
                return false;
            }
            int num = amount;
            if (scaleToBodySize)
            {
                num = GenMath.RoundRandom(amount * entity.BodySize);
            }
            else if (scaleToBodySizeSquared)
            {
                num = GenMath.RoundRandom(amount * entity.BodySize * entity.BodySize);
            }
            if (scaleToButcherEfficiency)
            {
                if (entity.RaceProps.IsMechanoid)
                {
                    num = GenMath.RoundRandom(amount * butcher.GetStatValue(BSDefs.ButcheryMechanoidEfficiency));
                }
                else
                {
                    num = GenMath.RoundRandom(amount * butcher.GetStatValue(BSDefs.ButcheryFleshEfficiency));
                }
            }
            if (num <= 0) return false;
            else if (num < 1f && Rand.Chance(num))
            {
                num = 1;
            }
            thing = ThingMaker.MakeThing(thingDef);
            thing.stackCount = num;
            if (itemQualityRange != null)
            {
                thing.TryGetComp<CompQuality>()?.SetQuality(itemQualityRange.Value.RandomInRange, ArtGenerationContext.Colony);
            }
            return true;
        }
    }

    [HarmonyPatch]
    public class ButcheringHarmonyPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.ButcherProducts))]
        public static void ButcherProductsPostfix(Pawn __instance, ref IEnumerable<Thing> __result, Pawn butcher, float efficiency)
        {
            if (__result == null || __instance == null)
            {
                return;
            }
            var allPawnExts = __instance.GetAllPawnExtensions();
            var targetMeatDef = allPawnExts.FirstOrDefault(x=>x.meatOverride != null)?.meatOverride;
            var resultList = __result.ToList();
            if (targetMeatDef != null)
            {
                var ogMeatType = __instance.RaceProps?.meatDef;
                for (int idx = 0; idx < resultList.Count; idx++)
                {
                    Thing result = resultList[idx];
                    if (result?.def == ogMeatType)
                    {
                        int stackCount = result.stackCount;
                        resultList[idx] = ThingMaker.MakeThing(targetMeatDef);
                        resultList[idx].stackCount = stackCount;
                    }
                }
            }
            foreach (var ext in allPawnExts)
            {
                if (ext.butcherProducts != null)
                {
                    foreach (var product in ext.butcherProducts)
                    {
                        if (product.TryMake(butcher, __instance, out Thing thing))
                        {
                            resultList.Add(thing);
                        }
                    }
                }
            }
        }
    }
}
