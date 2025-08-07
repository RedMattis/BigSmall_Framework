using BigAndSmall;
using RimWorld;
using System;
using Verse;

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

    public class ProductionGene : TickdownGene //PGene
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

        const int tickFrequency = 1000;

        public static int ModifyProductionBasedOnSize(int result, Pawn pawn)
        {
            var cache = FastAcccess.GetCache(pawn);

            if (cache != null)
                result = Math.Max(1, (int)(result * cache.scaleMultiplier.DoubleMaxLinear));
            return result;
        }

        public override void TickEvent()
        {
            if (props == null)
            {
                props = def.GetModExtension<ProductionGeneSettings>();
                if (props == null)
                {
                    Log.Error("ProductionGeneSettings not found for " + def.defName);
                }
            }
            // If full, produce resources and add to inventory.
            if (props != null && ActiveAndFull)
            {
                if (pawn?.Dead != false || pawn.Deathresting)
                {
                    return;
                }
                var resourceToProduce = ResourceDef;
                var amountToProduce = ResourceAmount;

                // Add to inventory.
                var inventory = pawn.inventory;
                var thing = ThingMaker.MakeThing(resourceToProduce);
                thing.stackCount = amountToProduce;

                // Add to inventory if the pawn is not spawned or on the map.
                if (pawn.Map == null || pawn.Spawned == false)
                {
                    inventory.innerContainer.TryAdd(thing);
                }
                else
                {
                    GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }

                // Reset progress.
                fullness = 0;
            }


            if (Active)
            {
                float num = tickFrequency / GatherResourcesIntervalDays;
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

        public override void ResetCountdown()
        {
            tickDown = tickFrequency;
        }
    }
}
