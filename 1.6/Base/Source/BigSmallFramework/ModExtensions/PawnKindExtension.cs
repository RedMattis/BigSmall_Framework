using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class PawnKindExtension : DefModExtension
    {
        public class SkillPassion
        {
            public SkillDef skill = null;
            public Passion passion = Passion.None;
        }
        public class ApparelAndEquipmentGraphics
        {
            public CustomizableGraphic graphic = null;
            public bool colorAToApparelClr = true;

            // Weapon
            public bool applyToEquippment = false;

            // Apparel
            public bool? applyToApparel = null; // Default true unless applyToEquippment is true.
            public List<ApparelLayerDef> apparelLayer = null;
            public List<BodyPartGroupDef> bodyPartGroup = null;
            public List<ThingCategoryDef> thingCategories = null;
            public List<string> tradeTags = null;
            public string requiredTag = null;
            
        }

        public SimpleCurve ageCurve = null;
        public SimpleCurve ageCurveChronological = null;
        public SimpleCurve psylinkLevels = null;

        public List<SkillRange> clampedSkills = null;
        public List<SkillRange> skillRange = null;
        public bool skillRangeApplyToBabies = false;
        public bool canHavePassions = true;
        public List<SkillPassion> forcedPassions = null;

        public List<GeneDef> appendGenes = [];
        public bool appendAsXenogenes = false;
        public bool removeOverlappingGenes = true;
        public float animalSapienceChance = 0;

        public List<ApparelAndEquipmentGraphics> itemGraphics = null;
        public CustomizableGraphic pawnGraphic = null;

        /// <summary>
        /// Generate a "humanlike animal" dummy based on this PawnKindDef.
        /// Used to treat the pawn as if it had been generated from an animal.
        /// </summary>
        public bool generateHumanlikeAnimalFromThis = false;

        public Pawn Execute(Pawn pawn, bool singlePawn=false)
        {
            SetModdableGraphics(pawn);
            if (singlePawn && Rand.Chance(animalSapienceChance))
            {
                pawn = RaceMorpher.SwapAnimalToSapientVersion(pawn);
            }
            AppendGenes(pawn);
            ApplyPsylink(pawn);
            ApplyAgeCurve(pawn);
            ModifySkills(pawn);

            return pawn;
        }

        public void ModifySkills(Pawn pawn)
        {
            if (canHavePassions == false && pawn.skills != null)
            {
                foreach (var skill in pawn.skills.skills)
                {
                    skill.passion = Passion.None;
                }
            }
            if (forcedPassions != null && pawn.skills != null)
            {
                foreach ((var skill, var passion) in forcedPassions.Select(x => (x.skill, x.passion)))
                {
                    foreach (var pawnSkill in pawn.skills.skills)
                    {
                        if (pawnSkill.def != skill) continue;
                        pawnSkill.passion = passion;
                    }
                }
            }

            if (skillRange != null
                && pawn.skills != null
                && (skillRangeApplyToBabies || pawn.ageTracker?.CurLifeStage != LifeStageDefOf.HumanlikeBaby)
                )
            {
                foreach ((var skill, var range) in skillRange.Select(x => (x.Skill, x.Range)))
                {
                    foreach (var pawnSkill in pawn.skills.skills)
                    {
                        if (pawnSkill.def != skill) continue;
                        var randomLevel = Rand.RangeInclusive(range.min, range.max);
                        pawnSkill.Level = randomLevel;
                    }
                }
            }


            if (clampedSkills != null
                && pawn.skills != null
                && (skillRangeApplyToBabies || pawn.ageTracker?.CurLifeStage != LifeStageDefOf.HumanlikeBaby)
                )
            {
                foreach ((var skill, var range) in clampedSkills.Select(x => (x.Skill, x.Range)))
                {
                    foreach (var pawnSkill in pawn.skills.skills)
                    {
                        if (pawnSkill.def != skill) continue;
                        var learnedLevel = pawnSkill.GetLevel(includeAptitudes: false);
                        if (learnedLevel < range.min)
                        {
                            pawnSkill.Level = range.min;
                        }
                        else if (learnedLevel > range.max)
                        {
                            pawnSkill.Level = range.max;
                        }
                    }
                }
            }
        }

        public void SetModdableGraphics(Pawn pawn)
        {
            if (pawnGraphic != null)
            {
                CustomizableGraphic.Replace(pawn, pawnGraphic);
            }
            if (itemGraphics != null)
            {
                SetModdableApparelEtc(pawn);
            }
        }

        private void SetModdableApparelEtc(Pawn pawn)
        {
            var equippmentList = pawn.equipment?.AllEquipmentListForReading;
            var apparelWorn = pawn.apparel?.WornApparel;
            foreach (var gfx in itemGraphics)
            {
                if (gfx.applyToEquippment && equippmentList != null)
                {
                    foreach (var equippment in equippmentList)
                    {
                        CustomizableGraphic.Replace(equippment, gfx.graphic);
                    }
                }
                bool applyToApparel = gfx.applyToApparel ?? (gfx.applyToEquippment == false);
                if (apparelWorn != null && applyToApparel)
                {
                    foreach (var apparel in apparelWorn)
                    {
                        var def = apparel.def;
                        var app = apparel.def.apparel;
                        if ((gfx.apparelLayer == null || (app.layers != null && app.layers.Intersect(gfx.apparelLayer).Any()))
                            && (gfx.bodyPartGroup == null || (app.bodyPartGroups.Intersect(gfx.bodyPartGroup).Any()))
                            && (gfx.requiredTag == null || (app.tags.Contains(gfx.requiredTag))
                            && (gfx.thingCategories == null || (def.thingCategories.Intersect(gfx.thingCategories).Any()))
                            && (gfx.tradeTags == null || (def.tradeTags.Intersect(gfx.tradeTags).Any()))))
                        {
                            CustomizableGraphic.Replace(apparel, gfx.graphic);
                            if (gfx.colorAToApparelClr && gfx.graphic.colorA is Color colorA)
                            {
                                apparel.DrawColor = colorA;
                            }
                        }
                    }
                }
            }
        }

        public void AppendGenes(Pawn pawn)
        {
            if (pawn.genes == null) return;
            // Check exclusion tags and remove all conflicting genes.
            List<Gene> pawnGenes = [..pawn.genes.GenesListForReading];
            if (removeOverlappingGenes)
            {
                var appendGeneExlusions = appendGenes.SelectMany(x => x.exclusionTags).ToList();
                if (appendGeneExlusions.Any())
                {
                    foreach (var gene in pawnGenes.Where(x => !x.def.exclusionTags.NullOrEmpty() && x.def.exclusionTags.Intersect(appendGeneExlusions).Any()))
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                }
            }

            // Append
            if (appendAsXenogenes)
            {
                foreach (var gene in appendGenes)
                {
                    pawn.genes.AddGene(gene, true);
                }
            }
            else
            {
                foreach (var gene in appendGenes)
                {
                    pawn.genes.AddGene(gene, false);
                }
            }
        }

        public void ApplyAgeCurve(Pawn pawn)
        {
            if (ageCurve != null)
            {
                pawn.ageTracker.AgeBiologicalTicks = (long)ageCurve.Evaluate(Rand.Value) * 3600000;
            }
            if (ageCurveChronological != null)
            {
                var newAge = (long)ageCurveChronological.Evaluate(Rand.Value) * 3600000;
                if (newAge > pawn.ageTracker.AgeBiologicalTicks)
                {
                    pawn.ageTracker.AgeChronologicalTicks = newAge;
                }
            }

        }
        public void ApplyPsylink(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike == false) return;
            if (psylinkLevels is SimpleCurve psyLinkCurve && ModsConfig.RoyaltyActive)
            {
                int countToSet = (int)psyLinkCurve.Evaluate(Rand.Value);

                if (countToSet > 0)
                {
                    // Check if they have it already.
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier) is Hediff_Level hediff_Level)
                    {
                        int level = hediff_Level.level;
                        hediff_Level.SetLevelTo(countToSet + level);
                    }
                    else
                    {
                        hediff_Level = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn, pawn.health.hediffSet.GetBrain()) as Hediff_Level;
                        pawn.health.AddHediff(hediff_Level);
                        hediff_Level.SetLevelTo(countToSet);
                    }
                }
            }
        }
    }
}
