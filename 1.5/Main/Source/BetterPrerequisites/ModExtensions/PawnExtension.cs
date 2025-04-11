using BigAndSmall;
using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace BetterPrerequisites
{
    // GeneExtension is literally just here to avoid breaking old XML refering to it by the old name.
    public class GeneExtension : PawnExtension { }
}

namespace BigAndSmall
{

    public class PawnExtension : SmartExtension
    {

        // Used for race-defaults.
        public static PawnExtension defaultPawnExtension = new();

        public string traitIcon = null;

        /// <summary>
        /// The order in which this extension is applied.
        /// Higher numbers are applied later, which means they can in some cases overwrite earlier extensions.
        /// </summary>
        public int priority = 0;

        /// <summary>
        /// Used by PGenes. If it evaluates to False the gene will disable itself.
        /// Useful for werewolf-like beaviour, or genes that deactivate if not drunk or something.
        /// </summary>
        public List<ConditionalStatAffecter> conditionals = null;
        public string ConditionalDescription =>
                conditionals?.Select(x => $"{x.Label}:\n" +
                $"{x.statFactors?.Select(y => y.ToString()).ToLineList("  - ", capitalizeItems: true)}" +
                $"{x.statOffsets?.Select(y => y.ToString()).ToLineList("  - ", capitalizeItems: true)}").ToLineList();

        /// <summary>
        /// Used by the above. Inverts the conditional
        /// </summary>
        public bool? invert;
        /// <summary>
        /// Turns off the zoomed-out render cache for the pawn. Useful if your pawn graphics might otherwise get cut off.
        /// </summary>
        public bool renderCacheOff = false;

        public List<RacialFeatureDef> racialFeatures = null;

        public List<TaggedString> RacialFeaturesDescription => racialFeatures?.Select(x => x.LabelCap).ToList();

        /// <summary>
        /// Used by Genes. When the gene is added/activated it will apply these hediffs to the pawn.
        /// </summary>
        public List<HediffToBody> applyBodyHediff;
        public List<string> ApplyBodyHediffDescription => applyBodyHediff?.Where(x=>x.conditionals == null)?
            .Select(x => (string)"BS_ApplyBodyHediff".Translate(x.hediff.LabelCap)).ToList();
        /// <summary>
        /// Same as above but applies to specific bodyparts.
        /// </summary>
        public List<HediffToBodyparts> applyPartHediff;
        public List<string> ApplyPartHediffDescription => applyPartHediff?.Where(x => x.conditionals == null)?
            .Select(x => (string)"BS_ApplyPartHediff".Translate(x.hediff.LabelCap, string.Join(", ", x.bodyparts))).ToList();

        /// <summary>
        /// This is the magic thing that makes the pawn swap to a different ThingDef. E.g. "Race".
        /// </summary>
        public ThingDef thingDefSwap = null;
        public string ThingDefSwapDescription => thingDefSwap == null ? null : $"BS_ThingDefSwapDesc".Translate(thingDefSwap.LabelCap).CapitalizeFirst();

        /// <summary>
        /// Forces the swap to the ThingDef. If false, it will be cautious to avoid accidentally turning robots into
        /// biological snake-people, etc. If in doubt, leave this as false.
        /// </summary>
        public bool forceThingDefSwap = false;

        /// <summary>
        /// Used by Genes. A bit of a miseleading name.
        /// The listed genes will be added to the pawn when this gene is added.
        /// When removed the genes will be removed as well.
        /// 
        /// It was originally used to make genes with multiple sets of graphics, but it can be used nowadays to "bundle" genes.
        /// </summary>
        public List<GeneDef> hiddenGenes = [];
        /// <summary>
        /// All the listed thoughts will be nulled.
        /// 
        /// It is suggested to not add too many of these to a gene since they WILL show in the tooltip. Hediffs don't have this issue.
        /// </summary>
        public List<ThoughtDef> nullsThoughts = null;

        /// <summary>
        /// Sets what apparel the pawn can wear. Check in the class for more details.
        /// </summary>
        public ApparelRestrictions apparelRestrictions = null;

        public bool? canWieldThings = null;

