using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static RimWorld.Building_HoldingPlatform;

namespace BigAndSmall
{
    public class BS_GeneSlimeProps : DefModExtension
    {
        public float resourceMaxOffset = 0;

        [Obsolete]
        public float resourceStartOffset = 0;
    }

    public class BS_GeneSlimePower : Gene_Resource, IGeneResourceDrain
    {
        public int offsetFromGenes = 0;
        public override float MinLevelForAlert => 0f;

        protected override Color BarColor => new ColorInt(30, 60, 120).ToColor;
        protected override Color BarHighlightColor => new ColorInt(50, 100, 150).ToColor;

        protected float cachedIncrease = 0;

        public override float InitialResourceMax => 1f;

        public override float MaxLevelOffset => 0;

        public override float Max => InitialResourceMax + cachedIncrease;
        public Gene_Resource Resource => this;

        public Hediff SlimeHediff => GetSlimeHediff();

        public bool CanOffset => Active;

        public float ResourceLossPerDay => def.resourceLossPerDay;

        public Pawn Pawn => pawn;

        public string DisplayLabel => Label + " (" + "Gene".Translate() + ")";

        
        public float DefaultTargetValue => Max < 2 ? 1f : 1.5f;

        public override void PostAdd()
        {
            Reset();
        }

        public override void SetTargetValuePct(float val)
        {
            if (float.IsNaN(val) || float.IsNaN(max))
            {
                return;
            }
            if (val > 1)
            {
                val = 1;
            }
            targetValue = val * Max;
        }

        public override void Reset()
        {
            CalculateResourceMaxOffset();
            targetValue = DefaultTargetValue;
            cur = DefaultTargetValue;
            SlimeHediff.Severity = Mathf.Clamp(Value, 0.05f, 9999);
            RefreshCache();
        }

        public override void Tick()
        {
            base.Tick();

            // Every 1000 ticks, adjust the resource updwards or downwards based on the target value and whether the pawn is starved or not.
            if (Find.TickManager.TicksGame % 500 == 0)
            {
                // Check if player-controlled
                bool playerControlled = pawn.IsColonist || pawn.IsPrisonerOfColony;
                if (!playerControlled)
                {
                    targetValue = DefaultTargetValue;
                }

                RecalculateMax();

                float maxValueChange = 0.125f;

                SlimeHediff.Severity = Mathf.Clamp(Value, 0.05f, 9999);

                float moveTowards;
                bool hasFoodNeed = pawn?.needs?.food != null;
                if (!hasFoodNeed) { moveTowards = targetValue; } // Probably a CreepJoiner.
                // Check if pawn has malnutrition. If so shrink.
                else if (pawn?.health?.hediffSet?.HasHediff(HediffDefOf.Malnutrition) ?? false)
                {
                    moveTowards = 0f;
                }
                else if (pawn?.needs?.food?.CurLevelPercentage > 0.29f)
                {
                    // If so, set the target to 0.5
                    moveTowards = targetValue;
                }
                else if (targetValue < cur)
                {
                    moveTowards = targetValue;
                }
                else
                {
                    return;
                }

                float valueChange;

                maxValueChange = Mathf.Min(maxValueChange, Mathf.Abs(moveTowards - cur));

                if (moveTowards > cur)
                {
                    valueChange = maxValueChange;
                }
                else
                {
                    valueChange = -maxValueChange;
                }
                float newValue = cur + valueChange;

                // If we would move past the target, reduce the value change to only move to the target.
                if (moveTowards > cur && cur + valueChange > moveTowards)
                {
                    newValue = moveTowards;
                }
                else if (moveTowards < cur && cur + valueChange < moveTowards)
                {
                    newValue = moveTowards;
                }

                // Tiny changs should not prompt a refresh.
                if (Mathf.Abs(newValue - Value) < 0.01f)
                {
                    Value = newValue;
                    SlimeHediff.Severity = Mathf.Clamp(Value, 0.05f, 9999);
                    return;
                }
                // If value change was negative, fill the hunger bar somewhat.
                else if (newValue < Value && hasFoodNeed)
                {
                    pawn.needs.food.CurLevelPercentage += 0.20f;
                }
                else if (newValue > Value && hasFoodNeed)
                {
                    // If value change was positive, drain the hunger by 75%, leaving at least 10%
                    pawn.needs.food.CurLevelPercentage = Mathf.Max(0.10f, pawn.needs.food.CurLevelPercentage - 0.50f);
                }

                Value = newValue;
                SlimeHediff.Severity = Mathf.Clamp(Value, 0.05f, 9999);
                RefreshCache();
            }
        }

        private void RefreshCache()
        {
            HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
        }

        private void RecalculateMax()
        {
            float currPerccent = cur / max;
            CalculateResourceMaxOffset();

            var newMax = Max;
            // If roughly equal, do nothing. (epsilon)
            if (Mathf.Abs(newMax - max) < 0.03f)
            {
                return;
            }
            cur = Mathf.Clamp(cur, 0f, max);
            if (currPerccent > 0)
            {
                SetTargetValuePct(Mathf.Clamp(currPerccent, 0, 1));
            }
            SlimeHediff.Severity = Mathf.Clamp(Value, 0.05f, 9999);
        }

        private float CalculateResourceMaxOffset()
        {
            float increase = 0;
            foreach (Gene curGene in GeneHelpers.GetAllActiveGenes(pawn))
            {
                if (curGene.def.HasModExtension<BS_GeneSlimeProps>())
                {
                    increase += curGene.def.GetModExtension<BS_GeneSlimeProps>().resourceMaxOffset;
                }
            }
            cachedIncrease = increase;
            max = InitialResourceMax + increase;
            return increase;
        }

        static HediffDef slimeHediffDef = null;
        public Hediff GetSlimeHediff()
        {
            if (slimeHediffDef == null)
            {
                // Get all hediffs in the library
                var hediffs = DefDatabase<HediffDef>.AllDefsListForReading;

                var hediffList = hediffs.Where(x => x.defName == "BS_SlimeMetabolism");
                if (hediffList.Count() == 0)
                {
                    Log.Error("BS_SlimeMetabolism hediff not found in the library.");
                    return null;
                }
                slimeHediffDef = hediffList.First();
            }
            if (!pawn.health.hediffSet.HasHediff(slimeHediffDef))
            {
                pawn.health.AddHediff(slimeHediffDef);
            }
            return pawn.health.hediffSet.GetFirstHediffOfDef(slimeHediffDef);
        }


        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!Active)
            {
                yield break;
            }
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            foreach (Gizmo resourceDrainGizmo in GeneResourceDrainUtility.GetResourceDrainGizmos(this))
            {
                yield return resourceDrainGizmo;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cachedIncrease, "cachedIncrease", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                targetValue *= max;
            }
        }
    }
}
