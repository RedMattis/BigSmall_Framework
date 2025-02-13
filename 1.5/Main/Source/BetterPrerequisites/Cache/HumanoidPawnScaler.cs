using BetterPrerequisites;
using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;


namespace BigAndSmall
{
    public static class BS
    {
        public static BSSettings Settings => BigSmallMod.settings;

        private static int internalTick = 0;

        /// <summary>
        /// Used when you need to make sure ticks aren't randomly skipped. Thanks Ludeon or whatever mod causes this. Ó_ò
        /// </summary>
        public static int Tick { get => internalTick; }
        public static void IncrementTick() => internalTick += 1;
        public static void SetTick(int tick) => internalTick = tick;
    }
    public class BigAndSmallCache : GameComponent
    {
        public static BigAndSmallCache instance = null;
        public static HashSet<PGene> pGenesThatReevaluate = [];

        private HashSet<BSCache> scribedCache = [];
        public static bool regenerationAttempted = false;
        public Game game;

        public static Queue<Pawn> refreshQueue = new();

        public static Queue<Action> queuedJobs = new();

        public static Dictionary<int, HashSet<BSCache>> schedulePostUpdate = [];
        public static Dictionary<int, HashSet<BSCache>> scheduleFullUpdate = [];
        public static HashSet<BSCache> currentlyQueued = []; // Ensures we never queue the same cache twice.

        public static float globalRandNum = 1;

        public static HashSet<BSCache> ScribedCache { get => instance.scribedCache; set => instance.scribedCache = value; }

        public BigAndSmallCache(Game game)
        {
            this.game = game;
            instance = this;
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // Get all pawns registered in the game.
            var allPawns = PawnsFinder.All_AliveOrDead;

            RaceFuser.PostSaveLoadedSetup();
            foreach (var pawn in allPawns.Where(x => x != null && !x.Discarded && !x.Destroyed))
            {
                if (HumanoidPawnScaler.GetCache(pawn, scheduleForce:1) is BSCache cache) { }
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
            }
        }

        
        public override void GameComponentTick()
        {
            BS.IncrementTick();
            int tick = BS.Tick;
            
            if (queuedJobs.Count > 0)
            {
                var job = queuedJobs.Dequeue();
                job();
            }

            //int currentTick = Find.TickManager.TicksGame;
            

            if (schedulePostUpdate.ContainsKey(tick))
            {
                foreach (var cache in schedulePostUpdate[tick])
                {
                    cache?.DelayedUpdate();
                }
                schedulePostUpdate.Remove(tick);
            }
            if (scheduleFullUpdate.ContainsKey(tick))
            {
                
                foreach (var cache in scheduleFullUpdate[tick])
                {
                    var cPawn = cache?.pawn;
                    try
                    {
                        if (cPawn != null && !cPawn.Discarded)
                        {
                            HumanoidPawnScaler.GetCache(cPawn, forceRefresh: true);
                        }
                    }
                    finally
                    {
                        currentlyQueued.Remove(cache);
                    }
                }
                scheduleFullUpdate.Remove(tick);
            }

            if (tick % 100 == 0)
            {
                SlowUpdate(tick);
            }
            
        }

        private static void SlowUpdate(int currentTick)
        {
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
                if (cachedPawn != null && (cachedPawn.Spawned || cachedPawn.Corpse?.Spawned == true))
                {
                    HumanoidPawnScaler.GetCache(cachedPawn, forceRefresh: true);
                }
            }
            if (BigSmallMod.settings.jesusMode)
            {
                if (currentTick % 1000 == 0)
                {
                    // Set all needs to full, and mood to max for testing purposes. Avoids mental breaks when testing, etc.
                    var allPawns = PawnsFinder.AllMapsAndWorld_Alive;
                    foreach (var pawn in allPawns.Where(x => x != null && !x.Discarded && !x.Destroyed))
                    {
                        pawn.needs?.AllNeeds?.ForEach(x => x.CurLevel = x.MaxLevel);
                    }
                }
            }

