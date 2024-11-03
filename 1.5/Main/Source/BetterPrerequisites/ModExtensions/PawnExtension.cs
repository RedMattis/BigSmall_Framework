using BigAndSmall;
using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static BigAndSmall.RaceHelper;

namespace BetterPrerequisites
{
    // GeneExtension is literally just here to avoid breaking old XML refering to it by the old name.
    public class GeneExtension : PawnExtension { }
}
namespace BigAndSmall
{

    public class FacialAnimDisabler
    {
        public string headName = "NOT_";
        public string skinName = "NOT_";
        public string lidName = "NOT_";
        public string lidOptionsName = "NOT_";
        public string eyeballName = "NOT_";
        public string mouthName = "NOT_";
        public string browName = "NOT_";
    }

    public class PawnExtension : DefModExtension
    {
        // Used for race-defaults.
        public static PawnExtension defaultPawnExtension = new();


        public int priority = 0;
        //public HediffDef applyBodyHediff;
        public List<ConditionalStatAffecter> conditionals;
        public bool? invert;
        public bool renderCacheOff = false;

        public List<HediffToBody> applyBodyHediff;
        public List<HediffToBodyparts> applyPartHediff;

        public ThingDef thingDefSwap = null;
        public bool forceThingDefSwap = false;
        //public bool oneTimeSwap = true;

        public List<GeneDef> hiddenGenes = [];

        public ApparelRestrictions apparelRestrictions = null;

        public TransformationGene transformGene = null;
        public SimpleCurve sizeByAge = null;
        public SimpleCurve sizeByAgeMult = null;

        public List<Aptitude> aptitudes = null;

        #region Rendering
        public CustomMaterial bodyMaterial = null;
        public CustomMaterial headMaterial = null;
        public AdaptiveGraphicsCollection bodyPaths = [];
        public AdaptiveGraphicsCollection headPaths = [];
        public bool hideBody = false;
        public bool hideHead = false;
        public bool forceFemaleBody = false;
        #endregion

        public PawnDiet pawnDiet = null;
        public bool pawnDietRacialOverride = false;

        #region Birth
        public List<int> babyBirthCount = null;
        public int? babyStartAge = null;
        public float pregnancySpeedMultiplier = 1;
        #endregion


        public float bodyPosOffset = 0f;
        public float headPosMultiplier = 0f; // Actually an offset to the multiplier
        public bool preventDisfigurement = false;
        public bool canWalkOnCreep = false;

        public bool isDrone = false;
        public bool forceUnarmed = false;

        public bool hideInGenePicker = false;
        public bool hideInXenotypeUI = false; // Obsolete. Remove this later. Just here for a bit while I update the defs.

        public ConsumeSoul consumeSoulOnHit = null;

        //public bool isMechanical = false;
        public bool isUnliving = false;
        public bool isDeathlike = false;
        public bool isMechanical = false;
        public bool isBloodfeeder = false;


        // Metamorph Stuff.
        public XenotypeDef metamorphTarget = null;
        public XenotypeDef retromorphTarget = null;
        public int? metamorphAtAge = null;
        public int? retromorphUnderAge = null;
        public bool metamorphIfPregnant = false;
        public bool metamorphIfNight = false;
        public bool metamorphIfDay = false;

        // Locked Needs.
        public List<BetterPrerequisites.LockedNeedClass> lockedNeeds;
        //

        public Shader geneShader = null;
        public FacialAnimDisabler facialDisabler = null;

        public bool disableFacialAnimations = false;



        #region Obsolete
        public bool unarmedOnly = false;    // Still plugged in, but the name was kind of bad. Use forceUnarmed instead.
        #endregion

        // The below are only used for races at the moment. They could be ported over, but at the moment they are not
        // Invoked outside of that context.
        #region Some of these are RACE ONLY. Use elsewhere at your own risk.
        #region
        public List<HediffDef> forcedHediffs = [];
        #endregion

        public List<TraitDef> forcedTraits = new List<TraitDef>();

        #region Biotech
        public List<GeneDef> forcedEndogenes = null;
        public List<GeneDef> immutableEndogenes = null;
        public List<GeneDef> forcedXenogenes = null;
        #endregion

        #region White, Black, and Allow-lists.
        public FilterListSet<GeneDef> geneFilters = null;
        public FilterListSet<GeneCategoryDef> geneCategoryFilters = null;
        public FilterListSet<string> geneTagFilters = null;

