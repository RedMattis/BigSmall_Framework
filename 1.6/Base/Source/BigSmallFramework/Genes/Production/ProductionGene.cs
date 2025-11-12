using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace BigAndSmall
{
    public class ProductionGeneSettings : DefModExtension
    {
        public class SubProductionGeneSettings
        {
            public ThingDef product;
            public int baseAmount = 10;
        }
        public int baseAmount = 10;
        public float frequencyInDays = 1;
        public string progressName = "NameMissing"; // Currently Unused.
        public ThingDef product;
        public string saveKey = "SaveKeyMissing";
        public List<SubProductionGeneSettings> extra = [];
    }

    public class ProductionGene : TickdownGene
    {
        ProductionGeneSettings Props = null;
        const int ticksPerDay = 60000;
        protected float fullness;

        protected int ResourceAmount => ModifyProductionBasedOnSize(Props.baseAmount, pawn);
        protected float GatherResourcesIntervalDays => Props.frequencyInDays * ticksPerDay;
        protected ThingDef ResourceDef => Props.product;

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
            Props = def.GetModExtension<ProductionGeneSettings>();
            Scribe_Values.Look(ref fullness, Props.saveKey, 0f);
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
            if (Props == null)
            {
                Props = def.GetModExtension<ProductionGeneSettings>();
                if (Props == null)
                {
                    Log.Error("ProductionGeneSettings not found for " + def.defName);
                }
            }
            // If full, produce resources and add to inventory.
            if (Props != null && ActiveAndFull)
            {
                if (pawn?.Dead != false || pawn.Deathresting)
                {
                    return;
                }
                var resourceToProduce = ResourceDef;
                var amountToProduce = ResourceAmount;

                // Add to inventory.
                var inventory = pawn.inventory;
                Produce(resourceToProduce, amountToProduce, inventory);

                foreach(var extra in Props.extra)
                {
                    var extraResourceToProduce = extra.product;
                    var extraAmountToProduce = ModifyProductionBasedOnSize(extra.baseAmount, pawn);
                    Produce(extraResourceToProduce, extraAmountToProduce, inventory);
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

        private void Produce(ThingDef resourceToProduce, int amountToProduce, Pawn_InventoryTracker inventory)
        {
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
        }

        public override void ResetCountdown()
        {
            tickDown = tickFrequency;
        }
    }
}