            pGenesThatReevaluate = new HashSet<PGene>(pGenesThatReevaluate.Where(x => x != null && x.pawn != null && !x.pawn.Discarded));
            foreach (var pGene in pGenesThatReevaluate.Where(x => x.pawn.Spawned))
            {
                bool active = pGene.previouslyActive == true;
                bool newState = pGene.RegenerateState();
                if (active != newState)
                {
                    pGene.pawn.genes.Notify_GenesChanged(pGene.def);
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
        /// ForceRefresh and get the cache... later, unless paused. If pasued get it now. Mostly to force updates when in character editor, etc.
        /// </summary>
        public static BSCache LazyGetCache(Pawn pawn, int scheduleForce = 10) //, bool debug=false)
        {
            if (Find.TickManager?.Paused == true || Find.TickManager?.NotPlaying == true)
            {
                return GetCache(pawn, forceRefresh: true);
            }
            var cache = GetCache(pawn, canRegenerate: false);
            ShedueleForceRegenerate(cache, scheduleForce);
            return cache;
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

                // Need to check performance of this carefully...
                if (result.isDefaultCache && CheckForScribedEntry(pawn) is BSCache scribedCache)
                {
                    result = scribedCache;
                }
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
                    BigAndSmallCache.ScribedCache.Add(result);
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
                if (pawn.Corpse?.Spawned == true)
                {
                    pawn.Corpse.RotStageChanged();
                }
            }
        }

        private static void ShedueleForceRegenerate(BSCache cache, int delay)
        {
            if (BigAndSmallCache.currentlyQueued.Contains(cache))
            {
                return;
            }
            Assert.IsTrue(delay > 0, "Delay must be greater than 0.");
            int targetTick = BS.Tick + delay;
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
            foreach (var cache in BigAndSmallCache.ScribedCache.Where(x => x.id == id))
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
                && (BigSmallMod.settings.scaleAnimals || pawn.RaceProps.Humanlike)
                && (pawn.needs != null || pawn.Dead);
        }
    }

    public partial class BSCache : IExposable, ICacheable
    {
        public static bool Compare(BSCache a, BSCache b)
        {
            return a.id == b.id;
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
                int tick = BS.Tick;
                creationTick ??= BS.Tick;
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
                bodyConstantHeadScaleBigOnly = allPawnExt.Any(x => x.bodyConstantHeadScaleBigOnly);

                preventHeadScalingFactor = allPawnExt.Where(x => x.preventHeadScalingFactor != null)
                    .DefaultIfEmpty(new PawnExtension { preventHeadScalingFactor = 1.0f })
                    .Average(x => x.preventHeadScalingFactor.Value);

                preventHeadOffsetFactor = allPawnExt.Where(x => x.preventHeadOffsetFactor != null)
                    .DefaultIfEmpty(new PawnExtension { preventHeadOffsetFactor = preventHeadScalingFactor })
                    .Average(x => x.preventHeadOffsetFactor.Value);



                CalculateGenderAndApparentGender(allPawnExt);

                bool hasSizeAffliction = ScalingMethods.CheckForSizeAffliction(pawn);
                CalculateSize(dStage, allPawnExt, hasSizeAffliction);

                if (isHumanlike)
                {
                    willEatDef.Clear();
                    pawnDiet = nonRacePawnExt.Where(x => x.pawnDiet != null).Select(x => x.pawnDiet).ToList();
                    if (racePawnExts.Any(x => x.pawnDiet != null) && !nonRacePawnExt.Any(x => x.pawnDietRacialOverride))
                    {
                        pawnDiet.AddRange(racePawnExts.Where(x => x.pawnDiet != null).Select(x => x.pawnDiet));
                    }
                    var activeGenedefs = activeGenes.Select(x => x.def).ToList();
                    newFoodCatAllow = BSDefLibrary.FoodCategoryDefs.Where(x => x.DefaultAcceptPawn(pawn, activeGenedefs, pawnDiet).Fuse(pawnDiet.Select(y => y.AcceptFoodCategory(x))).ExplicitlyAllowed()).ToList();
                    newFoodCatDeny = BSDefLibrary.FoodCategoryDefs.Where(x => x.DefaultAcceptPawn(pawn, activeGenedefs, pawnDiet).Fuse(pawnDiet.Select(y => y.AcceptFoodCategory(x))).NeutralOrWorse()).ToList();

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
                facialAnimationDisabled = allPawnExt.Any(x => x.disableFacialAnimations)
                    || facialAnimationDisabled_Transform;



                // Add together bodyPosOffset from GeneExtension.
                float bodyPosOffset = allPawnExt.Sum(x => x.bodyPosOffset);
                float headPosMultiplier = 1 + allPawnExt.Sum(x => x.headPosMultiplier);
                bool preventDisfigurement = allPawnExt.Any(x => x.preventDisfigurement);

                var alcoholHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AlcoholHigh);
                float alcoholLevel = alcoholHediff?.Severity ?? 0;
                alcoholAmount = alcoholLevel;

                this.minimumLearning = minimumLearning;
                this.growthPointGain = pawn.GetStatValue(BSDefs.SM_GrowthPointAccumulation);
                //this.foodNeedCapacityMult = pawn.GetStatValue(BSDefs.SM_Food_Need_Capacity);

                isBloodFeeder = IsBloodfeederPatch.IsBloodfeeder(pawn) || allPawnExt.Any(x => x.isBloodfeeder);
                this.hasSizeAffliction = hasSizeAffliction;
                attackSpeedMultiplier = pawn.GetStatValue(BSDefs.SM_AttackSpeed);
                attackSpeedUnarmedMultiplier = pawn.GetStatValue(BSDefs.SM_UnarmedAttackSpeed);

                isDrone = allPawnExt.Any(x => x.isDrone);

                this.disableLookChangeDesired = allPawnExt.Any(x => x.disableLookChangeDesired);

                // Check if they are a shambler
                var isShambler = pawn?.mutant?.Def?.defName?.ToLower().Contains("shambler") == true;
                // Check if it has the "ShamblerCorpse" hediff
                isShambler = isShambler || pawn.health.hediffSet.HasHediff(HediffDefOf.ShamblerCorpse);

                //isMechanical = geneExts.Any(x => x.isMechanical) || raceCompProps.isMechanical;
                isUnliving = undeadGenes.Count > 0 || animalUndead || isShambler || allPawnExt.Any(x => x.isUnliving);
                willBeUndead = willBecomeUndead;
                bleedRateFactor = allPawnExt.Any(x => x.bleedRate != null) ? allPawnExt.Where(x => x.bleedRate != null).Aggregate(1f, (acc, x) => acc * x.bleedRate.Value) : 1;
                if (bleedRateFactor == 0) bleedState = BleedRateState.NoBleeding;
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


                var forcedRot = allPawnExt.Select(x => x.forcedRotDrawMode).Where(x => x != null);
                forcedRotDrawMode = forcedRot.EnumerableNullOrEmpty() ? null : forcedRot.First();
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
                    BigAndSmallCache.ScribedCache = [];
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
            Gender? gender = allExts.FirstOrDefault(x => x.ApparentGender != null)?.ApparentGender;
            bool invertApparentGender = allExts.Any(x => x.invertApparentGender);

            if (gender != null && invertApparentGender) gender = gender == Gender.Male ? Gender.Female : Gender.Male;
            if (gender == null && invertApparentGender) gender = pawn.gender == Gender.Male ? Gender.Female : Gender.Male;
            return gender;
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
            HashSet<PawnExtension> allPawnExt = [.. otherExts, .. raceExts];

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

            // Stay on if it was on before.
            var faDisabler = allPawnExt.Where(x => x.facialDisabler != null).Select(x => x.facialDisabler);
            facialAnimationModified =  faDisabler.Any();
            if (faDisabler.Any())
            {
                NalsToggles.ToggleNalsStuff(pawn, faDisabler.First());
            }
        }

        