        public FilterListSet<TraitDef> traitFilters = null;

        public FilterListSet<HediffDef> hediffFilters = null;
        public FilterListSet<HairDef> hairFilters = null;
        public FilterListSet<RecipeDef> surgeryRecipes = null;
        //public AllowListHolder<ThingDef> allowedFood = null;  // Not yet implemented. Easy enough, just not needed yet.
        #endregion

        public float headPosMultiplierOffset = 0f;

        // These are just for generating pawns. They are most useful on custom races, not on genes/hediffs.
        // Don't forget that is also inherits the props from "CompProperties_ColorAndFur".
        // E.g. skinColorOverride, hairColorOverride, etc.
        public List<GeneDef> randomHairGenes = null;
        public List<GeneDef> randomSkinGenes = null;

        #region tags

        
        #endregion
        #endregion

        public float GetSizeFromSizeByAge(float? age)
        {
            if (sizeByAge == null || age == null) return 0f;
            return sizeByAge.Evaluate(age.Value);
        }

        public float GetSizeMultiplierFromSizeByAge(float? age)
        {
            if (sizeByAgeMult == null || age == null) return 1f;
            if (age == null) return 1;
            return sizeByAgeMult.Evaluate(age.Value);
        }

        public bool CanMorphDown => retromorphUnderAge != null;
        public bool CanMorphUp => metamorphAtAge != null || metamorphIfPregnant || metamorphIfNight || metamorphIfDay;
        public bool CanMorphAtAll => CanMorphDown || CanMorphUp;
        public bool HasMorphTarget => metamorphTarget != null || retromorphTarget != null;
        public bool MorphRelated => CanMorphAtAll || HasMorphTarget;

        public StringBuilder GetAllEffectorDescriptions()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (applyBodyHediff != null)
            {
                foreach (var hdiffToBody in applyBodyHediff)
                {
                    if (hdiffToBody.conditionals != null && hdiffToBody.hediff != null)
                    {
                        string hdiffLbl = hdiffToBody.hediff.label ?? hdiffToBody.hediff.defName;

                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(($"\"{hdiffLbl.CapitalizeFirst()}\" {"BS_ActiveIf".Translate()}:").Colorize(ColoredText.TipSectionTitleColor));
                        foreach (var conditional in hdiffToBody.conditionals)
                        {
                            stringBuilder.AppendLine($"• {conditional.Label}");
                        }
                    }
                }
            }
            if (applyPartHediff != null)
            {
                foreach (var hdiffToParts in applyPartHediff)
                {
                    if (hdiffToParts.conditionals != null && hdiffToParts.hediff != null)
                    {
                        string hdiffLbl = hdiffToParts.hediff.label ?? hdiffToParts.hediff.defName;

                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(($"\"{hdiffLbl.CapitalizeFirst()}\" {"BS_ActiveIf".Translate()}:").Colorize(ColoredText.TipSectionTitleColor));
                        foreach (var conditional in hdiffToParts.conditionals)
                        {
                            stringBuilder.AppendLine($"• {conditional.Label}");
                        }
                    }
                }
            }

            return stringBuilder;
        }

        public List<string> GetImmutableEndoGeneExclusionTags()
        {
            return immutableEndogenes.Where(x=>x.exclusionTags != null).SelectMany(x => x.exclusionTags).ToList();
        }

        public bool IsGeneLegal(GeneDef gene)
        {
            if (gene == null) return true;
            if (AllForcedGenes.Contains(gene)) return true;
            if (geneFilters?.GetFilterResult(gene).Denied() == true) return false;
            if (gene.displayCategory != null &&
                geneCategoryFilters?.GetFilterResult(gene.displayCategory).Denied() == true) return false;
            if (gene.exclusionTags != null && gene.exclusionTags.Count > 1 &&
                geneTagFilters?.GetFilterResultFromItemList(gene.exclusionTags).Denied() == true) return false;
            if (immutableEndogenes != null && !AllForcedGenes.Contains(gene))
            {
                var forcedTags = GetImmutableEndoGeneExclusionTags();
                if (!forcedTags.NullOrEmpty() && !gene.exclusionTags.NullOrEmpty())
                {
                    return !gene.exclusionTags.Any(forcedTags.Contains);
                }
            }
            return true;
        }