        /// <summary>
        /// Transforms the pawn into a specific xenotype under some conditions.
        /// Currently used by the hidden Grigori gene to trigger potential Nephilim transformation in hybrid kids.
        /// </summary>
        public TransformationGene transformGene = null;

        /// <summary>
        /// Applies a size curve to the age of the pawn. Can be used to change how big they are at different ages.
        /// </summary>
        /// 
        public SimpleCurve sizeByAge = null;
        public string SizeByAgeDescription => sizeByAge?.Select((CurvePoint pt) => "PeriodYears".Translate(pt.x).ToString() + ": +" + pt.y.ToString()).ToLineList("  - ", capitalizeItems: true);

        /// <summary>
        /// Same as above, but multiplies the size by the curve.
        /// </summary>
        public SimpleCurve sizeByAgeMult = null;
        public string SizeByAgeMultDescription => sizeByAgeMult?.Select((CurvePoint pt) => "PeriodYears".Translate(pt.x).ToString() + ": x" + pt.y.ToString()).ToLineList("  - ", capitalizeItems: true);

        /// <summary>
        /// Works about the same as Rimworld's aptitudes. Let's you add skill-offsets to genes/hediffs/races.
        /// </summary>
        public List<Aptitude> aptitudes = null;
        public List<string> AptitudeDescription => aptitudes?.Select((Aptitude x) => x.skill.LabelCap.ToString() + " " + x.level.ToStringWithSign()).ToList();

        public float? bleedRate = null;

        public string BleedRateDescription => bleedRate == null ? null : "BS_BleedRateDesc".Translate(bleedRate.Value.ToStringPercent());
        //         public string ForceUnarmedDescription => forceUnarmed ? "BS_ForceUnarmedDesc".Translate() : null;
        #region Rendering
        /// <summary>
        /// Prevents head-scaling/offsets from sources other than the pawn's general size.
        /// </summary>
        public bool preventHeadScaling = false;

        /// <summary>
        /// Prevents auto-scaling the pawn's head from changing body-size sources other than explicit headscale.
        /// This means large pawns won't get slightly smaller heads, and small ones won't get a chibi-head.
        /// 
        /// Basically just a more specific version of the above.
        /// </summary>
        public bool bodyConstantHeadScale = false;

        /// <summary>
        /// Exactly the same as the above, but permits big head if the body is small.
        /// </summary>
        public bool bodyConstantHeadScaleBigOnly = false;

        /// <summary>
        /// If specified, instead of blocking scale it will be reduced by this factor.
        /// </summary>
        public float? preventHeadScalingFactor = null;

        public float? preventHeadOffsetFactor = null;

        /// <summary>
        /// Sets a custom material for the body. Highly versatile.
        /// </summary>
        public CustomMaterial bodyMaterial = null;
        /// <summary>
        /// Sets a custom material for the head. Highly versatile.
        /// </summary>
        public CustomMaterial headMaterial = null;
        /// <summary>
        /// Sets the path(s) of the body. Can be per body type, gender, etc.
        /// You can for example set a list of paths to be used by male hulks only.
        /// </summary>
        public AdaptivePathPathList bodyPaths = [];
        public AdaptivePathPathList bodyDessicatedPaths = [];
        public bool removeTattoos = false;

        public RotDrawMode? forcedRotDrawMode = null;
        /// <summary>
        /// Same as above.
        /// </summary>
        public AdaptivePathPathList headPaths = [];
        public AdaptivePathPathList headDessicatedPaths = [];

        /// <summary>
        /// If set this lets you disabe a renderNode type on the pawn.
        /// </summary>
        public bool hideHumanlikeRenderNodes = false;

        public bool hideBody = false;
        public bool hideHead = false;
        /// <summary>
        /// Makes the pawn consider the pawn female for rendering purposes.
        /// Useful for compatibility. Despite the name, it also affects the head.
        /// </summary>
        public Gender? forceGender = null;
        protected bool forceFemaleBody = false;

        public bool ignoreForceGender = false;

        protected Gender? apparentGender = null;
        public bool invertApparentGender = false;
        public Gender? ApparentGender => forceFemaleBody ? Gender.Female : apparentGender;

        public BodyTypesPerGender bodyTypes = [];
        #endregion

