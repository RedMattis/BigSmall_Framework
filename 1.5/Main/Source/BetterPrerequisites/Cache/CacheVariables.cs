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
    public partial class BSCache
    {
        public bool isDefaultCache = false;

        public Pawn pawn = null;
        public bool refreshQueued = false;
        public int? lastUpdateTick = null;
        public int? creationTick = null;
        public bool SameTick => lastUpdateTick == Find.TickManager.TicksGame;

        //public CacheTimer Timer { get; set; } = new CacheTimer();


        public bool isHumanlike = false;
        public ThingDef originalThing = null;
        public HashSet<HediffDef> raceTrackerHistory = [];

        public bool approximatelyNoChange = true;
        public bool hideHead = false;
        public bool hideBody = false;
        public BodyTypesPerGender bodyTypeOverride = null;
        private Gender? apparentGender = null;
        public string bodyGraphicPath = null;
        public string bodyDessicatedGraphicPath = null;
        public string headGraphicPath = null;
        public string headDessicatedGraphicPath = null;
        public CustomMaterial bodyMaterial = null;
        public CustomMaterial headMaterial = null;
        public RotDrawMode? forcedRotDrawMode = null;

        public float bodyRenderSize = 1;
        public float headRenderSize = 1;

        public enum BleedRateState { Unchanged, SlowBleeding, VerySlowBleeding, NoBleeding }

        public float totalSize = 1;
        public float totalCosmeticSize = 1;
        public float totalSizeOffset = 0;
        public PercentChange scaleMultiplier = new(1, 1, 1);
        public PercentChange previousScaleMultiplier = null;
        public PercentChange cosmeticScaleMultiplier = new(1, 1, 1);

        public float healthMultiplier = 1;
        public float healthMultiplier_previous = 1;
        public float internalDamageDivisor = 1;

        public Dictionary<ThingDef, bool> willEatDef = [];

        public float minimumLearning = 0;
        public float growthPointGain = 1;
        //public float foodNeedCapacityMult = 1;
        //public float? previousFoodCapacity = null;

        public bool preventHeadScaling = false;
        public bool bodyConstantHeadScale = false;
        public bool bodyConstantHeadScaleBigOnly = false;
        public float preventHeadScalingFactor = 1f;
        public float preventHeadOffsetFactor = 1f;
        public float headSizeMultiplier = 1;
        public float headPositionMultiplier = 1;
        public float worldspaceOffset = 0;

        /// <summary>
        /// If populated should always have 4 items, one for each rotation.
        /// </summary>
        public List<Vector3> complexHeadOffsets = null;
        public List<Vector3> complexBodyOffsets = null;
        public bool hasComplexHeadOffsets = false;

        /// <summary>
        /// This one returns true on stuff like bloodless pawns just so they can't have blood drained from them.
        /// </summary>
        public bool isBloodFeeder = false;
        public bool hasSizeAffliction = false;
        public float attackSpeedMultiplier = 1;
        public float attackSpeedUnarmedMultiplier = 1;
        public float alcoholAmount = 0;
        public RomanceTags romanceTags = null;

        public ApparelRestrictions apparelRestrictions = null;
        //public bool canWearApparel = true;
        //public bool canWearClothing = true;
        //public bool canWearArmor = true;

        public bool injuriesRescaled = false;
        public bool isUnliving = false;
        public BleedRateState bleedRate = BleedRateState.Unchanged;
        public float bleedRateFactor = 1;
        public bool slowBleeding = false;
        public bool deathlike = false;
        public bool isMechanical = false;

        /// <summary>
        /// Banns addictions that are not whitelisted or better.
        /// </summary>
        public bool banAddictions = false;
        public bool willBeUndead = false;
        public bool unarmedOnly = false;
        public bool succubusUnbonded = false;
        public float pregnancySpeed = 1;
        public bool everFertile = false;
        public bool animalFriend = false;
        public float apparentAge = 30;

        public DevelopmentalStage developmentalStage = DevelopmentalStage.None;

        public float raidWealthOffset = 0;
        public float raidWealthMultiplier = 1;

        public float bodyPosOffset = 0;
        //public float headPosMultiplier = 1;

        public bool preventDisfigurement = false;

        public bool renderCacheOff = false;

        //public FoodKind diet = FoodKind.Any;
        public List<NewFoodCategory> newFoodCatAllow = null;
        public List<NewFoodCategory> newFoodCatDeny = null;
        public List<PawnDiet> pawnDiet = [];

        public List<ApparelCache> apparelCaches = [];

        // Color and Fur Transform cache
        public Color? savedSkinColor = null;
        public Color? savedHairColor = null;
        public string savedFurSkin = null;
        public string savedBodyDef = null;
        public string savedHeadDef = null;
        //public bool disableBeards = false;
        public string savedBeardDef = null;
        public string savedHairDef = null;


        public int? randomPickSkinColor = null;
        public int? randomPickHairColor = null;

        public bool facialAnimationDisabled = false;
        public bool facialAnimationDisabled_Transform = false; // Used for the ColorAndFur Hediff.
        public FacialAnimDisabler facialAnimDisabler = null;

        public bool disableLookChangeDesired = false;

        public bool isDrone = false;
        public List<Aptitude> aptitudes = [];

        public List<GeneDef> endogenesRemovedByRace = [];
        public List<GeneDef> xenoenesRemovedByRace = [];

        public string id = "BS_DefaultID";

        public void ExposeData()
        {

            // Scribe Pawn
            Scribe_Values.Look(ref id, "BS_CachePawnID", defaultValue: "BS_DefaultCahced");

            Scribe_Defs.Look(ref originalThing, "BS_OriginalThing");

            // Scribe Values
            Scribe_Values.Look(ref healthMultiplier, "BS_HealthMultiplier", 1);
            Scribe_Values.Look(ref healthMultiplier_previous, "BS_HealthMultiplier_Previous", 1);

            Scribe_Values.Look(ref bodyRenderSize, "BS_BodyRenderSize", 1);
            Scribe_Values.Look(ref headRenderSize, "BS_HeadRenderSize", 1);
            Scribe_Values.Look(ref totalSize, "BS_TotalSize", 1);
            Scribe_Values.Look(ref totalCosmeticSize, "BS_TotalCosmeticSize", 1);
            Scribe_Deep.Look(ref scaleMultiplier, "BS_ScaleMultiplier");
            Scribe_Deep.Look(ref previousScaleMultiplier, "BS_PreviousScaleMultiplier");
            Scribe_Deep.Look(ref cosmeticScaleMultiplier, "BS_CosmeticScaleMultiplier");
            Scribe_Values.Look(ref totalSizeOffset, "BS_SizeOffset", 0);
            Scribe_Values.Look(ref isHumanlike, "BS_IsHumanlike", false);
            Scribe_Values.Look(ref headPositionMultiplier, "BS_HeadPositionMultiplier", 1);

            Scribe_Values.Look(ref hideHead, "BS_HideHead", false);
            Scribe_Values.Look(ref hideBody, "BS_HideBody", false);

            Scribe_Values.Look(ref minimumLearning, "BS_MinimumLearning", 0.35f);
            Scribe_Values.Look(ref headSizeMultiplier, "BS_HeadSizeMultiplier", 1);
            Scribe_Values.Look(ref isBloodFeeder, "BS_IsBloodFeeder", false);
            Scribe_Values.Look(ref hasSizeAffliction, "BS_HasSizeAffliction", false);
            Scribe_Values.Look(ref attackSpeedMultiplier, "BS_AttackSpeedMultiplier", 1);
            Scribe_Values.Look(ref alcoholAmount, "BS_AlcoholAmount", 0);

            Scribe_Values.Look(ref isUnliving, "BS_IsUnliving", false);
            Scribe_Values.Look(ref bleedRate, "BS_BleedRate", BleedRateState.Unchanged);
            Scribe_Values.Look(ref slowBleeding, "BS_SlowBleeding", false);
            Scribe_Values.Look(ref deathlike, "BS_Deathlike", false);
            Scribe_Values.Look(ref isMechanical, "BS_IsMechanical", false);
            Scribe_Values.Look(ref willBeUndead, "BS_WillBeUndead", false);
            Scribe_Values.Look(ref unarmedOnly, "BS_UnarmedOnly", false);
            Scribe_Values.Look(ref succubusUnbonded, "BS_SuccubusUnbonded", false);
            Scribe_Values.Look(ref pregnancySpeed, "BS_FastPregnancy", 1f);
            Scribe_Values.Look(ref everFertile, "BS_EverFertile", false);
            Scribe_Values.Look(ref apparentAge, "BS_ApparentAge", 30);

            // Between Sessions Save Required
            Scribe_Values.Look(ref injuriesRescaled, "BS_InjuriesRescaled", false);
            Scribe_Collections.Look(ref apparelCaches, "BS_ApparelCaches", LookMode.Deep);
            Scribe_Values.Look(ref preventDisfigurement, "BS_PreventDisfigurement", false);
            Scribe_Values.Look(ref renderCacheOff, "BS_RenderCacheOff", false);
            Scribe_Values.Look(ref savedSkinColor, "BS_SavedSkinColor", null);
            Scribe_Values.Look(ref savedHairColor, "BS_SavedHairColor", null);
            Scribe_Values.Look(ref savedBeardDef, "BS_SavedBeardDef", null);
            Scribe_Values.Look(ref randomPickSkinColor, "BS_RandomPickSkinColor", null);
            Scribe_Values.Look(ref randomPickHairColor, "BS_RandomPickHairColor", null);
            Scribe_Values.Look(ref facialAnimationDisabled, "BS_FacialAnimationDisabled", false);
            Scribe_Values.Look(ref savedFurSkin, "BS_SavedFurskinName");
            Scribe_Values.Look(ref savedBodyDef, "BS_SavedBodyDefName");
            Scribe_Values.Look(ref savedHeadDef, "BS_SavedHeadDefName");
            Scribe_Collections.Look(ref endogenesRemovedByRace, "BS_EndogenesRemovedByRace", LookMode.Def);
            Scribe_Collections.Look(ref xenoenesRemovedByRace, "BS_XenoenesRemovedByRace", LookMode.Def);


            const bool debugMode = true;
            if (debugMode)
            {
                // Food
                Scribe_Collections.Look(ref newFoodCatAllow, "BS_NewFoodCatAllow", LookMode.Def);
                Scribe_Collections.Look(ref newFoodCatDeny, "BS_NewFoodCatDeny", LookMode.Def);
                Scribe_Collections.Look(ref pawnDiet, "BS_PawnDiet", LookMode.Def);

                // Other
                Scribe_Values.Look(ref animalFriend, "BS_AnimalFriend", false);
                Scribe_Values.Look(ref raidWealthOffset, "BS_RaidWealthOffset", 0);
                Scribe_Values.Look(ref raidWealthMultiplier, "BS_RaidWealthMultiplier", 1);
                Scribe_Values.Look(ref isDrone, "BS_IsDrone", false);
            }
        }
    }
}