        public void ScheduleUpdate(int delayTicks)
        {
            int targetTick = BS.Tick + delayTicks;
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

            
            foreach(var gene in GeneHelpers.GetAllActiveGenes(pawn))
            {
                if (gene is PGene pGene)
                {
                    pGene.RefreshEffects();
                }
            }

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
                    BigAndSmallCache.ScribedCache = [];
                    BigAndSmallCache.regenerationAttempted = true;
                }
                throw;
            }
            pawn.skills?.DirtyAptitudes();

            var faDisabler = allPawnExts.Where(x => x.facialDisabler != null).Select(x => x.facialDisabler);
            facialAnimationModified = faDisabler.Any();
            if (faDisabler.Any())
            {
                NalsToggles.ToggleNalsStuff(pawn, faDisabler.First());
            }
        }

        [Unsaved]
        public HashSet<Gene> recordedActiveGenes = [];

        // Ideally this mess should be refactored and fixed. This is a workaround for the logic mess that has built up over 2 years
        // and the various framework merges.
        public void PrerequisiteGeneUpdate()
        {
            HashSet<Gene> lastActiveGenes = [.. recordedActiveGenes];
            if (pawn?.genes != null)
            {
                var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
                var allGenes = pawn.genes.GenesListForReading;
                allGenes.ForEach(g => { if (g is PGene pg) pg.ForceRun = true; });

                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    bool wasActive = lastActiveGenes.Contains(gene);
                    bool isActive = activeGenes.Contains(gene);
                    if (wasActive != isActive)
                    {
                        if (gene is PGene pGene)
                        {
                            pGene.ForceRun = true;
                        }
                        GeneEffectManager.RefreshGeneEffects(gene, active: isActive);
                    }
                }

                recordedActiveGenes = activeGenes;
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
