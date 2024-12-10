using BetterPrerequisites;
using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class BigAndSmallCache : GameComponent
    {
        public static HashSet<PGene> pGenesThatReevaluate = [];

        public static HashSet<BSCache> scribedCache = [];
        public static bool regenerationAttempted = false;
        public Game game;

        public static Queue<Pawn> refreshQueue = new();

        public static Queue<Action> queuedJobs = new();

        public static Dictionary<int, HashSet<BSCache>> schedulePostUpdate = [];
        public static Dictionary<int, HashSet<BSCache>> scheduleFullUpdate = [];
        public static HashSet<BSCache> currentlyQueued = []; // Ensures we never queue the same cache twice.

        public static float globalRandNum = 1;

        public BigAndSmallCache(Game game)
        {
            this.game = game;
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // Get all pawns registered in the game.
            var allPawns = PawnsFinder.All_AliveOrDead;
            foreach (var pawn in allPawns.Where(x => x != null && !x.Discarded && !x.Destroyed))
            {
                if (HumanoidPawnScaler.GetCache(pawn, scheduleForce:1) is BSCache cache)
                {
                }
            }
        }

        public void QueueJobOrRunNowIfPaused(Action job)
        {
            if (Find.TickManager?.Paused == true)
            {
                job();
            }
            else
            {
                queuedJobs.Enqueue(job);
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look<BSCache>(ref scribedCache, saveDestroyedThings: false, "BS_scribedCache", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                queuedJobs.Clear();
                schedulePostUpdate.Clear();
                scheduleFullUpdate.Clear();
                currentlyQueued.Clear();
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
            globalRandNum = Rand.Value;

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
            if (currentTick % 1000 != 0)
            {
                if (BigSmallMod.settings.jesusMode)
                {
                    // Set all needs to full, and mood to max for testing purposes.
                    var allPawns = PawnsFinder.AllMapsAndWorld_Alive;
                    foreach (var pawn in allPawns.Where(x => x != null && !x.Discarded && !x.Destroyed))
                    {
                        pawn.needs?.AllNeeds?.ForEach(x => x.CurLevel = x.MaxLevel);
                    }
                }
            }

            //if (currentTick % 500 == 0)
            //{
            //    HumanoidPawnScaler.permitThreadedCaches = true;
            //}


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
        static bool threadInit = false;
        [ThreadStatic]
        static PerThreadMiniCache _tCache;
        [ThreadStatic]
        static bool threadStaticCacheInitialized;
        [ThreadStatic]
        static Dictionary<int, BSCache> _tDictCache;
        [ThreadStatic]
        static int dictCalls = 0;
        const int maxThreadDictUses = 100;


        public static BSCache GetCacheUltraSpeed(Pawn pawn, bool canRegenerate = false)
        {
            //if (_tCache.pawn == pawn) return BSCache.defaultCache;
            if (_tCache.pawn != pawn)
            {
                _tCache.pawn = pawn;
                if (!threadInit)
                {
                    _tDictCache = [];
                    threadInit = true;
                }
                if (_tDictCache.TryGetValue(pawn.thingIDNumber, out BSCache cache))
                {
                    _tCache.cache = cache;
                    dictCalls++;
                    if (dictCalls > maxThreadDictUses)
                    {
                        _tDictCache.Clear();
                        dictCalls = 0;
                    }
                    return cache;
                }
                _tCache.cache = GetCache(pawn, canRegenerate: canRegenerate);
                return _tCache.cache;
            }
            else return _tCache.cache;
        }
        [Obsolete]
        public static BSCache GetBSDict(Pawn pawn, bool forceRefresh = false, bool canRegenerate = true, int scheduleForce = -1)
        {
            return GetCache(pawn, forceRefresh, canRegenerate, scheduleForce);
        }

        public static void ShedueleForceRegenerateSafe(Pawn pawn, int tick)
        {
            var cache = GetCache(pawn, canRegenerate: false);
            ShedueleForceRegenerate(cache, tick);
        }

        /// <summary>
        /// Get the cache later, unless paused, then get it now. Mostly to force updates when in character editor, etc.
        /// </summary>
        public static BSCache LazyGetCache(Pawn pawn, int scheduleForce = 10) //, bool debug=false)
        {
            if (Find.TickManager?.Paused == true || Find.TickManager?.NotPlaying == true)
            {
                return GetCache(pawn, forceRefresh: true);
            }
            ShedueleForceRegenerateSafe(pawn, scheduleForce);
            return GetCache(pawn, canRegenerate: false);
        }

        /// <summary>
        /// Note that if the pawn is "null" then it will use a generic cache with default values. This is mostly to just get rid of the need to
        /// null-check everything that calls this method.
        /// </summary>
        /// <returns></returns>
        public static BSCache GetCache(Pawn pawn, bool forceRefresh = false, bool canRegenerate=true, int scheduleForce=-1, bool reevaluateGraphics=false) //, bool debug=false)
        {
            if (pawn == null)
            {
                return BSCache.defaultCache;
            }

            bool newEntry;
            BSCache result;
            if (canRegenerate && RunNormalCalculations(pawn))
            {
                result = GetCacheInner(pawn, out newEntry, forceRefresh: forceRefresh, canRegenerate: true);
            }
            else
            {
                // Unless values have already been set, this will just be a cache with default values.
                result = GetCacheInner(pawn, out newEntry, forceRefresh, canRegenerate: false);
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
                SafeGfxDirty(pawn);
            }
            else if (reevaluateGraphics)
            {
                result.ReevaluateGraphics();
            }
            if (scheduleForce > -1)
            {
                ShedueleForceRegenerate(result, scheduleForce);
            }

            return result;

            static void SafeGfxDirty(Pawn pawn)
            {
                if (pawn.Spawned) // && UnityData.IsInMainThread)//Thread.CurrentThread == BigSmall.mainThread)
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
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
        public static BSCache defaultCache = new() { isDefaultCache = true };
        public bool isDefaultCache = false;

        public Pawn pawn = null;
        public bool refreshQueued = false;
        public int? lastUpdateTick = null;
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

        
        public float minimumLearning = 0;
        public float growthPointGain = 1;
        //public float foodNeedCapacityMult = 1;
        //public float? previousFoodCapacity = null;

        public bool preventHeadScaling = false;
        public bool bodyConstantHeadScale = false;
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
        public float alcoholmAmount = 0;
        public RomanceTags romanceTags = null;

        public ApparelRestrictions apparelRestrictions = null;
        //public bool canWearApparel = true;
        //public bool canWearClothing = true;
        //public bool canWearArmor = true;

        public bool injuriesRescaled = false;
        public bool isUnliving = false;
        public BleedRateState bleedRate = BleedRateState.Unchanged;
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

        public bool isDrone = false;
        public List<Aptitude> aptitudes = [];

        public List<GeneDef> endogenesRemovedByRace = [];
        public List<GeneDef> xenoenesRemovedByRace = [];

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
            //Scribe_Values.Look(ref apparentGender, "BS_ApparentGender", null);

            //Scribe_Deep.Look(ref apparelRestrictions, "BS_ApparelRestrictions");

            Scribe_Values.Look(ref minimumLearning, "BS_MinimumLearning", 0.35f);
            Scribe_Values.Look(ref headSizeMultiplier, "BS_HeadSizeMultiplier", 1);
            Scribe_Values.Look(ref isBloodFeeder, "BS_IsBloodFeeder", false);
            Scribe_Values.Look(ref hasSizeAffliction, "BS_HasSizeAffliction", false);
            Scribe_Values.Look(ref attackSpeedMultiplier, "BS_AttackSpeedMultiplier", 1);
            Scribe_Values.Look(ref alcoholmAmount, "BS_AlcoholAmount", 0);
            
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


            //Scribe_Values.Look(ref bodyPosOffset, "BS_BodyPosOffset", 0);
            //Scribe_Values.Look(ref headPosMultiplier, "BS_HeadPosMultiplier", 1);

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
            if (pawn == null) { throw new Exception("Big & Small: Cannot regenerate Pawn Cache because the Pawn is null."); }
            if (regenerationInProgress || RaceMorpher.runningRaceSwap)
            {
                HumanoidPawnScaler.GetCache(pawn, scheduleForce: 10);
                return false;
            }
            regenerationInProgress = true;
            try
            {
                int tick = Find.TickManager.TicksGame;
                DevelopmentalStage dStage;
                try
                {
                    dStage = pawn.DevelopmentalStage;
                    developmentalStage = dStage;
                }
                catch
                {
                    Log.Warning($"[BigAndSmall] caught an exception when fetching Developmental Stage for {pawn.Name} Aborting generation of pawn cache.\n" +
                        $"This likely means the pawn lacks \"lifeStageAges\" or another requirement for fetching the age is missing.");
                    return false;
                }
                isHumanlike = pawn.RaceProps?.Humanlike == true;
                originalThing ??= pawn.def;
                var raceTrackers = pawn.GetRaceTrackers();

                raceTrackerHistory.AddRange(raceTrackers.Select(x => x.def));

                var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
                var allPawnExt = ModExtHelper.GetAllPawnExtensions(pawn);
                var racePawnExts = pawn.GetRacePawnExtensions();
                var nonRacePawnExt = ModExtHelper.GetAllPawnExtensions(pawn, parentBlacklist: [typeof(RaceTracker)]);
                preventHeadScaling = allPawnExt.Any(x => x.preventHeadScaling);
                bodyConstantHeadScale = allPawnExt.Any(x => x.bodyConstantHeadScale);


                CalculateGenderAndApparentGender(allPawnExt);

                bool hasSizeAffliction = ScalingMethods.CheckForSizeAffliction(pawn);
                CalculateSize(dStage, allPawnExt, hasSizeAffliction);

                if (isHumanlike)
                {
                    pawnDiet = nonRacePawnExt.Where(x => x.pawnDiet != null).Select(x => x.pawnDiet).ToList();
                    if (racePawnExts.Any(x => x.pawnDiet != null) && !nonRacePawnExt.Any(x => x.pawnDietRacialOverride))
                    {
                        pawnDiet.AddRange(racePawnExts.Where(x => x.pawnDiet != null).Select(x => x.pawnDiet));
                    }
                    var activeGenedefs = activeGenes.Select(x => x.def).ToList();
                    newFoodCatAllow = BSDefLibrary.FoodCategoryDefs.Where(x => x.DefaultAcceptPawn(pawn, activeGenedefs, pawnDiet).Fuse(pawnDiet.Select(y => y.AcceptFoodCategory(x))).ExplicitlyAllowed()).ToList();
                    newFoodCatDeny = BSDefLibrary.FoodCategoryDefs.Where(x => x.DefaultAcceptPawn(pawn, activeGenedefs, pawnDiet).Fuse(pawnDiet.Select(y => y.AcceptFoodCategory(x))).NeutralOrWorse()).ToList();

                    // Log what filtered it
                    //BSDefLibrary.FoodCategoryDefs.ForEach(x =>
                    //{
                    //    var result = x.DefaultAcceptPawn(pawn, activeGenedefs, pawnDiet);
                    //    var result2 = pawnDiet.Select(y => y.AcceptFoodCategory(x)).Fuse();
                    //    Log.Message($"[Allow] {pawn} can {(result.Fuse(result2).ExplicitlyAllowed() ? "eat" : "not eat")} {x.defName} because {result} and {result2}");
                    //    Log.Message($"[DENY] {pawn} can {(result.Fuse(result2).NotExplicitlyAllowed() ? "not eat" : "eat")} {x.defName} because {result} and {result2}");
                    //});
                    //var whiteListed = pawnDiet.

                    ApparelRestrictions appRestrict = new();
                    var appRestrictList = allPawnExt.Where(x => x.apparelRestrictions != null).Select(x => x.apparelRestrictions).ToList();
                    if (appRestrictList.Count > 0)
                    {
                        appRestrict = appRestrictList.Aggregate(appRestrict, (acc, x) => acc.MakeFusionWith(x));
                        apparelRestrictions = appRestrict;
                    }
                    else
                    {
                        apparelRestrictions = null;
                    }
                }

                aptitudes = allPawnExt.Where(x => x.aptitudes != null).SelectMany(x => x.aptitudes).ToList();

                //diet = GameUtils.GetDiet(pawn);

                float minimumLearning = pawn.GetStatValue(BSDefs.SM_Minimum_Learning_Speed);

                // Traits on pawn
                var traits = pawn.story?.traits?.allTraits;

                // Hediff Caching
                var hediffs = pawn.health?.hediffSet.hediffs;
                bool willBecomeUndead = hediffs.Any(x => x.def.defName == "VU_DraculVampirism" || x.def.defName == "BS_ReturnedReanimation") == true;


                // Get all genes with the GeneExtension
                // Gene Caching
                var undeadGenes = GeneHelpers.GetActiveGenesByNames(pawn,
                [
                    "VU_Unliving", "VU_Lesser_Unliving_Resilience", "VU_Unliving_Resilience", "BS_RoboticResilienceLesser", "BS_RoboticResilience", "BS_IsUnliving"
                ]);

                bool animalReturned = pawn.health.hediffSet.HasHediff(BSDefs.VU_AnimalReturned);
                bool animalVampirism = pawn.health.hediffSet.HasHediff(BSDefs.VU_DraculAnimalVampirism);

                bool animalUndead = animalReturned || animalVampirism;

                bool isAgeless = activeGenes.Any(x => x.def == BSDefs.Ageless);
                bool isNonsenescent = activeGenes.Any(x => x.def == BSDefs.DiseaseFree);
                float age = pawn.ageTracker.AgeBiologicalYearsFloat;
                if (age > 18 && isAgeless)  // Stop ageless pawns hitting on teenagers all the time.
                {
                    apparentAge = Mathf.Clamp(age, 30, 60);
                }
                else if (isNonsenescent)
                {
                    apparentAge = Mathf.Min(age, 60);
                }
                else // Does it _really_ matter if you are a 120 or 180 year old cyborg?
                {
                    apparentAge = Mathf.Min(age, 80);
                }



                bool noBlood = activeGenes.Any(x => x.def.defName == "VU_NoBlood") || animalReturned;
                bool verySlowBleeding = animalVampirism;
                bool slowBleeding = activeGenes.Any(x => x.def.defName == "BS_SlowBleeding");

                BleedRateState bleedState = noBlood ? BleedRateState.NoBleeding
                                                  : verySlowBleeding ? BleedRateState.VerySlowBleeding
                                                  : slowBleeding ? BleedRateState.SlowBleeding
                                                  : BleedRateState.Unchanged;

                if (raceTrackers.Any(x => x.CurStage is HediffStage hs && hs.totalBleedFactor == 0))
                {
                    bleedState = BleedRateState.NoBleeding;
                }

                // Has Deathlike gene or VU_AnimalReturned Hediff.
                bool succubusUnbonded = false;
                if (activeGenes.Any(x => x.def.defName == "VU_LethalLover"))
                {
                    // Check if psychic bond is active
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) == null)
                    {
                        succubusUnbonded = true;
                    }
                }

                isMechanical = allPawnExt.Any(x => x.isMechanical);
                bool everFertile = activeGenes.Any(x => x.def.defName == "BS_EverFertile");
                animalFriend = pawn.story?.traits?.allTraits.Any(x => !x.Suppressed && x.def.defName == "BS_AnimalFriend") == true || isMechanical;



                //facialAnimationDisabled = activeGenes.Any(x => x.def == BSDefs.BS_FacialAnimDisabled);
                facialAnimationDisabled = allPawnExt.Any(x => x.disableFacialAnimations || x.facialDisabler != null)
                    || facialAnimationDisabled_Transform;

                // Add together bodyPosOffset from GeneExtension.
                float bodyPosOffset = allPawnExt.Sum(x => x.bodyPosOffset);
                float headPosMultiplier = 1 + allPawnExt.Sum(x => x.headPosMultiplier);
                bool preventDisfigurement = allPawnExt.Any(x => x.preventDisfigurement);

                var alcoholHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AlcoholHigh);
                float alcoholLevel = alcoholHediff?.Severity ?? 0;
                alcoholmAmount = alcoholLevel;

                this.minimumLearning = minimumLearning;
                this.growthPointGain = pawn.GetStatValue(BSDefs.SM_GrowthPointAccumulation);
                //this.foodNeedCapacityMult = pawn.GetStatValue(BSDefs.SM_Food_Need_Capacity);

                isBloodFeeder = IsBloodfeederPatch.IsBloodfeeder(pawn) || allPawnExt.Any(x => x.isBloodfeeder);
                this.hasSizeAffliction = hasSizeAffliction;
                attackSpeedMultiplier = pawn.GetStatValue(BSDefs.SM_AttackSpeed);
                attackSpeedUnarmedMultiplier = pawn.GetStatValue(BSDefs.SM_UnarmedAttackSpeed);

                isDrone = allPawnExt.Any(x => x.isDrone);


                // Check if they are a shambler
                var isShambler = pawn?.mutant?.Def?.defName?.ToLower().Contains("shambler") == true;
                // Check if it has the "ShamblerCorpse" hediff
                isShambler = isShambler || pawn.health.hediffSet.HasHediff(HediffDefOf.ShamblerCorpse);

                //isMechanical = geneExts.Any(x => x.isMechanical) || raceCompProps.isMechanical;
                isUnliving = undeadGenes.Count > 0 || animalUndead || isShambler || allPawnExt.Any(x => x.isUnliving);
                willBeUndead = willBecomeUndead;
                bleedRate = bleedState;
                deathlike = animalUndead || allPawnExt.Any(x => x.isDeathlike);

                unarmedOnly = allPawnExt.Any(x => x.unarmedOnly || x.forceUnarmed) ||
                                activeGenes.Any(x => new List<string> { "BS_UnarmedOnly", "BS_NoEquip", "BS_UnarmedOnly_Android" }.Contains(x.def.defName));

                this.succubusUnbonded = succubusUnbonded;
                romanceTags = allPawnExt.Select(x => x.romanceTags).Where(x => x != null)?.GetMerged();
                if (romanceTags == null && HumanLikes.Humanlikes.Contains(pawn?.def) || racePawnExts.All(x => x.romanceTags == null))
                {
                    romanceTags = RomanceTags.simpleRaceDefault;
                }
                pregnancySpeed = allPawnExt.Aggregate(1f, (acc, x) => acc * x.pregnancySpeedMultiplier);
                this.everFertile = everFertile;
                renderCacheOff = allPawnExt.Any(x => x.renderCacheOff);

                this.bodyPosOffset = bodyPosOffset;

                raidWealthMultiplier = pawn.GetStatValue(StatDef.Named("SM_RaidWealthMultiplier"));
                raidWealthOffset = pawn.GetStatValue(StatDef.Named("SM_RaidWealthOffset"));

                bodyRenderSize = GetBodyRenderSize();
                headRenderSize = GetHeadRenderSize();
                headPositionMultiplier = CalculateHeadOffset(headPosMultiplier);
                SetWorldOffset();

                complexHeadOffsets = allPawnExt.Select(x => x.headDrawData).Where(x => x != null).ToList().GetCombinedOffsetsByRot(headPositionMultiplier);
                complexBodyOffsets = allPawnExt.Select(x => x.bodyDrawData).Where(x => x != null).ToList().GetCombinedOffsetsByRot();

                // Check if the body size, head size, body offset, or head position has changed. If not set approximatelyNoChange to false.
                approximatelyNoChange = bodyRenderSize.Approx(1) && headRenderSize.Approx(1) && bodyPosOffset.Approx(0) &&
                    headPosMultiplier.Approx(1) && headPositionMultiplier.Approx(1) && worldspaceOffset.Approx(0) &&
                    complexHeadOffsets == null && complexBodyOffsets == null;

                hasComplexHeadOffsets = complexHeadOffsets != null;

                if (isHumanlike)
                {
                    ReevaluateGraphics(nonRacePawnExt, racePawnExts);
                }

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
                    BigAndSmallCache.scribedCache = [];
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

        private void CalculateGenderAndApparentGender(List<PawnExtension> allPawnExt)
        {
            var forcedGender = allPawnExt.Any(x => x.forceGender != null) ? allPawnExt.First(x => x.forceGender != null).forceGender : null;
            forcedGender = allPawnExt.Any(x => x.ignoreForceGender) ? null : forcedGender;
            if (forcedGender != null && forcedGender != pawn.gender)
            {
                pawn.gender = forcedGender.Value;
            }
            apparentGender = GetApparentGender(allPawnExt);
        }

        private Gender? GetApparentGender(List<PawnExtension> allExts = null)
        {
            return allExts.FirstOrDefault(x => x.ApparentGender != null)?.ApparentGender;
        }

        public void ReevaluateGraphics(List<PawnExtension> otherExts = null, List<PawnExtension> raceExts = null)
        {
            if (otherExts == null || raceExts == null)
            {
                otherExts = ModExtHelper.GetAllExtensions<PawnExtension>(pawn);

                raceExts = pawn.GetRacePawnExtensions();
            }
            headMaterial = null; bodyMaterial = null;
            headGraphicPath = null; bodyGraphicPath = null;

            //apparentGender = allPawnExt.FirstOrDefault(x => x.ApparentGender != null)?.ApparentGender;
            CalculateGenderAndApparentGender([..otherExts, ..raceExts]);

            int pawnRNGSeed = pawn.GetPawnRNGSeed();

            var extensionsWithHeadPaths = otherExts.Where(x => x.headPaths.ValidFor(this));

            // Only use  race props if there are no other extensions with head paths.
            extensionsWithHeadPaths = extensionsWithHeadPaths.EnumerableNullOrEmpty() ? raceExts.Where(x => x.headPaths.ValidFor(this)) : extensionsWithHeadPaths;
            PawnExtension headGfxExt = null;
            if (extensionsWithHeadPaths.EnumerableNullOrEmpty() == false)
            {
                using (new RandBlock(pawnRNGSeed))
                {
                    headGfxExt = extensionsWithHeadPaths.RandomElement();
                    headGraphicPath = headGfxExt.headPaths.GetPaths(this, forceGender: apparentGender).RandomElement();
                }
                if (headGfxExt.headMaterial != null) headMaterial = headGfxExt.headMaterial;
                headDessicatedGraphicPath = headGfxExt.GetDessicatedFromHeadPath(headGraphicPath);
            }


            var extensionsWithBodyPaths = otherExts.Where(x => x.bodyPaths.ValidFor(this));
            extensionsWithBodyPaths = extensionsWithBodyPaths.EnumerableNullOrEmpty() ? raceExts.Where(x => x.bodyPaths.ValidFor(this)) : extensionsWithBodyPaths;

            PawnExtension bodyGfxExt = null;
            if (extensionsWithBodyPaths.EnumerableNullOrEmpty() == false)
            {
                using (new RandBlock(pawnRNGSeed))
                {
                    bodyGfxExt = extensionsWithBodyPaths.RandomElement();
                }
                if (headGfxExt != null && headGfxExt.bodyPaths.ValidFor(this)) bodyGfxExt = headGfxExt; // Override with the active headGfxExt if it has valid body paths.
                using (new RandBlock(pawnRNGSeed))
                {
                    bodyGraphicPath = bodyGfxExt.bodyPaths.GetPaths(this, forceGender: apparentGender).RandomElement();
                }
                if (bodyGfxExt.bodyMaterial != null) bodyMaterial = bodyGfxExt.bodyMaterial;
                bodyDessicatedGraphicPath = bodyGfxExt.GetDessicatedFromBodyPath(bodyGraphicPath);
            }
            var btpg = new BodyTypesPerGender();
            btpg.AddRange(otherExts.SelectMany(x => x.bodyTypes));
            if (btpg.Count == 0) btpg.AddRange(raceExts.SelectMany(x => x.bodyTypes));
            bodyTypeOverride = btpg.Count == 0 ? null : btpg;

            // Set body/hair/etc. from mostly other sources.
            if (this != defaultCache && pawn?.story?.bodyType != null && pawn?.story?.headType != null)
            {
                GenderMethods.UpdateBodyHeadAndBeardPostGenderChange(this);
            }
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
            PrerequisiteGeneUpdate();

            pawn.def.modExtensions?.OfType<RaceExtension>()?.FirstOrDefault()?.ApplyTrackerIfMissing(pawn, this);

            var racePawnExts = pawn.GetRacePawnExtensions();
            var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
            var otherPawnExts = ModExtHelper.GetAllExtensions<PawnExtension>(pawn, parentBlacklist: [typeof(RaceTracker)]);
            List<PawnExtension> allPawnExts = [..racePawnExts, ..otherPawnExts];
            //List<PawnExtension> geneExts = activeGenes
            //    .Where(x => x?.def?.modExtensions != null && x.def.modExtensions.Any(y => y is PawnExtension))?
            //    .Select(x => x.def.GetModExtension<PawnExtension>()).ToList();


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

            if (pawn.apparel?.WornApparel != null && pawn.apparel.WornApparel.Count > 0)
            {
                if (selfRepairingApparel || indestructibleApparel)
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
                if (apparelRestrictions != null)
                {
                    List<Apparel> apparelToRemove = [];
                    apparelToRemove.AddRange(from app in pawn.apparel.WornApparel
                                             where app?.def != null && apparelRestrictions.CanWear(app.def) != null
                                             select app);
                    if (apparelToRemove.Count > 0)
                    {
                        for (int idx = apparelToRemove.Count - 1; idx >= 0; idx--)
                        {
                            Apparel apItem = apparelToRemove[idx];
                            try
                            {
                                // If colonist, or prisoner
                                if (pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony)
                                {
                                    // Drop the item if it's not allowed.
                                    pawn.apparel.TryDrop(apItem);
                                }
                                else
                                {
                                    // If not, just remove it.
                                    pawn.apparel.Remove(apItem);
                                }
                            }
                            catch
                            {
                                Log.Warning($"[BigAndSmall] Failed to remove apparel {apItem} from {pawn.Name}.");
                            }
                        }
                    }
                }

            }
            banAddictions = allPawnExts.Any(x => x.banAddictionsByDefault);

            try
            {
                SimpleRaceUpdate(racePawnExts, otherPawnExts, pawn.GetRaceCompProps());
            }
            catch (Exception)
            {
                if (!BigAndSmallCache.regenerationAttempted)
                {
                    Log.Warning($"Issue updating RaceCache of {pawn} ({id}). Cleaing and regenerating cache.");
                    // Reassigning instead of clearing in case it is null for some reason.
                    HumanoidPawnScaler.Cache = new ConcurrentDictionary<Pawn, BSCache>();
                    BigAndSmallCache.scribedCache = [];
                    BigAndSmallCache.regenerationAttempted = true;
                }
                throw;
            }
            pawn.skills?.DirtyAptitudes();
        }

        [Unsaved]
        public HashSet<Gene> lastActiveGenes = [];
        [Unsaved]
        public HashSet<Gene> lastInactiveGenes = [];

        // Ideally this mess should be refactored and fixed. This is a workaround for the logic mess that has built up over 2 years
        // and the various framework merges.
        public void PrerequisiteGeneUpdate()
        {
            (HashSet<Gene> lActiveGenes, HashSet<Gene> lInactiveGenes) = ([.. lastActiveGenes], lastInactiveGenes.Where(g => !lastActiveGenes.Contains(g)).ToHashSet());
            if (pawn?.genes != null)
            {
                var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
                var allGenes = pawn.genes.GenesListForReading;
                allGenes.Where(g => g is PGene pGene).Cast<PGene>().ToList().ForEach(pg => pg.ForceRun = true);

                lastActiveGenes = activeGenes;
                lastInactiveGenes = allGenes.Where(x => !lastActiveGenes.Contains(x)).ToHashSet();

                
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    bool wasActive = lActiveGenes.Contains(gene);
                    bool isActive = activeGenes.Contains(gene);
                    if (wasActive != isActive)
                    {
                        GeneEffectManager.RefreshGeneEffects(gene, active: isActive);
                    }
                }
            }
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
