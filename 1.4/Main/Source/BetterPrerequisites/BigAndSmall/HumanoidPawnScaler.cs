using BetterPrerequisites;
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
    public class BigAndSmallCache : GameComponent
    {
        public static List<BSCache> scribedCache = new List<BSCache>();
        public Game game;
        public BigAndSmallCache(Game game)
        {
            this.game = game;
        }
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref scribedCache, saveDestroyedThings:false, "BS_scribedCache", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                scribedCache = scribedCache.Distinct().ToList();
                HumanoidPawnScaler.Cache.Clear();

                var allPawns = game?.Maps?.SelectMany(x => x.mapPawns.AllPawns);
                // Get all pawns from the game and add them to the cache
                foreach (var pawn in allPawns)
                {
                    var id = pawn.ThingID;
                    foreach (var cache in scribedCache.Where(x => x.id == id))
                    {
                        HumanoidPawnScaler.Cache[pawn] = cache;
                    }
                }
            }
        }
    }

    public class HumanoidPawnScaler : DictCache<Pawn, BSCache>
    {
        /// <summary>
        /// Note that if the pawn is "null" then it will use a generic cache with default values. This is mostly to just get rid of the need to
        /// null-check everything that calls this method.
        /// </summary>
        /// <returns></returns>
        public static BSCache GetBSDict(Pawn pawn, bool forceRefresh = false)
        {
            if (pawn == null)
            {
                return BSCache.defaultCache;
            }

            bool newEntry;
            BSCache result;
            if (RunNormalCalculations(pawn))
            {
                result = GetCache(pawn, out newEntry, forceRefresh: forceRefresh);
            }
            else
            {
                // Unless values have already been set, this will just be a cache with default values.
                result = GetCache(pawn, out newEntry, forceRefresh, forceDefault: true);
            }
            if (newEntry)
            {
                // Check if the new entry is actually new or if there is a scribed entry to load instead.
                var scribedResult = CheckForScribedEntry(pawn);
                if (scribedResult != null)
                {
                    result = scribedResult;
                }
                else
                {
                    BigAndSmallCache.scribedCache.Add(result);
                }
            }

            return result;
        }

        public static BSCache CheckForScribedEntry(Pawn pawn)
        {
            var id = pawn.ThingID;
            foreach (var cache in BigAndSmallCache.scribedCache.Where(x => x.id == id))
            {
                Cache[pawn] = cache;
                cache.pawn = pawn;
                return cache;
            }
            return null;
        }

        public static bool RunNormalCalculations(Pawn pawn)
        {
            return pawn != null
                && BigSmall.performScaleCalculations
                && (pawn.RaceProps.Humanlike || BigSmallMod.settings.scaleAnimals)
                && (pawn.needs != null || pawn.Dead);
        }
    }

    public class BSCache : IExposable, ICacheable
    {
        public static BSCache defaultCache = new BSCache();

        public Pawn pawn = null;
        public CacheTimer Timer { get; set; } = new CacheTimer();

        public float bodyRenderSize = 1;
        public float headRenderSize = 1;

        public enum BleedRateState { Unchanged, SlowBleeding, VerySlowBleeding, NoBleeding }

        public float totalSize = 1;
        public float totalCosmeticSize = 1;
        public PercentChange scaleMultiplier = new PercentChange(1, 1, 1);
        public PercentChange previousScaleMultiplier = null;
        public PercentChange cosmeticScaleMultiplier = new PercentChange(1, 1, 1);
        public float sizeOffset = 0;
        public float minimumLearning = 0;
        public float foodNeedCapacityMult = 1;
        public float? previousFoodCapacity = null;
        public float headSizeMultiplier = 1;

        /// <summary>
        /// This one returns true on stuff like bloodless pawns just so they can't have blood drained from them.
        /// </summary>
        public bool isBloodFeeder = false;
        public bool hasSizeAffliction = false;
        public float attackSpeedMultiplier = 1;
        public float psychicSensitivity = 1;
        public float alcoholmAmount = 0;

        public bool canWearApparel = true;
        public bool canWearClothing = true;
        public bool canWearArmor = true;

        public bool injuriesRescaled = false;
        public bool isUnliving = false;
        public BleedRateState bleedRate = BleedRateState.Unchanged;
        public bool slowBleeding = false;
        public bool deathlike = false;
        public bool willBeUndead = false;
        public bool unarmedOnly = false;
        public bool succubusUnbonded = false;
        public bool fastPregnancy = false;
        public bool everFertile = false;
        public bool animalFriend = false;

        public float raidWealthOffset = 0;
        public float raidWealthMultiplier = 1;

        public float bodyPosOffset = 0;
        public float headPosMultiplier = 1;

        public FoodKind diet = FoodKind.Any;

        public string id = "BS_DefaultID";

        // Default Comparer function
        public static bool Compare(BSCache a, BSCache b)
        {
            return a.id == b.id;
        }

        public void ExposeData()
        {
            // Scribe Pawn
            Scribe_Values.Look(ref id, "BS_CachePawnID", defaultValue:"BS_DefaultCahced");
            
            // Scribe Values
            Scribe_Values.Look(ref bodyRenderSize, "BS_BodyRenderSize", 1);
            Scribe_Values.Look(ref headRenderSize, "BS_HeadRenderSize", 1);
            Scribe_Values.Look(ref totalSize, "BS_TotalSize", 1);
            Scribe_Values.Look(ref totalCosmeticSize, "BS_TotalCosmeticSize", 1);
            Scribe_Deep.Look(ref scaleMultiplier, "BS_ScaleMultiplier");
            Scribe_Deep.Look(ref previousScaleMultiplier, "BS_PreviousScaleMultiplier", null);
            Scribe_Deep.Look(ref cosmeticScaleMultiplier, "BS_CosmeticScaleMultiplier");
            Scribe_Values.Look(ref sizeOffset, "BS_SizeOffset", 0);
            Scribe_Values.Look(ref minimumLearning, "BS_MinimumLearning", 0);
            Scribe_Values.Look(ref foodNeedCapacityMult, "BS_FoodNeedCapacityMult", 1);
            Scribe_Values.Look(ref previousFoodCapacity, "BS_PreviousFoodCapacity", null);
            Scribe_Values.Look(ref headSizeMultiplier, "BS_HeadSizeMultiplier", 1);
            Scribe_Values.Look(ref isBloodFeeder, "BS_IsBloodFeeder", false);
            Scribe_Values.Look(ref hasSizeAffliction, "BS_HasSizeAffliction", false);
            Scribe_Values.Look(ref attackSpeedMultiplier, "BS_AttackSpeedMultiplier", 1);
            Scribe_Values.Look(ref psychicSensitivity, "BS_PsychicSensitivity", 1);
            Scribe_Values.Look(ref alcoholmAmount, "BS_AlcoholAmount", 0);
            Scribe_Values.Look(ref canWearApparel, "BS_CanWearApparel", true);
            Scribe_Values.Look(ref canWearClothing, "BS_CanWearClothing", true);
            Scribe_Values.Look(ref canWearArmor, "BS_CanWearArmor", true);
            Scribe_Values.Look(ref injuriesRescaled, "BS_InjuriesRescaled", false);
            Scribe_Values.Look(ref isUnliving, "BS_IsUnliving", false);
            Scribe_Values.Look(ref bleedRate, "BS_BleedRate", BleedRateState.Unchanged);
            Scribe_Values.Look(ref slowBleeding, "BS_SlowBleeding", false);
            Scribe_Values.Look(ref deathlike, "BS_Deathlike", false);
            Scribe_Values.Look(ref willBeUndead, "BS_WillBeUndead", false);
            Scribe_Values.Look(ref unarmedOnly, "BS_UnarmedOnly", false);
            Scribe_Values.Look(ref succubusUnbonded, "BS_SuccubusUnbonded", false);
            Scribe_Values.Look(ref fastPregnancy, "BS_FastPregnancy", false);
            Scribe_Values.Look(ref everFertile, "BS_EverFertile", false);
            Scribe_Values.Look(ref animalFriend, "BS_AnimalFriend", false);
            Scribe_Values.Look(ref raidWealthOffset, "BS_RaidWealthOffset", 0);
            Scribe_Values.Look(ref raidWealthMultiplier, "BS_RaidWealthMultiplier", 1);
            Scribe_Values.Look(ref bodyPosOffset, "BS_BodyPosOffset", 0);
            Scribe_Values.Look(ref headPosMultiplier, "BS_HeadPosMultiplier", 1);
            Scribe_Values.Look(ref diet, "BS_Diet", FoodKind.Any);
        }

        // Used for the Scribe.
        public BSCache()
        {
        }

        public BSCache(Pawn pawn)
        {
            this.pawn = pawn;
            id = pawn.ThingID;
        }

        public static bool regenerationInProgress = false;

        public bool RegenerateCache()
        {
            if (pawn == null) { throw new Exception("Big & Small: Cannot regenerate Pawn Cache because the Pawn is null.");}

            if (regenerationInProgress) { return false; }
            regenerationInProgress = true;
            try
            {
                var dStage = pawn.DevelopmentalStage;
                float sizeFromAge = pawn.ageTracker.CurLifeStage.bodySizeFactor;
                float baseSize = pawn.RaceProps.baseBodySize;
                float previousTotalSize = sizeFromAge * baseSize;

                StatDef statOffsetDef = StatDef.Named("SM_BodySizeOffset");
                float sizeOffset = pawn.GetStatValue(statOffsetDef);

                StatDef statCosmeticOffsetDef = StatDef.Named("SM_Cosmetic_BodySizeOffset");
                float cosmeticSizeOffset = pawn.GetStatValue(statCosmeticOffsetDef);

                cosmeticSizeOffset += sizeOffset;

                StatDef statMultDef = StatDef.Named("SM_BodySizeMultiplier");
                float sizeMultiplier = pawn.GetStatValue(statMultDef);

                //float cosmeticSizeMultiplier = 1f; // Not currently implemented.

                float bodySizeOffset = ((baseSize + sizeOffset) * sizeMultiplier * sizeFromAge) - previousTotalSize;

                float bodySizeCosmeticOffset = ((baseSize + cosmeticSizeOffset) * sizeMultiplier * sizeFromAge) - previousTotalSize;

                // Get total size
                float totalSize = bodySizeOffset + previousTotalSize;
                float totalCosmeticSize = bodySizeCosmeticOffset + previousTotalSize;

                // Check if the pawn has a hediff with a name starting with BS_Affliction
                bool hasSizeAffliction = ScalingMethods.CheckForSizeAffliction(pawn);
                if (!hasSizeAffliction)
                {
                    ////////////////////////////////// 
                    // Clamp Total Size

                    // Prevent babies from getting too large for cribs, or too smol in general.
                    if (dStage < DevelopmentalStage.Child)
                    {
                        totalSize = Mathf.Clamp(totalSize, 0.05f, 0.24f);
                    }
                    else if (totalSize < 0.10) totalSize = 0.10f;


                    ////////////////////////////////// 
                    // Clamp Offset to avoid extremes
                    if (totalSize < 0.05f && dStage < DevelopmentalStage.Child)
                    {
                        bodySizeOffset = -(previousTotalSize - 0.05f);
                    }
                    // Don't permit babies too large to fit in cribs (0.25)
                    else if (totalSize > 0.24f && dStage < DevelopmentalStage.Child && pawn.RaceProps.Humanlike)
                    {
                        bodySizeOffset = -(previousTotalSize - 0.24f);
                    }
                    else if (totalSize < 0.10f && dStage == DevelopmentalStage.Child)
                    {
                        bodySizeOffset = -(previousTotalSize - 0.10f);
                    }
                    // If adult basically limit size to 0.10
                    else if (totalSize < 0.10f && dStage > DevelopmentalStage.Child && pawn.RaceProps.Humanlike)
                    {
                        bodySizeOffset = -(previousTotalSize - 0.10f);
                    }
                }
                else
                {
                    // Even with funky status conditions set the limit at 2%.
                    totalSize = Mathf.Max(totalSize, 0.02f);
                }

                StatDef statHeadSize = StatDef.Named("SM_HeadSize_Cosmetic");
                float headSize = pawn.GetStatValue(statHeadSize);

                // Less difference for animals, they seem to get double-dipped somewhere?
                if (!pawn.RaceProps.Humanlike)
                {
                    bodySizeCosmeticOffset *= 0.5f;
                }

                PercentChange scaleMultiplier = GetPercentChange(bodySizeOffset, pawn);
                PercentChange cosmeticScaleMultiplier = GetPercentChange(bodySizeCosmeticOffset, pawn);

                if (!pawn.RaceProps.Humanlike && cosmeticScaleMultiplier.linear > 1.5f)
                {
                    // Never let animals render huge, it just looks silly.
                    float maxSize = 3;
                    if (hasSizeAffliction) maxSize = 6;
                    cosmeticScaleMultiplier.linear = Mathf.Min(Mathf.Lerp(cosmeticScaleMultiplier.linear, 1.5f, 0.65f), maxSize);
                }

                StatDef statMinLearn = StatDef.Named("SM_Minimum_Learning_Speed");
                float minimumLearning = pawn.GetStatValue(statMinLearn);

                StatDef statFoodNeedCapMult = StatDef.Named("SM_Food_Need_Capacity");
                float foodNeedCapacityMult = pawn.GetStatValue(statFoodNeedCapMult);

                // Traits on pawn
                var traits = pawn.story?.traits?.allTraits;


                // Hediff Caching
                var hediffs = pawn.health?.hediffSet.hediffs;
                bool willBecomeUndead = hediffs.Any(x => x.def.defName == "VU_DraculVampirism" || x.def.defName == "BS_ReturnedReanimation") == true;

                if (traits != null)
                {
                    try
                    {
                        if (pawn.needs != null && traits.Any(x => !x.Suppressed && x.def.defName == "BS_AlcoholAddict") && !hediffs.Any(x => x.def.defName == "AlcoholAddiction"))
                        {
                            // Add Alcohol Addiction.
                            pawn.health.AddHediff(HediffDef.Named("AlcoholAddiction"));
                        }
                    }
                    catch
                    {
                        // Do nothing, just catch the error. This isn't important enough to send an error if it fails.
                    }

                }

                // Gene Caching
                var undeadGenes = Helpers.GetActiveGenesByNames(pawn, new List<string>
                {
                    "VU_Unliving", "VU_Lesser_Unliving_Resilience", "VU_Unliving_Resilience", "BS_RoboticResilienceLesser", "BS_RoboticResilience", "BS_IsUnliving"
                });

                bool animalReturned = pawn.health.hediffSet.HasHediff(BSDefs.VU_AnimalReturned);
                bool animalVampirism = pawn.health.hediffSet.HasHediff(BSDefs.VU_DraculAnimalVampirism);

                bool animalUndead = animalReturned || animalVampirism;

                var allActiveGenes = Helpers.GetAllActiveGenes(pawn);

                bool noBlood = allActiveGenes.Any(x => x.def.defName == "VU_NoBlood") || animalReturned;
                bool verySlowBleeding = animalVampirism;
                bool slowBleeding = allActiveGenes.Any(x => x.def.defName == "BS_SlowBleeding");

                BleedRateState bleedState = noBlood ? BleedRateState.NoBleeding
                                                  : verySlowBleeding ? BleedRateState.VerySlowBleeding
                                                  : slowBleeding ? BleedRateState.SlowBleeding
                                                  : BleedRateState.Unchanged;

                // Has Deathlike gene or VU_AnimalReturned Hediff.
                bool deathlike = allActiveGenes.Any(x => x.def.defName == "BS_Deathlike") || animalUndead;
                bool unarmedOnly = allActiveGenes.Any(x => new List<string> { "BS_UnarmedOnly", "BS_NoEquip", "BS_UnarmedOnly_Android" }.Contains(x.def.defName));
                bool succubusUnbonded = false;
                if (allActiveGenes.Any(x => x.def.defName == "VU_LethalLover"))
                {
                    // Check if psychic bond is active
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) == null)
                    {
                        succubusUnbonded = true;
                    }
                }
                bool fastPregnancy = allActiveGenes.Any(x => x.def.defName == "BS_ShortPregnancy");
                bool everFertile = allActiveGenes.Any(x => x.def.defName == "BS_EverFertile");
                bool animalFriend = pawn.story?.traits?.allTraits.Any(x => !x.Suppressed && x.def.defName == "BS_AnimalFriend") == true;


                bool cannotWearClothing = allActiveGenes.Any(x => x.def.defName == "BS_CannotWearClothing");
                bool cannotWearArmor = allActiveGenes.Any(x => x.def.defName == "BS_CannotWearArmor");
                bool cannotWearApparel = allActiveGenes.Any(x => x.def.defName == "BS_CannotWearClothingOrArmor");

                StatDef statAttackSpeed = StatDef.Named("SM_AttackSpeed");
                float attackSpeedMultiplier = pawn.GetStatValue(statAttackSpeed);

                // Get all genes with the GeneExtension
                List<GeneExtension> genesWithExtension = Helpers.GetAllActiveGenes(pawn).Select(x => x.def.GetModExtension<GeneExtension>()).Where(x => x != null).ToList();

                // Add together bodyPosOffset from GeneExtension.
                float bodyPosOffset = genesWithExtension.Sum(x => x.bodyPosOffset);
                float headPosMultiplier = genesWithExtension.Sum(x => x.headPosMultiplier);

                var alcoholHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AlcoholHigh);
                float alcoholLevel = alcoholHediff?.Severity ?? 0;


                // Set Cache Values
                this.totalSize = totalSize;
                this.totalCosmeticSize = totalCosmeticSize;
                this.sizeOffset = bodySizeOffset;
                previousScaleMultiplier = this.scaleMultiplier;
                this.scaleMultiplier = scaleMultiplier;
                this.cosmeticScaleMultiplier = cosmeticScaleMultiplier;
                this.minimumLearning = minimumLearning;
                this.foodNeedCapacityMult = foodNeedCapacityMult;
                headSizeMultiplier = headSize;
                isBloodFeeder = IsBloodfeederPatch.IsBloodfeeder(pawn) || bleedState == BSCache.BleedRateState.NoBleeding;
                this.hasSizeAffliction = hasSizeAffliction;
                this.attackSpeedMultiplier = attackSpeedMultiplier;
                psychicSensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
                alcoholmAmount = alcoholLevel;


                canWearClothing = !(cannotWearClothing || cannotWearApparel);
                canWearArmor = !(cannotWearArmor || cannotWearApparel);
                canWearApparel = !cannotWearApparel;

                injuriesRescaled = false;
                isUnliving = undeadGenes.Count > 0 || animalUndead;
                willBeUndead = willBecomeUndead;
                bleedRate = bleedState;
                this.deathlike = deathlike;
                this.unarmedOnly = unarmedOnly;
                diet = GameUtils.GetDiet(pawn);
                this.succubusUnbonded = succubusUnbonded;
                this.fastPregnancy = fastPregnancy;
                this.everFertile = everFertile;
                this.animalFriend = animalFriend;

                this.bodyPosOffset = bodyPosOffset;
                this.headPosMultiplier = 1 + headPosMultiplier;

                raidWealthMultiplier = pawn.GetStatValue(StatDef.Named("SM_RaidWealthMultiplier"));
                raidWealthOffset = pawn.GetStatValue(StatDef.Named("SM_RaidWealthOffset"));

                bodyRenderSize = GetBodyRenderSize();
                headRenderSize = GetHeadRenderSize();
            }
            finally
            {
                regenerationInProgress = false;
            }
            return true;
        }


        public class PercentChange : IExposable
        {
            public float linear = 1;
            public float quadratic = 1;
            public float cubic = 1;
            public float KelibersLaw => Mathf.Pow(cubic, 0.75f);    // Results in a colonist that does nothing but eat. Not a great idea...
            public float DoubleMaxLinear => linear < 1 ? linear : 1 + (linear - 1) * 2;
            public float TripleMaxLinear => linear < 1 ? linear : 1 + (linear - 1) * 3;

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

        private static PercentChange GetPercentChange(float bodySizeOffset, Pawn pawn)
        {
            if (pawn != null
                && (pawn.needs != null || pawn.Dead))
            {
                float sizeFromAge = pawn.ageTracker.CurLifeStage.bodySizeFactor;
                float baseSize = pawn.RaceProps.baseBodySize;
                float prevBodySize = sizeFromAge * baseSize;
                float postBodySize = prevBodySize + bodySizeOffset;
                float percentChange = postBodySize / prevBodySize;
                // This math is 'effed, but it happens to give nice results, so, whatever.
                // Not changing it now since the balance is fine.
                float quadratic = Mathf.Pow(percentChange, 2) - 1;
                float cubic = Mathf.Pow(percentChange, 3) - 1;

                // Ensure we don't get negative values.
                percentChange = Mathf.Clamp(percentChange, 0.04f, 9999999);
                quadratic = Mathf.Clamp(quadratic, 0.04f, 9999999);
                cubic = Mathf.Clamp(cubic, 0.04f, 9999999);

                // Let's not make humans sprites unreasonably small.
                if (percentChange < 0.2f) percentChange = 0.2f;
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

        
    }
}
