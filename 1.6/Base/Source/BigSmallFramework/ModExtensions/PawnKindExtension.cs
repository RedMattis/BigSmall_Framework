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
            public Passion? passion = null;
            public int incrementBy = 0;
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
        public bool? canHavePassions = null;
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
            if (pawn.skills?.skills == null)
            {
                if (skillRange != null || clampedSkills != null || forcedPassions != null || canHavePassions != null)
                {
                    Log.Warning($"PawnKindExtension for {pawn} tried to modify skills but they have no skills.");
                }
            }
            if (canHavePassions == false)
            {
                foreach (var skill in pawn.skills.skills)
                {
                    skill.passion = Passion.None;
                }
            }
            if (forcedPassions != null)
            {
                foreach (var fPassion in forcedPassions)
                {
                    foreach (var pawnSkill in pawn.skills.skills.Where(x=>x.def == fPassion.skill))
                    {
                        if (fPassion.passion is Passion newPassion)
                        {
                            pawnSkill.passion = newPassion;

                            if (ModsConfig.BiotechActive)
                            {
                                foreach (var gene in GeneHelpers.GetAllActiveGenes(pawn))
                                {
                                    if (gene.def.passionMod is PassionMod passionMod && passionMod.skill == fPassion.skill)
                                    {
                                        gene.passionPreAdd = newPassion;
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < Math.Abs(fPassion.incrementBy); i++)
                        {
                            if (fPassion.incrementBy > 0)
                            {
                                pawnSkill.passion = pawnSkill.passion.IncrementPassion();
                                //Log.Message($"Incremented passion of {pawn} for {pawnSkill.def} to {pawnSkill.passion}");
                            }
                            else if (fPassion.incrementBy < 0)
                            {
                                pawnSkill.passion = (Passion)Math.Max((int)Passion.None, (int)pawnSkill.passion - 1);
                            }
                        }
                    }
                }
            }

            if (skillRange != null
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
