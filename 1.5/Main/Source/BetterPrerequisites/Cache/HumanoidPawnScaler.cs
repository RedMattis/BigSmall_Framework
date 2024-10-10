using BetterPrerequisites;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    public class BigAndSmallCache : GameComponent
    {
        public static HashSet<PGene> pGenesThatReevaluate = new();

        public static HashSet<BSCache> scribedCache = new();
        public static bool regenerationAttempted = false;
        public Game game;

        public static Queue<Pawn> refreshQueue = new();

        public static Queue<Action> queuedJobs = new();

        public static Dictionary<int, HashSet<BSCache>> schedulePostUpdate = [];
        public static Dictionary<int, HashSet<BSCache>> scheduleFullUpdate = [];
        public static HashSet<BSCache> currentlyQueued = new(); // Ensures we never queue the same cache twice.

        public BigAndSmallCache(Game game)
        {
            this.game = game;
        }
        public override void ExposeData()
        {
            Scribe_Collections.Look<BSCache>(ref scribedCache, saveDestroyedThings: false, "BS_scribedCache", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
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

        public override void GameComponentTick()
        {
            if (queuedJobs.Count > 0)
            {
                var job = queuedJobs.Dequeue();
                job();
            }

            int currentTick = Find.TickManager.TicksGame;

            if (schedulePostUpdate.ContainsKey(currentTick))
            {
                foreach (var cache in schedulePostUpdate[currentTick])
                {
                    cache?.DelayedUpdate();
                }
                schedulePostUpdate.Remove(currentTick);
            }
            if (scheduleFullUpdate.ContainsKey(currentTick))
            {
                foreach (var cache in scheduleFullUpdate[currentTick])
                {
                    var cPawn = cache?.pawn;
                    if (cPawn != null && !cPawn.Discarded)
                    {
                        HumanoidPawnScaler.GetCache(cPawn, forceRefresh: true);
                    }
                    currentlyQueued.Remove(cache);
                }
                scheduleFullUpdate.Remove(currentTick);
            }

            if (currentTick % 100 != 0)
            {
                return;
            }

            // If the queue is empty, enqueue the HumanoidPawnScaler.Cache.
            if (refreshQueue.Count == 0)
            {
                foreach (var cache in HumanoidPawnScaler.Cache.Values)
                {
                    if (cache.pawn != null)
                        refreshQueue.Enqueue(cache.pawn);
                }
            }
            else
            {
                // If the queue is not empty, dequeue the first cache and refresh it.
                var cachedPawn = refreshQueue.Dequeue();
                if (cachedPawn != null && cachedPawn.Spawned)
                {
                    HumanoidPawnScaler.GetCache(cachedPawn, forceRefresh: true);
                }
            }


            //if (Find.TickManager.TicksGame % 500 != 0)
            //{
            //    return;
            //}
            // Regenerate all pGene caches every 100 ticks.
            // This is done on a component to save the performance cost of having them check a timer every time
            // they are called.
            pGenesThatReevaluate = new HashSet<PGene>(pGenesThatReevaluate.Where(x => x != null && x.pawn != null && !x.pawn.Discarded));
            foreach (var pGene in pGenesThatReevaluate.Where(x=>x.pawn.Spawned))
            {
                bool active = pGene.previouslyActive == true;
                bool newState = pGene.RegenerateState();
                if (active != newState)
                {
                    GeneHelpers.NotifyGenesUpdated(pGene.pawn, pGene.def);
                }
            }
        }
    }

    public class HumanoidPawnScaler : DictCache<Pawn, BSCache>
    {
        public struct PerThreadMiniCache
        {
            public Pawn pawn;
            public BSCache cache;
        }
        [ThreadStatic]
        static PerThreadMiniCache threadStaticCache;

        public static BSCache GetCacheUltraSpeed(Pawn pawn, bool canRegenerate = true)
        {
            if (pawn != null && threadStaticCache.pawn == pawn)
            {
                return threadStaticCache.cache;
            }
            else if (canRegenerate) return GetCache(pawn, canRegenerate: canRegenerate);
            else return BSCache.defaultCache;
        }
        [Obsolete]
        public static BSCache GetBSDict(Pawn pawn, bool forceRefresh = false, bool canRegenerate = true, int scheduleForce = -1)
        {
            return GetCache(pawn, forceRefresh, canRegenerate, scheduleForce);
        }

        /// <summary>
        /// Note that if the pawn is "null" then it will use a generic cache with default values. This is mostly to just get rid of the need to
        /// null-check everything that calls this method.
        /// </summary>
        /// <returns></returns>
        public static BSCache GetCache(Pawn pawn, bool forceRefresh = false, bool canRegenerate=true, int scheduleForce=-1)//, bool debug=false)
        {
            if (pawn == null)
            {
                return BSCache.defaultCache;
            }

            //if (debug)
            //{
            //    Log.Message($"Big & Small: Getting Cache for {pawn}\n" +
            //        $"All parameters: forceRefresh: {forceRefresh}, regenerateIfTimer: {regenerateIfTimer}, canRegenerate: {canRegenerate}, scheduleForce: {scheduleForce}\n" +
            //        $"Predicted Outcome: {RunNormalCalculations(pawn)} {pawn != null}&&{BigSmall.performScaleCalculations}&&({pawn.RaceProps.Humanlike}||{BigSmallMod.settings.scaleAnimals})&&({pawn.needs != null}||{pawn.Dead})");
            //}

            bool newEntry;
            BSCache result;
            if (canRegenerate && RunNormalCalculations(pawn))
            {
                result = GetCache(pawn, out newEntry, forceRefresh: forceRefresh, canRegenerate: true);
            }
            else
            {
                // Unless values have already been set, this will just be a cache with default values.
                result = GetCache(pawn, out newEntry, forceRefresh, canRegenerate: false);
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
                    ShedueleForceRegenerate(result, 1);
                }
            }
            if (forceRefresh)
            {
                // Refresh graphics
                if (pawn.Spawned)
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
            if (scheduleForce > -1)
            {
                ShedueleForceRegenerate(result, scheduleForce);
            }

            return result;
        }

        private static void ShedueleForceRegenerate(BSCache cache, int delay)
        {
            if (BigAndSmallCache.currentlyQueued.Contains(cache))
            {
                return;
            }
            int targetTick = Find.TickManager.TicksGame + delay;
            if (BigAndSmallCache.scheduleFullUpdate.ContainsKey(targetTick) == false)
            {
                BigAndSmallCache.scheduleFullUpdate[targetTick] = [];
            }
            BigAndSmallCache.scheduleFullUpdate[targetTick].Add(cache);
            BigAndSmallCache.currentlyQueued.Add(cache);
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

    public partial class BSCache : IExposable, ICacheable
    {
        public static BSCache defaultCache = new();

        public Pawn pawn = null;
        public bool refreshQueued = false;
        public int? lastUpdateTick = null;
        public bool SameTick => lastUpdateTick == Find.TickManager.TicksGame;

        public CacheTimer Timer { get; set; } = new CacheTimer();

        public float bodyRenderSize = 1;
        public float headRenderSize = 1;

        public enum BleedRateState { Unchanged, SlowBleeding, VerySlowBleeding, NoBleeding }

        public float totalSize = 1;
        public float totalCosmeticSize = 1;
        public PercentChange scaleMultiplier = new(1, 1, 1);
        public PercentChange previousScaleMultiplier = null;
        public PercentChange cosmeticScaleMultiplier = new(1, 1, 1);
        public bool isHumanlike = false;

        public float healthMultiplier = 1;
        public float healthMultiplier_previous = 1;

        public float totalSizeOffset = 0;
        public float minimumLearning = 0;
        public float growthPointGain = 1;
        //public float foodNeedCapacityMult = 1;
        //public float? previousFoodCapacity = null;
        public float headSizeMultiplier = 1;

        /// <summary>
        /// This one returns true on stuff like bloodless pawns just so they can't have blood drained from them.
        /// </summary>
        public bool isBloodFeeder = false;
        public bool hasSizeAffliction = false;
        public float attackSpeedMultiplier = 1;
        public float attackSpeedUnarmedMultiplier = 1;
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
        public float pregnancySpeed = 1;
        public bool everFertile = false;
        public bool animalFriend = false;

        public float raidWealthOffset = 0;
        public float raidWealthMultiplier = 1;

        public float bodyPosOffset = 0;
        public float headPosMultiplier = 1;

        public bool preventDisfigurement = false;

        public bool renderCacheOff = false;

        public FoodKind diet = FoodKind.Any;

        public List<ApparelCache> apparelCaches = [];

        // Color and Fur Transform cache
        public Color? savedSkinColor = null;
        public Color? savedHairColor = null;
        public string savedFurSkin = null;
        public string savedBodyDef = null;
        public string savedHeadDef = null;
        //public bool disableBeards = false;
        public string savedBeardDef = null;


        public int? randomPickSkinColor = null;
        public int? randomPickHairColor = null;

        public bool facialAnimationDisabled = false;
        public bool facialAnimationDisabled_Transform = false; // Used for the ColorAndFur Hediff.

        public bool isDrone = false;

        public string id = "BS_DefaultID";

        // Default Comparer function
        public static bool Compare(BSCache a, BSCache b)
        {
            return a.id == b.id;
        }

        public void ExposeData()
        {
            // Scribe Pawn
            Scribe_Values.Look(ref id, "BS_CachePawnID", defaultValue: "BS_DefaultCahced");

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

            Scribe_Values.Look(ref minimumLearning, "BS_MinimumLearning", 0);
            //Scribe_Values.Look(ref foodNeedCapacityMult, "BS_FoodNeedCapacityMult", 1);
            //Scribe_Values.Look(ref previousFoodCapacity, "BS_PreviousFoodCapacity", 1);
            Scribe_Values.Look(ref headSizeMultiplier, "BS_HeadSizeMultiplier", 1);
            Scribe_Values.Look(ref isBloodFeeder, "BS_IsBloodFeeder", false);
            Scribe_Values.Look(ref hasSizeAffliction, "BS_HasSizeAffliction", false);
            Scribe_Values.Look(ref attackSpeedMultiplier, "BS_AttackSpeedMultiplier", 1);
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
            Scribe_Values.Look(ref pregnancySpeed, "BS_FastPregnancy", 1f);
            Scribe_Values.Look(ref everFertile, "BS_EverFertile", false);
            Scribe_Values.Look(ref animalFriend, "BS_AnimalFriend", false);
            Scribe_Values.Look(ref raidWealthOffset, "BS_RaidWealthOffset", 0);
            Scribe_Values.Look(ref raidWealthMultiplier, "BS_RaidWealthMultiplier", 1);
            Scribe_Values.Look(ref bodyPosOffset, "BS_BodyPosOffset", 0);
            Scribe_Values.Look(ref headPosMultiplier, "BS_HeadPosMultiplier", 1);
            Scribe_Values.Look(ref diet, "BS_Diet", FoodKind.Any);
            Scribe_Collections.Look(ref apparelCaches, "BS_ApparelCaches", LookMode.Deep);
            Scribe_Values.Look(ref preventDisfigurement, "BS_PreventDisfigurement", false);
            Scribe_Values.Look(ref renderCacheOff, "BS_RenderCacheOff", false);
            Scribe_Values.Look(ref savedSkinColor, "BS_SavedSkinColor", null);
            Scribe_Values.Look(ref savedHairColor, "BS_SavedHairColor", null);
            //Scribe_Values.Look(ref disableBeards, "BS_DisableBeards", false);
            Scribe_Values.Look(ref savedBeardDef, "BS_SavedBeardDef", null);
            Scribe_Values.Look(ref randomPickSkinColor, "BS_RandomPickSkinColor", null);
            Scribe_Values.Look(ref randomPickHairColor, "BS_RandomPickHairColor", null);
            Scribe_Values.Look(ref facialAnimationDisabled, "BS_FacialAnimationDisabled", false);
            Scribe_Values.Look(ref savedFurSkin, "BS_SavedFurskinName");
            Scribe_Values.Look(ref savedBodyDef, "BS_SavedBodyDefName");
            Scribe_Values.Look(ref savedHeadDef, "BS_SavedHeadDefName");
            Scribe_Values.Look(ref isDrone, "BS_IsDrone", false);

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
            //Log.Message($"DEBUG! REMOVE THIS BEFORE SUBMIT: Big & Small: Regenerating Cache for {pawn.Name}");
            if (pawn == null) { throw new Exception("Big & Small: Cannot regenerate Pawn Cache because the Pawn is null."); }
            if (regenerationInProgress) { return false; }
            regenerationInProgress = true;
            try
            {
                int tick = Find.TickManager.TicksGame;
                DevelopmentalStage dStage;
                try
                {
                    dStage = pawn.DevelopmentalStage;
                }
                catch
                {
                    Log.Warning($"[BigAndSmall] caught an exception when fetching Developmental Stage for {pawn.Name} Aborting generation of pawn cache.\n" +
                        $"This likely means the pawn lacks \"lifeStageAges\" or another requirement for fetching the age is missing.");
                    return false;
                }
                isHumanlike = pawn.RaceProps?.Humanlike == true;

                var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
                List<GeneExtension> geneExts = activeGenes
                    .Where(x => x?.def?.modExtensions != null && x.def.modExtensions.Any(y => y.GetType() == typeof(GeneExtension)))?
                    .Select(x => x.def.GetModExtension<GeneExtension>()).ToList();
                bool hasSizeAffliction = ScalingMethods.CheckForSizeAffliction(pawn);
                CalculateSize(dStage, geneExts, hasSizeAffliction);

                float minimumLearning = pawn.GetStatValue(BSDefs.SM_Minimum_Learning_Speed);

                // Traits on pawn
                var traits = pawn.story?.traits?.allTraits;

                // Hediff Caching
                var hediffs = pawn.health?.hediffSet.hediffs;
                bool willBecomeUndead = hediffs.Any(x => x.def.defName == "VU_DraculVampirism" || x.def.defName == "BS_ReturnedReanimation") == true;


                // Get all genes with the GeneExtension
                // Gene Caching
                var undeadGenes = GeneHelpers.GetActiveGenesByNames(pawn, new List<string>
                {
                    "VU_Unliving", "VU_Lesser_Unliving_Resilience", "VU_Unliving_Resilience", "BS_RoboticResilienceLesser", "BS_RoboticResilience", "BS_IsUnliving"
                });

                bool animalReturned = pawn.health.hediffSet.HasHediff(BSDefs.VU_AnimalReturned);
                bool animalVampirism = pawn.health.hediffSet.HasHediff(BSDefs.VU_DraculAnimalVampirism);

                bool animalUndead = animalReturned || animalVampirism;



                bool noBlood = activeGenes.Any(x => x.def.defName == "VU_NoBlood") || animalReturned;
                bool verySlowBleeding = animalVampirism;
                bool slowBleeding = activeGenes.Any(x => x.def.defName == "BS_SlowBleeding");

                BleedRateState bleedState = noBlood ? BleedRateState.NoBleeding
                                                  : verySlowBleeding ? BleedRateState.VerySlowBleeding
                                                  : slowBleeding ? BleedRateState.SlowBleeding
                                                  : BleedRateState.Unchanged;

                // Has Deathlike gene or VU_AnimalReturned Hediff.
                bool deathlike = activeGenes.Any(x => x.def.defName == "BS_Deathlike") || animalUndead;
                bool unarmedOnly = activeGenes.Any(x => new List<string> { "BS_UnarmedOnly", "BS_NoEquip", "BS_UnarmedOnly_Android" }.Contains(x.def.defName));
                bool unamredOnly = unarmedOnly || geneExts.Any(x => x.unarmedOnly || x.forceUnarmed);
                bool succubusUnbonded = false;
                if (activeGenes.Any(x => x.def.defName == "VU_LethalLover"))
                {
                    // Check if psychic bond is active
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) == null)
                    {
                        succubusUnbonded = true;
                    }
                }
                bool everFertile = activeGenes.Any(x => x.def.defName == "BS_EverFertile");
                bool animalFriend = pawn.story?.traits?.allTraits.Any(x => !x.Suppressed && x.def.defName == "BS_AnimalFriend") == true;


                bool cannotWearClothing = activeGenes.Any(x => x.def.defName == "BS_CannotWearClothing");
                bool cannotWearArmor = activeGenes.Any(x => x.def.defName == "BS_CannotWearArmor");
                bool cannotWearApparel = activeGenes.Any(x => x.def.defName == "BS_CannotWearClothingOrArmor");

                //facialAnimationDisabled = activeGenes.Any(x => x.def == BSDefs.BS_FacialAnimDisabled);
                facialAnimationDisabled = geneExts.Any(x => x.disableFacialAnimations || x.facialDisabler != null)
                    || facialAnimationDisabled_Transform;

                // Add together bodyPosOffset from GeneExtension.
                float bodyPosOffset = geneExts.Sum(x => x.bodyPosOffset);
                float headPosMultiplier = geneExts.Sum(x => x.headPosMultiplier);
                bool preventDisfigurement = geneExts.Any(x => x.preventDisfigurement);

                var alcoholHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AlcoholHigh);
                float alcoholLevel = alcoholHediff?.Severity ?? 0;
                alcoholmAmount = alcoholLevel;

                //int currentTick = Find.TickManager.TicksGame;

                // Set "Previous" Values. This is meant to make sure the previous values don't get overwritten before they can be used.
                //if (lastUpdateTick == null || lastUpdateTick != currentTick)
                //{
                //    lastUpdateTick = currentTick;
                //    previousScaleMultiplier = this.scaleMultiplier;  // First time this runs on a pawn after loading this will be 1.
                //    healthMultiplier_previous = CalculateHealthMultiplier(this.scaleMultiplier, pawn);
                //}

                // Set Cache Values
                

                // The current value.
                
                this.minimumLearning = minimumLearning;
                this.growthPointGain = pawn.GetStatValue(BSDefs.SM_GrowthPointAccumulation);
                //this.foodNeedCapacityMult = pawn.GetStatValue(BSDefs.SM_Food_Need_Capacity);
                
                isBloodFeeder = IsBloodfeederPatch.IsBloodfeeder(pawn) || bleedState == BSCache.BleedRateState.NoBleeding;
                this.hasSizeAffliction = hasSizeAffliction;
                attackSpeedMultiplier = pawn.GetStatValue(BSDefs.SM_AttackSpeed);
                attackSpeedUnarmedMultiplier = pawn.GetStatValue(BSDefs.SM_UnarmedAttackSpeed);

                canWearClothing = !(cannotWearClothing || cannotWearApparel);
                canWearArmor = !(cannotWearArmor || cannotWearApparel);
                canWearApparel = !cannotWearApparel;

                isDrone = geneExts.Any(x => x.isDrone);

                

                // Check if they are a shambler
                var isShambler = pawn?.mutant?.Def?.defName?.ToLower().Contains("shambler") == true;
                // Check if it has the "ShamblerCorpse" hediff
                isShambler = isShambler || pawn.health.hediffSet.HasHediff(HediffDefOf.ShamblerCorpse);

                isUnliving = undeadGenes.Count > 0 || animalUndead || isShambler;
                willBeUndead = willBecomeUndead;
                bleedRate = bleedState;
                this.deathlike = deathlike;
                this.unarmedOnly = unarmedOnly;
                diet = GameUtils.GetDiet(pawn);
                this.succubusUnbonded = succubusUnbonded;
                // Multiply the prengnacy multipliers.
                pregnancySpeed = geneExts.Aggregate(1f, (acc, x) => acc * x.pregnancySpeedMultiplier);
                this.everFertile = everFertile;
                this.animalFriend = animalFriend;
                renderCacheOff = geneExts.Any(x => x.renderCacheOff);

                this.bodyPosOffset = bodyPosOffset;
                this.headPosMultiplier = 1 + headPosMultiplier;

                raidWealthMultiplier = pawn.GetStatValue(StatDef.Named("SM_RaidWealthMultiplier"));
                raidWealthOffset = pawn.GetStatValue(StatDef.Named("SM_RaidWealthOffset"));

                bodyRenderSize = GetBodyRenderSize();
                headRenderSize = GetHeadRenderSize();

                // More stuff should probably be moved here.
                ScheduleUpdate(1);
            }
            catch
            {
                // Remove the cache entry so it can be regenerated if this has not already been attempted.
                if (!BigAndSmallCache.regenerationAttempted)
                {
                    Log.Warning($"Issue reloading cache of {pawn} ({id}). Removing entire cache so it can be regenerated.");
                    // Reassigning instead of clearing in case it is null for some reason.
                    HumanoidPawnScaler.Cache = new ConcurrentDictionary<Pawn, BSCache>();
                    BigAndSmallCache.scribedCache = new HashSet<BSCache>();
                    BigAndSmallCache.regenerationAttempted = true;
                }
                throw;
            }
            finally
            {
                regenerationInProgress = false;
            }

            return true;
        }



        public void ScheduleUpdate(int delayTicks)
        {
            int targetTick = Find.TickManager.TicksGame + delayTicks;
            if (BigAndSmallCache.schedulePostUpdate.ContainsKey(targetTick) == false)
            {
                BigAndSmallCache.schedulePostUpdate[targetTick] = [];
            }

            BigAndSmallCache.schedulePostUpdate[targetTick].Add(this);
        }

        /// <summary>
        /// Stuff that should be run a bit later. Typically 1 tick. This also has the benefit that it will never run more than once per tick.
        /// 
        /// Anything that we don't need to figure out RIGHT NOW. Can go here.
        /// 
        /// More stuff should probably be moved here. Delaying stuff helps dealing with issues like genes being appended on-by-one.
        /// </summary>
        public void DelayedUpdate()
        {
            if (pawn == null || pawn.Dead) { return; }

            var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
            List<GeneExtension> geneExts = activeGenes
                .Where(x => x?.def?.modExtensions != null && x.def.modExtensions.Any(y => y.GetType() == typeof(GeneExtension)))?
                .Select(x => x.def.GetModExtension<GeneExtension>()).ToList();


            // Traits on pawn
            var traits = pawn.story?.traits?.allTraits;

            // Hediff Caching
            var hediffs = pawn.health?.hediffSet.hediffs;
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
            if (BSDefs.BS_SoulPower != null)
            {
                var soulPower = pawn.GetStatValue(BSDefs.BS_SoulPower);
                if (soulPower > 0.1 && !pawn.Dead)
                {
                    // Check if the pawn has the Soul Power Hediff
                    if (!hediffs.Any(x => x.def == BSDefs.BS_SoulPowerHediff))
                    {
                        pawn.health.AddHediff(BSDefs.BS_SoulPowerHediff);
                    }
                }
            }

            bool selfRepairingApparel = activeGenes.Any(x => x.def.defName == "BS_SelfRepairingApparel");
            bool indestructibleApparel = activeGenes.Any(x => x.def.defName == "BS_IndestructibleApparel");
            indestructibleApparel |= pawn.health.hediffSet.HasHediff(BSDefs.BS_IndestructibelApparel);

            int currentTick = Find.TickManager.TicksGame;

            // Get all armour
            if (pawn?.apparel?.WornApparel?.Count > 0 && (selfRepairingApparel || indestructibleApparel))
            {
                var wornApparel = pawn.apparel.WornApparel;
                foreach (var apparel in wornApparel)
                {
                    bool found = false;
                    foreach (var apparelCache in apparelCaches.Where(x => x.apparelID == apparel.ThingID))
                    {
                        found = true;
                        if (indestructibleApparel)
                        {
                            apparelCache.RepairAllDurability(apparel);
                        }
                        else if (selfRepairingApparel)
                        {
                            // Repair 24% of the durability every day.
                            apparelCache.RepairDurability(apparel, currentTick, 0.24f);
                        }
                    }
                    if (!found)
                    {
                        apparelCaches.Add(new ApparelCache(apparel));
                    }
                }
                // Check if any apparel has been removed, and remove it from the cache if so.
                for (int idx = apparelCaches.Count - 1; idx >= 0; idx--)
                {
                    ApparelCache apparelCache = apparelCaches[idx];
                    if (!wornApparel.Any(x => x.ThingID == apparelCache.apparelID))
                    {
                        apparelCaches.RemoveAt(idx);
                    }
                }
            }

            Metamorphosis.HandleMetamorph(pawn, geneExts);
        }

        

    }

    public class ApparelCache : IExposable
    {
        public string apparelID;
        public int highestSeenDurability = 1;
        public int lastSeenTick = 0;
        public float accumulatedHp = 0;

        public ApparelCache()
        {
        }

        public ApparelCache(Apparel apparel)
        {
            apparelID = apparel.ThingID;
            highestSeenDurability = apparel.HitPoints;
            lastSeenTick = Find.TickManager.TicksGame;
        }

        public void RepairDurability(Apparel apparel, int currentTick, float fractionPerDay)
        {
            // In case the durability has been increased by outside factors.
            highestSeenDurability = Mathf.Max(highestSeenDurability, apparel.HitPoints);


            int ticksPerDay = 60000;
            int ticksSinceLastSeen = currentTick - lastSeenTick;
            // The apparel should repair by <fraction> per day, calculated from the last time we saw it.
            float repairRate = ticksSinceLastSeen / (float)ticksPerDay * fractionPerDay;

            // Apparel maxHP
            float hpScale = Mathf.Max(apparel.MaxHitPoints, 50) / 50.0f;

            // Restore hp
            int apparelMaxHp = apparel.MaxHitPoints;
            int apparelHp = apparel.HitPoints;
            float hpToAdd = Mathf.Abs(repairRate * (apparelMaxHp - apparelHp) * hpScale);
            // Since the value is likely less than 1 we accumulate it until it's enough to repair a whole point.
            accumulatedHp += hpToAdd;

            if (accumulatedHp > 1)
            {
                //int oldhp = apparel.HitPoints;

                int newHp = apparelHp + (int)accumulatedHp;
                newHp = Math.Min(apparelMaxHp, newHp);
                newHp = Math.Min(newHp, highestSeenDurability);
                apparel.HitPoints = newHp;

                //Log.Message($"DEBUG: Repairing {apparel.Label} by {accumulatedHp} hp, to {apparel.HitPoints}/{highestSeenDurability}. Was {oldhp}.");
                accumulatedHp = 0;
                
            }

            lastSeenTick = currentTick;
        }

        public void RepairAllDurability(Apparel apparel)
        {
            highestSeenDurability = Mathf.Max(highestSeenDurability, apparel.HitPoints);
            apparel.HitPoints = highestSeenDurability;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref apparelID, "apparelID");
            Scribe_Values.Look(ref highestSeenDurability, "highestSeenDurability");
            Scribe_Values.Look(ref lastSeenTick, "lastSeenTick");
            Scribe_Values.Look(ref accumulatedHp, "accumulatedHp");
        }
    }
}