        /// <summary>
        /// Set what the pawn can eat. By default assumes human diet.
        /// </summary>
        public PawnDiet pawnDiet = null;
        public string PawnDietDescription => pawnDiet?.LabelCap;
        /// <summary>
        /// If true this will make the race's "PawnDiet" be ignored in favor of other diets.
        /// 
        /// </summary>
        public bool pawnDietRacialOverride = false;
        public float? internalDamageDivisor = 1;

        #region Birth
        /// <summary>
        /// How many babies the pawn will give birth to. If null, it will use normal counts.
        /// Note that this is better than the vanilla options since it will re-randomize the gene list for each additional
        /// baby instead of making them clones.
        /// </summary>
        public List<int> babyBirthCount = null;
        /// <summary>
        /// What "practical" age the baby born at. Creatures that give birth to less helpless offspring may want to start at 3.
        /// Insects that are born fully formed could start at 10, 13, or even 20.
        /// Consider combining with sizeByAgeMult to make the babies a plausible size.
        /// </summary>
        public int? babyStartAge = null;
        /// <summary>
        /// How fast pregnancy progresses. 1 is normal, 0.5 is half speed, 2 is double speed.
        /// </summary>
        public float pregnancySpeedMultiplier = 1;
        #endregion

        /// Stats
        public float soulFalloffStart = 0;
        public string SoulPowerFalloffStartDescription => soulFalloffStart == 0 ? null : "BS_SoulPowerFalloffDesc".Translate(soulFalloffStart.ToStringPercentSigned());

        public List<string> StatChangeDescriptions => [.. new List<string>
        {
            SoulPowerFalloffStartDescription,
            // TODO: Add other stats here later.
        }.Where(x => x != null)];

        /// <summary>
        /// Offsets the entire pawn's body up or down.
        /// </summary>
        public float bodyPosOffset = 0f;

        /// <summary>
        /// Offsets the head up or down relative to the body.
        /// </summary>
        public float headPosMultiplier = 0f; // Actually an offset to the multiplier

        /// <summary>
        /// Only the basic offsets will be respected at the moment. Supporting all is a performance hit.
        /// </summary>
        public BSDrawData bodyDrawData = null;
        public BSDrawData headDrawData = null;

        public bool preventDisfigurement = false;
        public string PreventDisfigurementDescription => preventDisfigurement ? "BS_PreventDisfigurementDesc".Translate() : null;
        /// <summary>
        /// Lets the pawn walk on VFE's creep. Only works on genes.
        /// </summary>
        public bool canWalkOnCreep = false;
        public string CanWalkOnCreepDescription => canWalkOnCreep ? "BS_CanWalkOnCreepDesc".Translate() : null;

        public RomanceTags romanceTags = null;
        public List<string> RomanceTagsDescription => romanceTags?.GetDescriptions();

        /// <summary>
        /// Can hold melee weapons, but will only use natural/bionic attacks.
        /// </summary>
        public bool forceUnarmed = false;
        public string ForceUnarmedDescription => forceUnarmed ? "BS_ForceUnarmedDesc".Translate() : null;

        public bool disableLookChangeDesired = false;

        public bool hideInGenePicker = false;
        public bool hideInXenotypeUI = false;
        /// <summary>
        /// Trigger the soul-consume effect on hit.
        /// </summary>
        public ConsumeSoul consumeSoulOnHit = null;
        public string ConsumeSoulOnHitDescription => consumeSoulOnHit == null ? null : "BS_ConsumeSoulOnHitDesc".Translate($"{100 * consumeSoulOnHit.gainMultiplier:f0}%");

        /// <summary>
        /// If set the pawn will be butchered for this meat instead of the default.
        /// </summary>
        public ThingDef meatOverride;

        /// <summary>
        /// If set the following extra products will be created when the pawn is butchered.
        /// </summary>
        public List<CustomButcherProduct> butcherProducts = null;

