using BetterPrerequisites;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{

    [StaticConstructorOnStartup]
    public class GeneGizmo_ResourceSlime : GeneGizmo_Resource
    {
        private List<Pair<IGeneResourceDrain, float>> tmpDrainGenes = new List<Pair<IGeneResourceDrain, float>>(); // Unused.

        public GeneGizmo_ResourceSlime(Gene_Resource gene, List<IGeneResourceDrain> drainGenes, Color barColor, Color barhighlightColor)
            : base(gene, drainGenes, barColor, barhighlightColor)
        {
            //draggableBar = true;
        }

        protected override string GetTooltip()
        {
            tmpDrainGenes.Clear();
            string text = $"{gene.ResourceLabel.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor)}: {gene.ValueForDisplay} / {gene.MaxForDisplay}\n";
            if (gene.pawn.IsColonistPlayerControlled || gene.pawn.IsPrisonerOfColony)
            {
                text = text + (string)("BS_AccumulateSlimeUntil".Translate() + ": ") + gene.PostProcessValue(gene.targetValue);
            }
            if (!gene.def.resourceDescription.NullOrEmpty())
            {
                text = text + "\n\n" + gene.def.resourceDescription.Formatted(gene.pawn.Named("PAWN")).Resolve();
            }
            return text;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            return base.GizmoOnGUI(topLeft, maxWidth, parms);
        }
    }

    public class BS_GeneSlimeProps : DefModExtension
    {
        public float resourceMaxOffset = 0;
        public float resourceStartOffset = 0;
    }

    public class BS_GeneSlimePower : BS_GenericResource, IGeneResourceDrain
    {
        protected override Color BarColor => new ColorInt(30, 60, 120).ToColor;
        protected override Color BarHighlightColor => new ColorInt(50, 100, 150).ToColor;

        private Hediff slimeHediff = null;

        public Hediff SlimeHediff
        {
            get
            { 
                // Check if hediff is null, or not assigned to the pawn.
                if (slimeHediff == null || pawn?.health?.hediffSet?.HasHediff(slimeHediff.def) != true)
                {
                    slimeHediff = AddOrGetHediff();
                }
                return slimeHediff;
            }
            set => slimeHediff = value;
        }

        public override float InitialResourceMax => 1f;

        public Gene_Resource Resource => this;

        public bool CanOffset
        {
            get
            {
                if (Active)
                {
                    return !pawn.Deathresting;
                }

                return false;
            }
        }

        public float ResourceLossPerDay => def.resourceLossPerDay;

        public Pawn Pawn => pawn;

        public string DisplayLabel => Label + " (" + "Gene".Translate() + ")";

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
                    targetValue = max > 2 ? max * 0.5f : 1.0f;
                }

                RecalculateMax();

                float maxValueChange = 0.125f;

                SlimeHediff.Severity = Mathf.Clamp(Value, 0.05f, 9999);

                float moveTowards;
                // Check if pawn has malnutrition. If so shrink.
                if (pawn?.health?.hediffSet?.HasHediff(HediffDefOf.Malnutrition) ?? false)
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

                // If value change was negative, fill the hunger bar somewhat.
                if (Mathf.Abs(newValue - Value) < 0.05f)
                {
                    // Pass
                }
                else if (newValue < Value)
                {
                    pawn.needs.food.CurLevelPercentage += 0.20f;
                }
                else if (newValue > Value)
                {
                    // If value change was positive, drain the hunger by 75%, leaving at least 10%
                    pawn.needs.food.CurLevelPercentage = Mathf.Max(0.10f, pawn.needs.food.CurLevelPercentage - 0.50f);
                }

                Value = newValue;
                SlimeHediff.Severity = Mathf.Clamp(Value, 0.05f, 9999);

                
            }
            //if (Find.TickManager.TicksGame % 10000 == 0)
            //{
            //    RefreshCache();
            //}
        }

        private void RefreshCache()
        {
            HumanoidPawnScaler.GetBSDict(pawn, forceRefresh: true);
        }

        public override void Reset()
        {
            cur = Mathf.Max(cur, 0.8f);
            targetValue = 0.8f;
            RecalculateMax();
            RefreshCache();
        }

        private void RecalculateMax(bool setup = false)
        {
            // for each active gene with the mod extension BS_GeneSlimeProps, add the resourceMaxOffset to the max value
            float maxIncrease = GetMaxOffset();
            float startBonus = GetStartingOffset();

            var newMax = InitialResourceMax + maxIncrease;
            // If roughly equal, do nothing. (epsilon)
            if (Mathf.Abs(newMax - max) < 0.03f)
            {
                return;
            }

            float changePercent = newMax / max;

            max = InitialResourceMax + maxIncrease;
            cur = Mathf.Clamp(cur, 0f, max);


            if (setup)
            {
                Value += startBonus;
                if (changePercent>1)
                    targetValue *= changePercent;
            }
            SlimeHediff.Severity = Mathf.Clamp(Value, 0.05f, 9999);
        }

        private float GetStartingOffset()
        {
            float currentBonus = 0;
            foreach (Gene curGene in GeneHelpers.GetAllActiveGenes(pawn))
            {
                if (curGene.def.HasModExtension<BS_GeneSlimeProps>())
                {
                    currentBonus += curGene.def.GetModExtension<BS_GeneSlimeProps>().resourceStartOffset;
                }
            }

            return currentBonus;
        }

        private float GetMaxOffset()
        {
            float increase = 0;
            foreach (Gene curGene in GeneHelpers.GetAllActiveGenes(pawn))
            {
                if (curGene.def.HasModExtension<BS_GeneSlimeProps>())
                {
                    increase += curGene.def.GetModExtension<BS_GeneSlimeProps>().resourceMaxOffset;
                }
            }

            return increase;
        }

        public override void PostAdd()
        {
            cur = 1;
            base.PostAdd();
            RecalculateMax(setup:true);
            RefreshCache();
        }

        private Hediff AddOrGetHediff()
        {
            // Get all hediffs in the library
            var hediffs = DefDatabase<HediffDef>.AllDefsListForReading;

            var hediffList = hediffs.Where(x => x.defName == "BS_SlimeMetabolism");
            if (hediffList.Count() == 0)
            {
                Log.Error("BS_SlimeMetabolism hediff not found in the library.");
                return null;
            }
            var hediff = hediffList.First();
            Hediff slimeHedif;
            // Check if we already have the hediff
            if (pawn.health.hediffSet.HasHediff(hediff))
            {
                // Get the hediff we added
                slimeHedif = pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
                slimeHedif.Severity = 1;
            }
            else
            {
                pawn.health.AddHediff(hediff);
                slimeHedif = pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
            }
            return slimeHedif;
        }
    }

    public class CompProperties_SlimeCost : CompProperties_PoolCost
    {
        public CompProperties_SlimeCost()
        {
            compClass = typeof(CompAbilityEffect_SlimeCost);
        }
    }

    public class CompAbilityEffect_SlimeCost : CompAbilityEffect_PoolCost
    {
        public new CompProperties_SlimeCost Props => (CompProperties_SlimeCost)props;
        protected override bool HasEnoughResource
        {
            // Replace in inherited class.
            get
            {
                BS_GeneSlimePower cPower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneSlimePower>();
                return cPower != null && cPower.Value >= Props.resourceCost;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            BS_GeneSlimePower slimePower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneSlimePower>();
            ResourcePoolUtils.OffsetResource(parent.pawn, 0f - Props.resourceCost, slimePower);
            slimePower.SlimeHediff.Severity = Mathf.Clamp(slimePower.Value, 0.05f, 9999);
        }

        public override bool GizmoDisabled(out string reason)
        {
            BS_GeneSlimePower cPower = parent.pawn.genes?.GetFirstGeneOfType<BS_GeneSlimePower>();
            if (cPower == null)
            {
                reason = "Ability Disabled: Missing Cursed Power Gene";
                return true;
            }
            if (cPower.Value < Props.resourceCost)
            {
                reason = "Ability Disabled: Not enough Power";
                return true;
            }
            float num = TotalostOfQueuedAbilities();
            float num2 = Props.resourceCost + num;
            if (Props.resourceCost > float.Epsilon && num2 > cPower.Value)
            {
                reason = "Ability Disabled: Not enough Power";
                return true;
            }
            reason = null;
            return false;
        }
    }
}