        public List<GeneDef> ForcedEndogenes => [.. (forcedEndogenes ?? []), .. (immutableEndogenes ?? [])];
        public List<GeneDef> ForcedXenogenes => forcedXenogenes ?? [];
        public List<GeneDef> AllForcedGenes => [.. ForcedEndogenes, .. ForcedXenogenes];

        //public bool ValidateLists(Pawn pawn)
        //{
        //    bool blockingOwnHediffs = forcedHediffs.Any(h => !IsHediffLegal(h));
        //    if (blockingOwnHediffs)
        //    {
        //        var illegalHediffs = forcedHediffs.Where(h => !IsHediffLegal(h));
        //        Log.Warning($"A filter on {pawn}'s race is forbidding its own hediffs:\nForbidden Hediffs: {illegalHediffs.Select(x => x.defName).ToCommaList()}.");
        //        return false;
        //    }
        //    return true;
        //}
    }

    public class ConsumeSoul
    {
        public float gainMultiplier = 1;
        public float? gainSkillMultiplier = null;
        public float exponentialFalloff = 2.5f;
    }

    public class HediffToBody
    {
        public HediffDef hediff;

        public List<ConditionalStatAffecter> conditionals;
    }

    public class HediffToBodyparts
    {
        public HediffDef hediff;

        public List<ConditionalStatAffecter> conditionals;

        public List<BodyPartDef> bodyparts = new List<BodyPartDef>();
    }

    public class GraphicPathPerBodyType
    {
        public BodyTypeDef bodyType;
        public string graphicPath;
    }

    public class TransformationGene
    {
        public List<string> genesRequired = new List<string>();
        public List<string> genesForbidden = new List<string>();
        public List<string> genesToAdd = new List<string>();
        public string xenotypeToAdd = null;
        public List<string> genesToRemove = new List<string>();

        public bool TryTransform(Pawn pawn, Gene parentGene)
        {
            if (CanTransform(pawn))
            {
                // Remove the parent gene. Without this we'd just keep calling this all the time.
                pawn?.genes?.RemoveGene(parentGene);

                // Check if parentGene is a xenogene
                bool xenoGene = pawn.genes.Xenogenes.Contains(parentGene);

                if (genesToRemove.Count > 0)
                {
                    foreach (var geneName in genesToRemove)
                    {
                        var genesToRemove = pawn?.genes?.GenesListForReading.Where(x => x.def.defName == geneName).ToList();
                        if (genesToRemove != null && genesToRemove.Any())
                        {
                            pawn?.genes?.RemoveGene(genesToRemove.First());
                        }
                    }
                }

                if (xenotypeToAdd != null)
                {
                    var xenotypeToAddDef = DefDatabase<XenotypeDef>.GetNamed(xenotypeToAdd, errorOnFail: false);
                    if (xenotypeToAddDef != null)
                    {
                        // Set pawn.genes.xenotype = xenotype; via reflection
                        var xenotypeField = typeof(Pawn_GeneTracker).GetField("xenotype", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        xenotypeField?.SetValue(pawn.genes, xenotypeToAddDef);
                        pawn.genes.xenotypeName = xenotypeToAddDef.LabelCap;
                        pawn.genes.iconDef = null;
                        for (int i = 0; i < xenotypeToAddDef.genes.Count; i++)
                        {
                            pawn.genes.AddGene(xenotypeToAddDef.genes[i], !xenotypeToAddDef.inheritable);
                        }
                    }
                }
                if (genesToAdd.Count > 0)
                {
                    foreach (var geneName in genesToAdd)
                    {
                        var gene = DefDatabase<GeneDef>.GetNamed(geneName, errorOnFail: false);
                        if (gene != null)
                        {
                            pawn?.genes?.AddGene(gene, xenoGene);
                        }
                    }
                }
                
                return true;
            }
            return false;
        }

        private bool CanTransform(Pawn pawn)
        {
            if (genesRequired.Count > 0)
            {
                foreach (var gene in genesRequired)
                {
                    if (!pawn?.genes?.GenesListForReading?.Any(x => x.def.defName == gene) == true)
                    {
                        return false;
                    }
                }
            }
            if (genesForbidden.Count > 0)
            {
                foreach (var gene in genesForbidden)
                {
                    if (pawn?.genes?.GenesListForReading?.Any(x => x.def.defName == gene) == true)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