        /// <summary>
        /// Pawn will be considered...
        /// </summary>
        public bool isUnliving = false;
        public bool isDeathlike = false;
        public bool isMechanical = false;
        public bool isBloodfeeder = false;
        /// <summary>
        /// Makes colonists care less about the pawn's death, and the pawn care less about death in general.
        /// </summary>
        public bool isDrone = false;
        public bool noFamilyRelations = false;
        public string NoFamilyRelationsDescription => noFamilyRelations ? "BS_NoFamilyRelationsDesc".Translate() : null;
        public string DroneDescription => isDrone ? "BS_DroneDesc".Translate() : null;
        private string UnlivingDescription => isUnliving ? "BS_UnlivingDesc".Translate() : null;
        private string DeathlikeDescription => isDeathlike ? "BS_DeathlikeDesc".Translate() : null;
        private string MechanicalDescription => isMechanical ? "BS_MechanicalDesc".Translate() : null;
        private string BloodfeederDescription => isBloodfeeder ? "BS_BloodfeederDesc".Translate() : null;
        public List<string> TagDescriptions => new List<string> { UnlivingDescription, DeathlikeDescription, MechanicalDescription, BloodfeederDescription, DroneDescription }
                .Where(x => x != null).ToList();


        /// <summary>
        /// If set to true the pawn will default to blocking additctions unless white/allowlisted.
        /// </summary>
        public bool banAddictionsByDefault = false;

        // Metamorph Stuff.

        /// <summary>
        /// Target to (possibly) morph to.
        /// </summary>
        public XenotypeDef metamorphTarget = null;
        /// <summary>
        /// Same as above, but for morphing "backwards". Currently only used for juvenlie forms based on age.
        /// E.g. Queens giving birth to drones.
        /// </summary>
        public XenotypeDef retromorphTarget = null;
        /// <summary>
        /// Trigger Metamorph at this age.
        /// </summary>
        public int? metamorphAtAge = null;
        /// <summary>
        /// Trigger Retromorph if less than this age.
        /// </summary>
        public int? retromorphUnderAge = null;
        public bool metamorphIfPregnant = false;
        public bool metamorphIfNight = false;
        public bool metamorphIfDay = false;

        // Locked Needs.
        /// <summary>
        /// Lock a Needbar at a certain level. Often more compatible than just removing them.
        /// </summary>
        public List<BetterPrerequisites.LockedNeedClass> lockedNeeds;
        public string LockedNeedsDescription => lockedNeeds?.Where(x=>x!=null && x.GetLabel() != "").Select(x => x.GetLabel())?.ToLineList("  - ", capitalizeItems: true);


        /// <summary>
        /// Disable Nal's facial animations on the pawn and restore their original head.
        /// </summary>
        public bool disableFacialAnimations = false;

        /// <summary>
        /// More granular version of the above. Currently not working after a Nal's update.
        /// </summary>
        public FacialAnimDisabler facialDisabler = null;

        public bool frequentUpdate = false;

        #region Obsolete
        public bool unarmedOnly = false;    // Still plugged in, but the name was kind of bad. Use forceUnarmed instead.
        #endregion

        // Some of these are RACE ONLY. Use elsewhere at your own risk.

        #region
        /// <summary>
        /// Force-adds these hediffs. Removes when race is removed.
        /// </summary>
        public List<HediffDef> forcedHediffs = [];
        public List<string> RaceForcedHediffsDesc => forcedHediffs.NullOrEmpty() ? null :
            forcedHediffs?.Select(x => (string)"BS_ApplyBodyHediff".Translate(x.LabelCap)).ToList();

        /// <summary>
        /// Force-adds these traits.
        /// </summary>
        public List<TraitDef> forcedTraits = [];

        public List<TraitDef> traitsDependentOnRace = [];
        /// <summary>
        /// Adds endogenes to the pawn. Ensures they are always present.
        /// </summary>
        public List<GeneDef> forcedEndogenes = null;
        /// <summary>
        /// Same as above, but also removes anything that would overwrite them
        /// </summary>
        public List<GeneDef> immutableEndogenes = null;
        /// <summary>
        /// Adds xenogenes to the pawn. Ensures they are always present.
        /// </summary>
        public List<GeneDef> forcedXenogenes = null;

        public List<GeneDef> genesDependentOnRace = [];
        #endregion

        /// <summary>
        /// Filters (removes) based on filtering settings
        /// </summary>
        #region White, Black, and Allow-lists.
        public FilterListSet<GeneDef> geneFilters = null;
        public FilterListSet<GeneCategoryDef> geneCategoryFilters = null;
        public FilterListSet<string> geneTagFilters = null;

