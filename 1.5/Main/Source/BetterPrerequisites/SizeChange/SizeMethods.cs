using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public partial class BSCache : IExposable, ICacheable
    {
        public void CalculateSize(DevelopmentalStage dStage, List<PawnExtension> geneExts, bool overrideLimits)
        {
            int currentTick = Find.TickManager.TicksGame;
            if (lastUpdateTick == null || lastUpdateTick != currentTick)
            {
                lastUpdateTick = currentTick;
                previousScaleMultiplier = this.scaleMultiplier;  // First time this runs on a pawn after loading this will be 1.
                healthMultiplier_previous = CalculateHealthMultiplier(this.scaleMultiplier, pawn);
            }

            float offsetFromSizeByAge = geneExts.Sum(x => x.GetSizeFromSizeByAge(pawn?.ageTracker?.AgeBiologicalYearsFloat));

            // Multiply each value together.
            float multiplierFromSizeMultiplierByAge = geneExts.Where(x => x.sizeByAgeMult != null).Aggregate(1f, (acc, x) => acc * x.GetSizeMultiplierFromSizeByAge(pawn?.ageTracker?.AgeBiologicalYearsFloat));

            float sizeFromAge = pawn?.ageTracker?.CurLifeStage?.bodySizeFactor ?? 1;
            float baseSize = pawn?.RaceProps?.baseBodySize ?? 1;
            float previousTotalSize = sizeFromAge * baseSize;

            float offsetStat = pawn.GetStatValue(BSDefs.SM_BodySizeOffset) + offsetFromSizeByAge;
            float cosmeticOffsetStat = pawn.GetStatValue(BSDefs.SM_Cosmetic_BodySizeOffset);
            float multiplierStat = pawn.GetStatValue(BSDefs.SM_BodySizeMultiplier) * multiplierFromSizeMultiplierByAge;
            float cosmeticMultiplierStat = pawn.GetStatValue(BSDefs.SM_Cosmetic_BodySizeMultiplier);

            cosmeticOffsetStat += offsetStat;

            float totalCosmeticMultiplier = multiplierStat + cosmeticMultiplierStat - 1;

            totalSizeOffset = ((baseSize + offsetStat) * multiplierStat * sizeFromAge) - previousTotalSize;
            float bodySizeCosmeticOffset = ((baseSize + cosmeticOffsetStat) * totalCosmeticMultiplier * sizeFromAge) - previousTotalSize;

            // Get total size
            totalSize = totalSizeOffset + previousTotalSize;
            totalCosmeticSize = bodySizeCosmeticOffset + previousTotalSize;

            // Check if the pawn has a hediff with a name starting with BS_Affliction

            if (!overrideLimits)
            {
                ////////////////////////////////// 
                // Clamp Total Size

                // Prevent babies from getting too large for even the giant cribs, or too smol in general.
                if (dStage < DevelopmentalStage.Child)
                {
                    totalSize = Mathf.Clamp(totalSize, 0.05f, 0.40f);
                    // Clamp the offset too.
                    totalSizeOffset = Mathf.Clamp(totalSizeOffset, 0.05f - previousTotalSize, 0.40f - previousTotalSize);

                }
                else if (totalSize < 0.10)
                {
                    totalSize = 0.10f;
                    totalSizeOffset = 0.10f - previousTotalSize;
                }


                ////////////////////////////////// 
                // Clamp Offset to avoid extremes
                if (totalSize < 0.05f && dStage < DevelopmentalStage.Child)
                {
                    totalSizeOffset = -(previousTotalSize - 0.05f);
                }
                // Don't permit babies too large to fit in cribs (0.25)
                else if (totalSize > 0.40f && dStage < DevelopmentalStage.Child && pawn.RaceProps.Humanlike)
                {
                    totalSizeOffset = -(previousTotalSize - 0.40f);
                }
                else if (totalSize < 0.10f && dStage == DevelopmentalStage.Child)
                {
                    totalSizeOffset = -(previousTotalSize - 0.10f);
                }
                // If adult basically limit size to 0.10
                else if (totalSize < 0.10f && dStage > DevelopmentalStage.Child && pawn.RaceProps.Humanlike)
                {
                    totalSizeOffset = -(previousTotalSize - 0.10f);
                }
            }
            else
            {
                // Even with funky status conditions set the limit at 2%.
                totalSize = Mathf.Max(totalSize, 0.02f);
            }

            headSizeMultiplier = pawn.GetStatValue(BSDefs.SM_HeadSize_Cosmetic);

            scaleMultiplier = GetPercentChange(totalSizeOffset, pawn);
            cosmeticScaleMultiplier = GetPercentChange(bodySizeCosmeticOffset, pawn);

            if (!pawn.RaceProps.Humanlike) //&& cosmeticScaleMultiplier.linear > 1.5f)
            {
                // Because of how we scale animals in the ELSE-statement the scaling of animals/Mechs gets run twice.
                // Checking their node explicitly risks missing cases where someone uses another node.
                cosmeticScaleMultiplier.linear = Mathf.Sqrt(cosmeticScaleMultiplier.linear);
            }
            
            healthMultiplier = CalculateHealthMultiplier(scaleMultiplier, pawn);
            if (!healthMultiplier_previous.ApproximatelyEquals(healthMultiplier))
            {
                injuriesRescaled = false;
            }

            //pawn.GetStatValue(BSDefs.BS_FinalSizeMultiplier);
            //pawn.GetStatValue(BSDefs.BS_MaxNutritionFromSize);
            if (BSDefs.BS_MaxNutritionFromSize.Worker is StatWorker_MaxNutritionFromSize maxNutritionWorker)
            {
                maxNutritionWorker.SetTemporaryStatCache(pawn, scaleMultiplier.linear);
            }
        }

        private float CalculateHeadOffset(float headPosMultiplier)
        {
            

            float headPos = Mathf.Lerp(bodyRenderSize, headRenderSize, 0.8f);
            headPos *= headPosMultiplier;
            headPos = Mathf.Max(bodyRenderSize, headRenderSize);
            if (headPos < 1) { headPos = Mathf.Pow(headPos, 0.96f); }

            if (preventHeadScaling || bodyConstantHeadScale || (bodyConstantHeadScaleBigOnly && bodyRenderSize > 1))
            {
                float hPos = bodyRenderSize;
                hPos *= headPosMultiplier;
                return Mathf.Lerp(headPos, hPos, preventHeadOffsetFactor);
            }
            return headPos;

            //var headPos = Mathf.Lerp(bodyRenderSize, headRenderSize, 0.8f);
            //headPos *= headPosMultiplier;
            ////var headPos = Mathf.Max(bodySize, headSize);

            //// Move up the head for dwarves etc. so they don't end up a walking head.
            //if (headPos < 1) { headPos = Mathf.Pow(headPos, 0.96f); }
            //headPositionMultiplier = headPos;
        }

        private void SetWorldOffset()
        {

            var factor = bodyRenderSize;

            var originalFactor = factor;
            if (factor < 1) { factor = 1; }
            float offsetFromCache = bodyPosOffset;
            float typeGScale = 1;
            if (pawn.story?.bodyType != null)
            {
                var bodyType = pawn.story.bodyType;

                typeGScale = bodyType.bodyGraphicScale.y;
                // Check if hulk. If so increase the value, because hulks are weirldy offset down in vanilla.
                if (bodyType == BodyTypeDefOf.Hulk)
                {
                    offsetFromCache += 0.25f;
                }
            }

            worldspaceOffset = (factor - 1) / 2 * (offsetFromCache + 1) + offsetFromCache * 0.30f * (originalFactor < 1 ? originalFactor : 1) * typeGScale;
        }

        private static PercentChange GetPercentChange(float bodySizeOffset, Pawn pawn)
        {
            if (pawn != null && bodySizeOffset != 0 &&
               (pawn.needs != null || pawn.Dead))
            {
                const float minimum = 0.2f;  // Let's not make humans sprites unreasonably small.
                float sizeFromAge = pawn.ageTracker.CurLifeStage.bodySizeFactor;
                float baseSize = pawn?.RaceProps?.baseBodySize ?? 1;
                float prevBodySize = sizeFromAge * baseSize;
                float postBodySize = prevBodySize + bodySizeOffset;
                float percentChange = postBodySize / prevBodySize;
                float quadratic = Mathf.Pow(postBodySize, 2) / Mathf.Pow(prevBodySize, 2);
                float cubic = Mathf.Pow(postBodySize, 3) / Mathf.Pow(prevBodySize, 3);

                // Ensure we don't get negative values.
                percentChange = Mathf.Max(percentChange, 0.04f);
                quadratic = Mathf.Max(quadratic, 0.04f);
                cubic = Mathf.Max(cubic, 0.04f);

                if (percentChange < minimum) percentChange = minimum;
                return new PercentChange(percentChange, quadratic, cubic);
            }
            return new PercentChange(1, 1, 1);
        }

        /// <summary>
        /// Used to get more realistic results from size changes.
        /// F.ex. most things scale quadratically, but weight/health scales by cube.
        /// 
        /// Technically a Rimworld Scale isn't really linear, but this type of change gives fairly good values when going upwards.
        /// Downwards is another story though, and we don't want small pawns to get utterly obliterated if something looks at the wrong.
        /// </summary>
        public enum SizeChangeType
        {
            Linear = 1,     // ...Height
            Quadratic = 2,  // Muscle Strength, food consumption, health, etc.
            Cubic = 3      // Weight
        };

        static readonly float hulkSize = 0.88f;
        static readonly float fatSize = 0.93f;
        static readonly float thinSize = 1.00f;
        public float GetHeadRenderSize()
        {
            float bodyRSize = GetBodyRenderSize();

            

            float bodyTypeScale = 1;
            // Even out the cosmetic sizes of the pawn since we already have genes for the bodysize itself.
            if (pawn.story != null && BigSmallMod.settings.scaleBodyTypes)
            {
                if (pawn.story.bodyType == BodyTypeDefOf.Hulk)
                {
                    bodyTypeScale = hulkSize;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Fat)
                {
                    bodyTypeScale = fatSize;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Thin)
                {
                    bodyTypeScale = thinSize;
                }
                bodyRSize *= 1 / bodyTypeScale;
            }

            float headSize = bodyRSize;

            if (headSize > 1)
            {
                //headSize = Mathf.Pow(bodyRSize, 0.8f);
                headSize = Mathf.Pow(bodyRSize, BigSmallMod.settings.headPowLarge);
                headSize = Math.Max(bodyRSize - 0.5f, headSize);
            }
            else
            {
                // Beeg head for tiny people.
                headSize = Mathf.Pow(bodyRSize, BigSmallMod.settings.headPowSmall);
            }

            headSize *= headSizeMultiplier;

            if (preventHeadScaling || bodyConstantHeadScale || (bodyConstantHeadScaleBigOnly && bodyRSize > 1))
            {
                float preventedHeadScale = preventHeadScaling ? bodyRSize : bodyRSize * headSizeMultiplier;
                return Mathf.Lerp(headSize, preventedHeadScale, preventHeadScalingFactor);
            }
            return headSize;
        }
        public float GetBodyRenderSize()
        {
            float bodyScale = cosmeticScaleMultiplier.linear;

            if (bodyScale == 1)
            {
                //return 1;
            }
            else if (bodyScale < 1)
            {
                if (!hasSizeAffliction)
                {
                    // Make Nisse babies smaller so they look plausible next to their parents.
                    if (pawn.DevelopmentalStage < DevelopmentalStage.Child)
                    {
                        bodyScale = Mathf.Pow(bodyScale, 0.95f);
                    }
                    else if (pawn.DevelopmentalStage < DevelopmentalStage.Adult)
                    {
                        bodyScale = Mathf.Pow(bodyScale, 0.90f);
                    }
                    else // Don't make children/adults too small on screen.
                    {
                        bodyScale = Mathf.Pow(bodyScale, 0.75f);
                    }
                }
                bodyScale = bodyScale * BigSmallMod.settings.visualSmallerMult;

            }
            else
            {
                if (pawn.DevelopmentalStage < DevelopmentalStage.Child) // Babies should still be small-ish. even for large races.
                {
                    bodyScale = Mathf.Pow(bodyScale, 0.40f);
                }
                else if (pawn.DevelopmentalStage < DevelopmentalStage.Adult) // Don't oversize children too much.
                {
                    bodyScale = Mathf.Pow(bodyScale, 0.50f);
                }
                else // Don't make large characters unreasonably huge.
                {
                    bodyScale = Mathf.Pow(bodyScale, 0.7f);
                }
                bodyScale = (bodyScale - 1) * BigSmallMod.settings.visualLargerMult + 1;
            }

            // Even out the cosmetic sizes of the pawn since we already have genes for the bodysize itself.
            if (pawn.story != null && BigSmallMod.settings.scaleBodyTypes)
            {
                if (pawn.story.bodyType == BodyTypeDefOf.Hulk)
                {
                    bodyScale *= hulkSize;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Fat)
                {
                    bodyScale *= fatSize;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Thin)
                {
                    bodyScale *= thinSize;
                }
            }

            return bodyScale;
        }
        private static float CalculateHealthMultiplier(BSCache.PercentChange scalMult, Pawn pawn)
        {
            if (scalMult.linear <= 1) return scalMult.linear;
            float percentChange = scalMult.linear;

            const float maxHealthScale = 4;  // A Thrumbo has x2. (8 / 4), then it falls off.
            float lerpScaleFactor = maxHealthScale / 1;

            float raceHealthBase = pawn.RaceProps?.baseHealthScale ?? 1;
            float raceSize = pawn.RaceProps?.baseBodySize ?? 1;

            float raceHealth = raceHealthBase / raceSize;
            float targetRaceHScale = Mathf.Max(maxHealthScale, raceHealth);

            float baseSize = raceSize * pawn?.ageTracker?.CurLifeStage?.bodySizeFactor ?? 1;
            float newSize = percentChange * baseSize;
            float sizeChange = newSize - baseSize;

            // At a total offset of +3.0, the health scale will be 8 if not better, as with a Thrumbo
            float n = Mathf.Clamp01(sizeChange / lerpScaleFactor);

            float newScale = Mathf.SmoothStep(raceHealth, targetRaceHScale, n);
            float newScale2 = Mathf.Lerp(raceHealth, targetRaceHScale, n);
            newScale = Mathf.Lerp(newScale, newScale2, 0.5f);

            float changeInRaceScale = newScale / raceHealth;

            return percentChange * changeInRaceScale;
        }

        public class PercentChange : IExposable
        {
            public float linear = 1;
            public float quadratic = 1;
            public float cubic = 1;
            public float KelibersLaw => Mathf.Pow(cubic, 0.75f);    // Results in a colonist that does nothing but eat. Not a great idea...
            public float DoubleMaxLinear => linear < 1 ? linear : 1 + ((linear - 1) * 2);
            public float TripleMaxLinear => linear < 1 ? linear : 1 + ((linear - 1) * 3);

            // For Scribe
            public PercentChange() { }

            public PercentChange(float linear, float quadratic, float cubic)
            {
                this.linear = linear;
                this.quadratic = quadratic;
                this.cubic = cubic;
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref linear, "linear", 1);
                Scribe_Values.Look(ref quadratic, "quadratic", 1);
                Scribe_Values.Look(ref cubic, "cubic", 1);
            }
        }
    }
}
