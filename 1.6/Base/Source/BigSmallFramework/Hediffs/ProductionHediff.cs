using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class ProductionHediffSettings : HediffCompProperties
    {
        public class ProductionSettings
        {
            public ThingDef product;
            public List<ThingDef> randomProduct = [];
            public int baseAmount = 10;

            public string ProductTooltip() => randomProduct.Any() ? string.Join(", ", randomProduct.Select(rp => rp.LabelCap))
                : product?.LabelCap ?? "ProductLabelMissing";
        }
        public float frequencyInDays = 1;
        public string progressName = "NameMissing"; // Currently Unused.
        public string saveKey = "SaveKeyMissing";
        public int activationAge = 13;
        public bool femaleOnly = false;
        public float chance = 1f;
        public List<ProductionSettings> products = [];

        public ProductionHediffSettings()
        {
            compClass = typeof(ProductionHediff);
        }

        public Type NextFromThis()
        {
            Type cc = null;
            if (compClass == typeof(ProductionHediff))
                cc = typeof(ProductionHediff_1);
            else if (compClass == typeof(ProductionHediff_1))
                cc = typeof(ProductionHediff_2);
            else if (compClass == typeof(ProductionHediff_2))
                cc = typeof(ProductionHediff_3);
            else if (compClass == typeof(ProductionHediff_3))
                cc = typeof(ProductionHediff_4);
            else if (compClass == typeof(ProductionHediff_4))
                cc = typeof(ProductionHediff_5);
            else if (compClass == typeof(ProductionHediff_5))
                cc = typeof(ProductionHediff_6);
            else if (compClass == typeof(ProductionHediff_6))
                cc = typeof(ProductionHediff_7);
            else if (compClass == typeof(ProductionHediff_7))
                cc = typeof(ProductionHediff_8);
            else if (compClass == typeof(ProductionHediff_8))
                cc = null;
            return cc;
        }
    }

    public class ProductionHediff_1 : ProductionHediff { }
    public class ProductionHediff_2 : ProductionHediff { }
    public class ProductionHediff_3 : ProductionHediff { }
    public class ProductionHediff_4 : ProductionHediff { }
    public class ProductionHediff_5 : ProductionHediff { }
    public class ProductionHediff_6 : ProductionHediff { }
    public class ProductionHediff_7 : ProductionHediff { }
    public class ProductionHediff_8 : ProductionHediff { }

    public class ProductionHediff : TickdownHediffComp
    {
        ProductionHediffSettings Props => props as ProductionHediffSettings;
        const int ticksPerDay = 60000;
        protected float fullness;
        protected float GatherResourcesIntervalDays => Props.frequencyInDays * ticksPerDay; // / 1000 for testing.

        protected virtual bool ProductionActive
        {
            get
            {
                if (parent.pawn.Suspended)
                {
                    return false;
                }
                return true;
            }
        }

        public virtual bool Active
        {
            get
            {
                if (Props.femaleOnly && parent.pawn.gender != Gender.Female)
                    return false;
                if (!ProductionActive)
                    return false;
                bool ageRequirementMet = parent?.pawn?.ageTracker?.AgeBiologicalYears >= Props.activationAge;
                if (!ageRequirementMet)
                    return false;
                return true;

            }
        }

        public virtual bool ActiveAndFull
        {
            get
            {
                return fullness >= 1f;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref fullness, Props.saveKey, 0f);
        }

        const int tickFrequency = 1000;

        public static int ModifyProductionBasedOnSize(int result, Pawn pawn)
        {
            var cache = pawn.GetCachePrepatched();

            if (cache != null)
                result = Math.Max(1, (int)(result * cache.scaleMultiplier.DoubleMaxLinear));
            return result;
        }

        public override void TickEvent()
        {
            // If full, produce resources and add to inventory.
            if (parent?.pawn is Pawn pawn)
            {
                if (ActiveAndFull)
                {
                    if (pawn?.Dead != false || pawn.Deathresting)
                    {
                        return;
                    }
                    var inventory = pawn.inventory;
                    foreach (var pData in Props.products)
                    {
                        if (Props.chance < 1f && Rand.Value > Props.chance)
                            continue;

                        var amount = ModifyProductionBasedOnSize(pData.baseAmount, pawn);
                        if (pData.randomProduct.Any())
                        {
                            var randomOption = pData.randomProduct.RandomElement();
                            Produce(randomOption, amount, pawn, inventory);
                        }
                        else if (pData.product != null)
                        {
                            Produce(pData.product, amount, pawn, inventory);
                        }

                        
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
        }

        private void Produce(ThingDef resourceToProduce, int amountToProduce, Pawn pawn, Pawn_InventoryTracker inventory)
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

        public override string CompDescriptionExtra
        {
            get
            {
                string s = base.CompDescriptionExtra;
                s += $"\n\n{string.Join(", ",Props.products.Select(p => p.ProductTooltip() + " x"
                    + ModifyProductionBasedOnSize(p.baseAmount, parent.pawn)))}".Colorize(ColoredText.TipSectionTitleColor);
                s += $", ({fullness.ToStringPercent()})";
                return s;
            }
        }
    }
}
