using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class Regeneration : TickdownGene
    {
        const float baseHealingPerDayForSize1 = 8;
        const int tickFq = 1000;
        const float healingPerEvent = baseHealingPerDayForSize1 / GenDate.TicksPerDay * tickFq;
        private static readonly FloatRange TendingQualityRange = new(0.4f, 1.0f);

        public override void ResetCountdown()
        {
            tickDown = tickFq;
        }
        public override void TickEvent()
        {
            if (pawn?.Dead == true || pawn?.health?.hediffSet == null)
            {
                return;
            }
            var allInjuries = HealthHelpers.GetAllInjuries(pawn);
            if (allInjuries.Any())
            {
                float healingAmount = GetHealingAmount() * healingPerEvent;
                float totalCoverage = allInjuries.Sum(x => x.Part.coverageAbsWithChildren + 0.0001f);
                float totalHealing = 0;
                foreach (var injury in allInjuries)
                {
                    float healing = healingAmount * ((injury.Part.coverageAbsWithChildren + 0.0001f) / totalCoverage) * Rand.Range(0.5f, 1.5f);
                    injury.Heal(healing);
                    totalHealing += healing;

                    try
                    {
                        if (injury.TendableNow())
                        {
                            injury.Tended(TendingQualityRange.RandomInRange, TendingQualityRange.TrueMax, 1);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ErrorOnce($"Unhandled exception trying to tend wound {e.Message}\n{e.StackTrace}", 346722245);
                    }
                    
                }
            }
            else
            {
                var missingPart = HealthHelpers.GetMissingPart(pawn);
                if (missingPart != null)
                {
                    var part = missingPart.Part;
                    pawn.health.RemoveHediff(missingPart);
                    // Add scar putting it at 90% of full health.
                    var scar = HediffMaker.MakeHediff(HediffDefOf.Misc, pawn, part);

                    var partHealth = pawn.health.hediffSet.GetPartHealth(part);
                    scar.Severity = partHealth * 0.85f;
                    if (scar.TryGetComp<HediffComp_GetsPermanent>() is HediffComp_GetsPermanent comp)
                    {
                        comp.IsPermanent = true;
                        comp.SetPainCategory(PainCategory.Painless);
                    }
                    pawn.health.AddHediff(scar, part, null, null);
                    pawn.health.Notify_HediffChanged(scar);
                }
            }
        }

        protected virtual float GetHealingAmount()
        {
            float healingRate = GetBaseHealingRate();
            if (pawn?.BodySize > 1.2f)
            {
                healingRate *= pawn.HealthScale;
            }
            return healingRate;
        }

        protected float GetBaseHealingRate()
        {
            float healingRate = pawn.GetStatValue(StatDefOf.InjuryHealingFactor, true, cacheStaleAfterTicks: 1000);
            if (healingRate > 1)
            {
                healingRate = 1 + (healingRate - 1) * 0.5f;
            }

            return healingRate;
        }
    }
}
