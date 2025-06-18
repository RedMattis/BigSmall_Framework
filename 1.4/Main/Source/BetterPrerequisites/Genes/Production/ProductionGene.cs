using BetterPrerequisites;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    // Doesn't work because the method is abstract.
    //[HarmonyPatch]
    //public class HasGatherableBodyResourcePatch
    //{
    //    // Need to test performance on this.
    //    [HarmonyPatch(typeof(CompHasGatherableBodyResource), "ResourceAmount", MethodType.Getter)]
    //    [HarmonyPostfix]
    //    public static void ResourceAmountPatch(ref int __result, ref CompHasGatherableBodyResource __instance)
    //    {
    //        if (__instance?.parent?.ParentHolder is Pawn pawn)
    //        {
    //            ProductionGene.ModifyProductionBasedOnSize(__result, pawn);
    //        }
    //    }
    //}

    public class ProductionGeneSettings : DefModExtension
    {
        public int baseAmount = 10;
        public float frequencyInDays = 1;
        public string progressName = "NameMissing"; // Currently Unused.
        public ThingDef product;
        public string saveKey = "SaveKeyMissing";
    }

    public class ProductionGene : PGene
    {
        ProductionGeneSettings props = null;
        const int ticksPerDay = 60000;
        protected float fullness;

        protected int ResourceAmount => ModifyProductionBasedOnSize(props.baseAmount, pawn);
        protected float GatherResourcesIntervalDays => props.frequencyInDays * ticksPerDay;
        protected ThingDef ResourceDef => props.product;

        protected virtual bool ProductionActive
        {
            get
            {
                if (pawn.Faction == null)
                {
                    return false;
                }

                if (pawn.Suspended)
                {
                    return false;
                }

                return true;
            }
        }

        public bool ActiveAndFull
        {
            get
            {
                if (!Active)
                {
                    return false;
                }

                return fullness >= 1f;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            props = def.GetModExtension<ProductionGeneSettings>();
            Scribe_Values.Look(ref fullness, props.saveKey, 0f);
        }

        public override void Tick()
        {
            base.Tick();
            if (props == null)
            {
                props = def.GetModExtension<ProductionGeneSettings>();
                if (props == null)
                {
                    Log.Error("ProductionGeneSettings not found for " + def.defName);
                }
            }
            if (Find.TickManager.TicksGame % 1000 == 0 && Active)
            {
                // If full, produce resources and add to inventory.
                if (props != null && ActiveAndFull)
                {
                    var resourceToProduce = ResourceDef;
                    var amountToProduce = ResourceAmount;

                    // Add to inventory.
                    var inventory = pawn.inventory;
                    var thing = ThingMaker.MakeThing(resourceToProduce);
                    thing.stackCount = amountToProduce;
                    if (inventory != null || !pawn.IsPrisonerOfColony)
                    {
                        inventory.innerContainer.TryAdd(thing);
                    }
                    // Prisoners should drop the resources on the ground, so the player can pick them up.
                    else
                    {
                        GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    }

                    // Reset progress.
                    fullness = 0;
                }
                //Log.Message($"Fullness for {ResourceDef.defName} is {fullness} for {pawn.Name}");
            }

            if (Active)
            {
                float num = 1f / GatherResourcesIntervalDays;
                if (pawn != null)
                {
                    num *= PawnUtility.BodyResourceGrowthSpeed(pawn);
                }

                fullness += num;
                if (fullness > 1f)
                {
                    fullness = 1f;
                }
            }
        }


        public static int ModifyProductionBasedOnSize(int result, Pawn pawn)
        {
            var cache = FastAcccess.GetCache(pawn);
            if (cache != null)
                result = Math.Max(1, (int)(result * cache.scaleMultiplier.TripleMaxLinear));
            return result;
        }

    }
}