        /// <summary>
        /// These filters don't remove the gene, but simply block it from being activated.
        /// </summary>
        public FilterListSet<GeneDef> activeGeneFilters = null;
        public FilterListSet<GeneCategoryDef> activeGeneCategoryFilters = null;
        public FilterListSet<string> activeGeneTagFilters = null;

        public FilterListSet<TraitDef> traitFilters = null;

        public FilterListSet<HediffDef> hediffFilters = null;
        public FilterListSet<HairDef> hairFilters = null;
        public FilterListSet<RecipeDef> surgeryRecipes = null;
        //public AllowListHolder<ThingDef> allowedFood = null;  // Not yet implemented. Easy enough, just not needed yet.
        #endregion

        /// <summary>
        /// These are just for generating pawns. They are most useful on custom races, not on genes/hediffs.
        /// Don't forget that is also inherits the props from "CompProperties_ColorAndFur".
        /// 
        /// Should work even without biotech
        /// </summary>;
        public List<GeneDef> randomSkinGenes = null;
        public List<GeneDef> randomHairGenes = null;

        #region tags


        #endregion

        public bool HasActiveGeneFilters => activeGeneFilters != null || activeGeneCategoryFilters != null || activeGeneTagFilters != null;
        public bool HasGeneRemovalFilters => geneFilters != null || geneCategoryFilters != null || geneTagFilters != null;
        public bool HasGeneFilters => HasGeneRemovalFilters || HasActiveGeneFilters;

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

        public bool RequiresCacheRefresh()
        {
            return aptitudes != null;
        }

        public StringBuilder GetAllEffectorDescriptions()
        {
            StringBuilder stringBuilder = new();
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
                            stringBuilder.AppendLine($"  - {conditional.Label}");
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
                            stringBuilder.AppendLine($"  - {conditional.Label}");
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

        /// <summary>
        /// Checks if a gene is legal for the pawn.
        /// </summary>
        /// <param name="gene"></param>
        /// <param name="removalCheck">If true the gene will be removed when failing legality check. Otherwise it will just be disabled.</param>
        /// <returns></returns>
        public FilterResult IsGeneLegal(GeneDef gene, bool removalCheck)
        {
            if (gene == null) return FilterResult.ForceAllow;
            if (AllForcedGenes.Contains(gene)) return FilterResult.ForceAllow;
            
            if (immutableEndogenes != null && !AllForcedGenes.Contains(gene))
            {
                var forcedTags = GetImmutableEndoGeneExclusionTags();
                if (!forcedTags.NullOrEmpty() && !gene.exclusionTags.NullOrEmpty() && gene.exclusionTags.Any(forcedTags.Contains))
                {
                    return FilterResult.Banned;
                }
            }
            List<FilterResult> results = [FilterResult.Neutral];
            results.Add(geneFilters?.GetFilterResult(gene) ?? FilterResult.Neutral);
            if (gene.displayCategory != null) results.Add(geneCategoryFilters?.GetFilterResult(gene.displayCategory) ?? FilterResult.Neutral);
            if (gene.exclusionTags != null) results.Add(geneTagFilters?.GetFilterResultFromItemList(gene.exclusionTags) ?? FilterResult.Neutral);
            if (gene.descriptionHyperlinks != null) results.Add(geneTagFilters?.GetFilterResultFromItemList(gene.descriptionHyperlinks
                .Where(x=>x.def is DefTag).Select(x=>x.def.defName).ToList()) ?? FilterResult.Neutral);

            if (!removalCheck)
            {
                results.Add(activeGeneFilters?.GetFilterResult(gene) ?? FilterResult.Neutral);
                if (gene.displayCategory != null) results.Add(activeGeneCategoryFilters?.GetFilterResult(gene.displayCategory) ?? FilterResult.Neutral);
                if (gene.exclusionTags != null) results.Add(activeGeneTagFilters?.GetFilterResultFromItemList(gene.exclusionTags) ?? FilterResult.Neutral);
                if (gene.descriptionHyperlinks != null) results.Add(activeGeneTagFilters?.GetFilterResultFromItemList(gene.descriptionHyperlinks
                    .Where(x => x.def is DefTag).Select(x => x.def.defName).ToList()) ?? FilterResult.Neutral);
            }
            return results.Fuse();
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

    
}
