using BigAndSmall.FilteredLists;
using HarmonyLib;
using JetBrains.Annotations;
using Prepatcher;
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
    public class HumanoidPawnScaler : DictCache<Pawn, BSCache>
    {
        
        public static void ShedueleForceRegenerateSafe(Pawn pawn, int tick)
        {
            var cache = GetCache(pawn, canRegenerate: false);
            ShedueleForceRegenerate(cache, tick);
        }
        public struct PerThreadMiniCache
        {
            public Pawn pawn;
            public BSCache cache;
            public bool properCache;
        }
        [ThreadStatic]
        static bool threadInit = false;
        [ThreadStatic]
        static PerThreadMiniCache _tCache;
        [ThreadStatic]
        static Dictionary<int, BSCache> _tDictCache;
        [ThreadStatic]
        static int tick10 = 0;

        public static BSCache GetCacheUltraSpeed(Pawn pawn, bool canRegenerate = false)
        {
            if (_tCache.pawn != pawn || !_tCache.properCache)
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
                    if (BS.Tick10 != tick10)
                    {
                        tick10 = BS.Tick10;
                        _tDictCache.Clear();
                    }
                    return cache;
                }
                _tCache.cache = GetCache(pawn, canRegenerate: canRegenerate);
                _tCache.properCache = !_tCache.cache.isDefaultCache;
                return _tCache.cache;
            }
            else return _tCache.cache;
        }

        /// <summary>
        /// ForceRefresh and get the cache... later, unless paused. If pasued get it now. Mostly to force updates when in character editor, etc.
        /// </summary>
        public static BSCache GetInvalidateLater(Pawn pawn, int scheduleForce = 10)
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
                if (!result.isDefaultCache) // BS.PrePatcherActive && 
                {
                    pawn.GetCachePrepatchedThreaded() = result;
                    pawn.GetCachePrepatched() = result;
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

        // Used by the Scribe.
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
            if (regenerationInProgress)
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
                catch (Exception e)
                {
                    Log.Warning($"[BigAndSmall] caught an exception when fetching Developmental Stage for {pawn.Name} Aborting generation of pawn cache.\n" +
                        $"This likely means the pawn lacks \"lifeStageAges\" or another requirement for fetching the age is missing.\n{e.Message}\n{e.StackTrace}");
                    return false;
                }

                isHumanlike = pawn.RaceProps?.Humanlike == true;
                originalThing ??= pawn.def;

                if (changeIndex < uint.MaxValue)
                {
                    changeIndex++;
                }
                else
                {
                    changeIndex = 1; // It is only ever 0 if it is the default cache.
                }

                var raceTrackers = pawn.GetRaceTrackers();

                raceTrackerHistory.AddRange(raceTrackers.Select(x => x.def));

                var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
                var allPawnExt = ModExtHelper.GetAllPawnExtensions(pawn);

                //var allPawnExtPlusInactive = ModExtHelper.GetAllPawnExtensions(pawn, includeInactive: true, checkForExclusionTags:false);
                //List<PawnExtension> allInactivePawnExt = [.. allPawnExtPlusInactive.Except(allPawnExt)];

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
                    pawnDiet = [.. nonRacePawnExt.Where(x => x.pawnDiet != null).Select(x => x.pawnDiet)];
                    if (racePawnExts.Any(x => x.pawnDiet != null) && !nonRacePawnExt.Any(x => x.pawnDietRacialOverride))
                    {
                        pawnDiet.AddRange(racePawnExts.Where(x => x.pawnDiet != null).Select(x => x.pawnDiet));
                    }
                    var activeGenedefs = activeGenes.Select(x => x.def).ToList();
                    newFoodCatAllow = [.. BSDefLibrary.FoodCategoryDefs.Where(x => x.DefaultAcceptPawn(pawn, activeGenedefs, pawnDiet).Fuse(pawnDiet.Select(y => y.AcceptFoodCategory(x))).ExplicitlyAllowed())];
                    newFoodCatDeny = [.. BSDefLibrary.FoodCategoryDefs.Where(x => x.DefaultAcceptPawn(pawn, activeGenedefs, pawnDiet).Fuse(pawnDiet.Select(y => y.AcceptFoodCategory(x))).NotExplicitlyAllowed())];

                    ApparelRestrictions appRestrict = new();
                    bool canWieldGiant = pawn.story.traits.allTraits.Any(x =>
                           x.def.defName.ToLower().Contains("bs_giant")
                        || x.def.defName.ToLower().Contains("warcasket"))
                        || this.totalSize > 1.99f;

                    var appRestrictList = allPawnExt.Where(x => x.apparelRestrictions != null).Select(x => x.apparelRestrictions).ToList();
                    if (canWieldGiant)
                    {
                        appRestrictList.Add(new ApparelRestrictions() { tags = new() { acceptlist = ["BS_GiantWeapon"] } });
                    }
                    if (appRestrictList.Count > 0)
                    {
                        appRestrict = appRestrictList.Aggregate(appRestrict, (acc, x) => acc.MakeFusionWith(x));
                        apparelRestrictions = appRestrict;
                    }
                    else
                    {
                        apparelRestrictions = null;
                    }
                    if (allPawnExt.Any(x => x.animalFineManipulation != null))
                    {
                        fineManipulation = allPawnExt.Where(x => x.animalFineManipulation != null).Max(x => x.animalFineManipulation.Value);
                    }

                    canWield = allPawnExt.Any(x => x.canWieldThings == true) || !allPawnExt.Any(x => x.canWieldThings == false);
                }

                canUseChargers = allPawnExt.Any(x=>x.canUseChargers);
                
                if (canUseChargers)
                {
                    float efficiency = pawn.GetStatValue(BSDefs.BS_BatteryCharging, applyPostProcess: true);
                    
                    if (efficiency <= 0)
                    {
                        canUseChargers = false;
                        Log.WarningOnce($"[BigAndSmall] {pawn} has canUseChargers enabled but has 0 or negative BatteryChargingEfficiency. Disabling canUseChargers.", 14237890);
                    }
                    else
                    {
                        poorUserOfChargers = efficiency < 0.71f;
                    }
                }

                HandleSkillsAndAptitudes(allPawnExt);

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

                isMechanical = allPawnExt.Any(x => x.isMechanical) || pawn.RaceProps.IsMechanoid;

                SetupRacialFeatures(allPawnExt);

                bool everFertile = activeGenes.Any(x => x.def.defName == "BS_EverFertile");
                animalFriend = pawn.story?.traits?.allTraits.Any(x => !x.Suppressed && x.def.defName == "BS_AnimalFriend") == true || isMechanical;


                hideHumanlikeRenderNodes = allPawnExt.Any(x => x.hideHumanlikeRenderNodes);
                //facialAnimationDisabled = activeGenes.Any(x => x.def == BSDefs.BS_FacialAnimDisabled);
                facialAnimationDisabled = allPawnExt.Any(x => x.disableFacialAnimations)
                    || facialAnimationDisabled_Transform;

                var faDisabler = allPawnExt.Where(x => x.facialDisabler != null).Select(x => x.facialDisabler);
                facialAnimDisabler = faDisabler.FirstOrFallback(null);


                // Add together bodyPosOffset from GeneExtension.
                float bodyPosOffset = allPawnExt.Sum(x => x.bodyPosOffset);
                float headPosMultiplier = 1 + allPawnExt.Sum(x => x.headPosMultiplier);
                bool preventDisfigurement = allPawnExt.Any(x => x.preventDisfigurement);

                var alcoholHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AlcoholHigh);
                float alcoholLevel = alcoholHediff?.Severity ?? 0;
                alcoholAmount = alcoholLevel;

                this.minimumLearning = minimumLearning;
                this.growthPointGain = pawn.GetStatValue(BSDefs.SM_GrowthPointAccumulation);
                internalDamageDivisor = allPawnExt.Any(x => x.internalDamageDivisor != null)
                    ? allPawnExt.Where(x => x.internalDamageDivisor != null)
                    .Aggregate(1f, (acc, x) => acc * x.internalDamageDivisor.Value) : 1;



                if (allPawnExt.Count > 0 && allPawnExt.Any(x => x.canHavePassions == false))
                {
                    foreach (var pawnSkill in pawn.skills.skills)
                    {
                        pawnSkill.passion = Passion.None;
                    }
                }

                isBloodFeeder = IsBloodfeederPatch.IsBloodfeeder(pawn) || allPawnExt.Any(x => x.isBloodfeeder);
                this.hasSizeAffliction = hasSizeAffliction;
                attackSpeedMultiplier = pawn.GetStatValue(BSDefs.SM_AttackSpeed);
                attackSpeedUnarmedMultiplier = pawn.GetStatValue(BSDefs.SM_UnarmedAttackSpeed);

                isDrone = allPawnExt.Any(x => x.isDrone);
                noFamilyRelations = allPawnExt.Any(x => x.noFamilyRelations);
                isAmorphous = allPawnExt.Any(x => x.isAmorphous);

                this.disableLookChangeDesired = allPawnExt.Any(x => x.disableLookChangeDesired);

                var isShambler = pawn?.mutant?.Def?.defName?.ToLower().Contains("shambler") == true;
                isShambler = isShambler || pawn.health.hediffSet.HasHediff(HediffDefOf.ShamblerCorpse);

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
                if (
                    (romanceTags == null && HumanLikes.Humanlikes.Contains(pawn?.def)) ||
                    (racePawnExts.Any() && racePawnExts.All(x => x.romanceTags == null)) ||
                    pawn?.def == ThingDefOf.Human ||
                    pawn?.def == ThingDefOf.CreepJoiner
                    )
                {
                    romanceTags = RomanceTags.simpleRaceDefault;
                }
                pregnancySpeed = allPawnExt.Aggregate(1f, (acc, x) => acc * x.pregnancySpeedMultiplier);
                this.everFertile = everFertile;
                renderCacheOff = allPawnExt.Any(x => x.renderCacheOff);
                partsCanBeHarvested = allPawnExt.All(x => x.partsCanBeHarvested);

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
                if (forcedRotDrawMode != null)
                {
                    // Check so the pawn HAS a rot draw mode.
                    if (pawn?.RaceProps.corpseDef?.GetCompProperties<CompProperties_Rottable>() == null)
                    {
                        forcedRotDrawMode = null;
                    }
                }

                // Check if the body size, head size, body offset, or head position has changed. If not set approximatelyNoChange to false.
                approximatelyNoChange = bodyRenderSize.Approx(1) && headRenderSize.Approx(1) && bodyPosOffset.Approx(0) &&
                    headPosMultiplier.Approx(1) && headPositionMultiplier.Approx(1) && worldspaceOffset.Approx(0) &&
                    complexHeadOffsets == null && complexBodyOffsets == null && pawn.RaceProps.baseBodySize < 2;

                // Always disable for large pawns so Thrumbo etc. don't get cut off.
                if (pawn.RaceProps.baseBodySize > 1.49) renderCacheOff = true;

                hasComplexHeadOffsets = complexHeadOffsets != null;

                if (isHumanlike)
                {
                    ReevaluateGraphics(nonRacePawnExt, racePawnExts);
                }

                // More stuff should probably be moved here.
                ScheduleUpdate(1);
            }
            catch (Exception e)
            {
                // Remove the cache entry so it can be regenerated if this has not already been attempted.
                if (!BigAndSmallCache.regenerationAttempted)
                {
                    Log.Warning($"Issue reloading cache of {pawn} ({id}). Removing entire cache so it can be regenerated.\n{e.Message}\n{e.StackTrace}");
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

        private void SetupRacialFeatures(List<PawnExtension> allPawnExt)
        {
            racialFeatures = [];
            var pawnExtGroupedByFuseTag = allPawnExt
                .Where(x => x.fuseTag != null || x.featureInfo != null)
                .GroupBy(x => x.fuseTag);
            foreach (var group in pawnExtGroupedByFuseTag)
            {
                string name = group.Key;
                if (name == null)
                {
                    foreach (var item in group)
                    {
                        var featureInfo = item.featureInfo.SetupFromThis([item]);
                        racialFeatures.Add(featureInfo);
                    }
                }
                else
                {
                    var items = group.ToList();
                    RacialFeature featureData = items.FirstOrDefault(items => items.featureInfo != null)?.featureInfo;
                    if (featureData != null)
                    {
                        var featureInfo = featureData.SetupFromThis(items);
                        racialFeatures.Add(featureInfo);
                    }
                }
            }

            racialFeaturesAuto = [.. allPawnExt
                .Where(x => x.RacialFeaturesWithAuto != null)
                .SelectMany(x => x.RacialFeaturesWithAuto ?? [])];
        }

        public void HandleSkillsAndAptitudes(List<PawnExtension> allPawnExt)
        {
            
            aptitudes = [.. allPawnExt.Where(x => x.aptitudes != null).SelectMany(x => x.aptitudes)];
            if (allPawnExt.Any(x => x.clampedSkills != null))
            {
                Dictionary<SkillDef, IntRange> skillRanges = allPawnExt.Where(x => x.clampedSkills != null)
                    .SelectMany(x => x.clampedSkills)
                    .GroupBy(x => x.Skill)
                    .ToDictionary(g => g.Key, g => new IntRange(g.Min(x => x.Range.min), g.Max(x => x.Range.max)));
                foreach (var pawnSkill in pawn.skills.skills)
                {
                    if (skillRanges.TryGetValue(pawnSkill.def, out IntRange range))
                    {
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

            var explicitlyDisabled = allPawnExt.SelectMany(x => x.disabledWorkTypes ?? []).Distinct();
            if (pawn.skills?.skills != null && (
                aptitudes.Any()
                || explicitlyDisabled.Any()
                || disabledWorkTypes.Any()
                || allPawnExt.Any(x => x.disableSkillsWithMinus20Aptitude
                || x.disableSkillBelowAptitude != null)
                || skillsDisabledByExtensions.Any() == false))
            {
                if (pawn.cachedDisabledWorkTypes == null)
                {
                    pawn.GetDisabledWorkTypes(permanentOnly: false);
                    pawn.GetDisabledWorkTypes(permanentOnly: true);
                }
                bool skillsChanged = false;
                if (this.explicitlyDisabled.Count() != explicitlyDisabled.Count()
                    || this.explicitlyDisabled.Intersect(explicitlyDisabled).Count() != explicitlyDisabled.Count())
                {
                    this.explicitlyDisabled = [.. explicitlyDisabled];
                    skillsChanged = true;
                }

                HashSet<SkillDef> skillsDisabled = [];
                if (skillsChanged)
                {
                    foreach (var skill in pawn.skills.skills)
                    { 
                        var allAssociatedWorkTypes = DefDatabase<WorkTypeDef>.AllDefs.Where(wt => wt.relevantSkills.Contains(skill.def));
                        if (allAssociatedWorkTypes.Count() > 0
                            && explicitlyDisabled.Intersect(allAssociatedWorkTypes).Count() == allAssociatedWorkTypes.Count())
                        {
                            skillsDisabled.Add(skill.def);
                        }
                    }
                }

                HashSet<Aptitude> disabledByAptitude = [.. allPawnExt.SelectMany(x => x.disableSkillBelowAptitude ?? []).Distinct()];
                foreach (var skill in pawn.skills.skills)
                {
                    skill.aptitudeCached = null;  // Force recalculation of aptitude.
                    int limit = disabledByAptitude
                        .Where(x => x.skill == skill.def)
                        .Select(x => x.level)
                        .DefaultIfEmpty(-19)
                        .Min();
                    if (skill.Aptitude < limit)
                    {
                        skillsDisabled.Add(skill.def);
                        if (!skillsDisabledByExtensions.Contains(skill.def))
                        {
                            skillsChanged = true;
                        }
                    }
                }
                if (skillsDisabledByExtensions.NullOrEmpty() == false)
                {
                    if (skillsDisabledByExtensions.Any(x => skillsDisabled.Contains(x) == false))
                    {
                        skillsChanged = true;
                    }
                }
                skillsDisabledByExtensions = [.. skillsDisabled];
                if (skillsChanged)
                {
                    List<WorkTypeDef> affectedWorkTypes = [.. explicitlyDisabled];
                    foreach (var skillDef in skillsDisabled)
                    {
                        affectedWorkTypes.AddRange(DefDatabase<WorkTypeDef>.AllDefs.Where(wt => wt.relevantSkills.Contains(skillDef)));
                    }
                    disabledWorkTypes = [..affectedWorkTypes];
                    pawn.Notify_DisabledWorkTypesChanged();
                    //Log.Message($"DEBUG: Notified pawn {pawn} of disabled work types: {string.Join(", ", disabledWorkTypes.Select(x => x.defName))}");
                }
            }
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
            Gender? apGender = allExts.FirstOrFallback(x => x.ApparentGender != null, null)?.ApparentGender;
            bool invertApparentGender = allExts.Any(x => x.invertApparentGender);

            if (apGender != null && invertApparentGender) apGender = apGender == Gender.Male ? Gender.Female : Gender.Male;
            if (apGender == null && invertApparentGender) apGender = pawn.gender == Gender.Male ? Gender.Female : Gender.Male;
            return apGender;
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
            //HashSet<PawnExtension> allPawnExt = [.. otherExts, .. raceExts];

            //apparentGender = allPawnExt.FirstOrDefault(x => x.ApparentGender != null)?.ApparentGender;
            CalculateGenderAndApparentGender([.. otherExts, .. raceExts]);

            int pawnRNGSeed = pawn.GetPawnRNGSeed();

            SetPawnHeadAndBodyTextures(otherExts.ToHashSet(), raceExts.ToHashSet(), pawnRNGSeed);

            // Set BodyTypes
            var btpg = new BodyTypesPerGender();
            btpg.AddRange(otherExts.SelectMany(x => x.bodyTypes));
            if (btpg.Count == 0) btpg.AddRange(raceExts.SelectMany(x => x.bodyTypes));
            bool btoWasNull = bodyTypeOverride == null;
            bodyTypeOverride = btpg.Count == 0 ? null : btpg;

            // We just removed an override, to be safe we'll reset the body and then reeveluate it.
            // The reason for this is to make it possible to restore a customb body to a vanilla one.
            // Normally any custom body is otherwise rejected since it could be from another mod.
            if (bodyTypeOverride == null && btoWasNull == false)
            {
                PawnGenerator.GetBodyTypeFor(pawn);
            }

            // Set body/hair/etc. from mostly other sources.
            if (this != defaultCache && pawn?.story?.bodyType != null && pawn?.story?.headType != null)
            {
                GenderMethods.UpdateBodyHeadAndBeardPostGenderChange(this);
            }



            // Stay on if it was on before.
        }

        private void SetPawnHeadAndBodyTextures(HashSet<PawnExtension> otherPawnExt, HashSet<PawnExtension> fromRace, int pawnRNGSeed)
        {
            List<(AdaptivePathList, PawnExtension)> GetValidPaths(HashSet<PawnExtension> allPawnExt, Func<PawnExtension, AdaptivePathList> pathSelector, BSCache cache)
            {
                var validPaths = allPawnExt.Select(p => (pathSelector(p), p)).Where(x => x.Item1 != null && x.Item1.ValidFor(cache, apparentGender)).ToList();
                if (validPaths.Count == 0) return [];
                int bestScore = validPaths.Max(x => x.p.priority);
                return [.. validPaths.Where(x => x.p.priority == bestScore)];
            }
            var bestHeadPaths = GetValidPaths(otherPawnExt, x => x.headPaths, this);
            var bestHeadDeadPaths = GetValidPaths(otherPawnExt, x => x.headDessicatedPaths, this);
            var bestBodyPaths = GetValidPaths(otherPawnExt, x => x.bodyPaths, this);
            var bestBodyDeadPaths = GetValidPaths(otherPawnExt, x => x.bodyDessicatedPaths, this);

            bestHeadPaths = bestHeadPaths.Count == 0 ? GetValidPaths(fromRace, x => x.headPaths, this) : bestHeadPaths;
            bestHeadDeadPaths = bestHeadDeadPaths.Count == 0 ? GetValidPaths(fromRace, x => x.headDessicatedPaths, this) : bestHeadDeadPaths;
            bestBodyPaths = bestBodyPaths.Count == 0 ? GetValidPaths(fromRace, x => x.bodyPaths, this) : bestBodyPaths;
            bestBodyDeadPaths = bestBodyDeadPaths.Count == 0 ? GetValidPaths(fromRace, x => x.bodyDessicatedPaths, this) : bestBodyDeadPaths;

            headGraphicPath = null;
            bodyGraphicPath = null;
            headDessicatedGraphicPath = null;
            bodyDessicatedGraphicPath = null;

            headMaterial = otherPawnExt.FirstOrFallback(x => x.headMaterial != null, null)?.headMaterial;
            bodyMaterial = otherPawnExt.FirstOrFallback(x => x.bodyMaterial != null, null)?.bodyMaterial;

            headMaterial ??= fromRace.FirstOrFallback(x => x.headMaterial != null, null)?.headMaterial;
            bodyMaterial ??= fromRace.FirstOrFallback(x => x.bodyMaterial != null, null)?.bodyMaterial;

            // Prefer grabbing Head & Body from the same source
            PawnExtension bestHeadSrc = null;
            if (bestHeadPaths.Count != 0)
            {
                using (new RandBlock(pawnRNGSeed))
                {
                    var headPath = bestHeadPaths.RandomElement();
                    (headGraphicPath, bestHeadSrc) = (headPath.Item1.GetPaths(this, forceGender: apparentGender).RandomElement(), headPath.Item2);
                    if (headPath.Item2.headMaterial != null)
                    {
                        headMaterial = headPath.Item2.headMaterial;
                    }
                }
            }
            if (bestHeadDeadPaths.Count != 0)
            {
                if (bestHeadSrc != null && bestHeadSrc.headDessicatedPaths.ValidFor(this, apparentGender))
                {
                    using (new RandBlock(pawnRNGSeed))
                    {
                        headDessicatedGraphicPath = bestHeadSrc.headDessicatedPaths.GetPaths(this, forceGender: apparentGender).RandomElement();
                    }
                }
                else
                {
                    var headPath = bestHeadDeadPaths.RandomElement();
                    using (new RandBlock(pawnRNGSeed))
                    {
                        headDessicatedGraphicPath = headPath.Item1.GetPaths(this, forceGender: apparentGender).RandomElement();
                    }
                }
            }
            PawnExtension bestBodySrc = bestHeadSrc;
            if (bestBodyPaths.Count != 0)
            {
                if (bestHeadSrc != null && bestHeadSrc.bodyPaths.ValidFor(this, apparentGender))
                {
                    using (new RandBlock(pawnRNGSeed))
                    {
                        bodyGraphicPath = bestHeadSrc.bodyPaths.GetPaths(this, forceGender: apparentGender).RandomElement();
                        if (bestHeadSrc.bodyMaterial != null)
                        {
                            bodyMaterial = bestHeadSrc.bodyMaterial;
                        }
                    }
                }
                else
                {
                    var bodyPath = bestBodyPaths.RandomElement();
                    using (new RandBlock(pawnRNGSeed))
                    {
                        (bodyGraphicPath, bestBodySrc) = (bodyPath.Item1.GetPaths(this, forceGender: apparentGender).RandomElement(), bodyPath.Item2);
                        if (bodyPath.Item2.bodyMaterial != null)
                        {
                            bodyMaterial = bodyPath.Item2.bodyMaterial;
                        }
                    }
                }
            }
            if (bestBodyDeadPaths.Count != 0)
            {
                if (bestBodySrc != null && bestBodySrc.bodyDessicatedPaths.ValidFor(this, apparentGender))
                {
                    using (new RandBlock(pawnRNGSeed))
                    {
                        bodyDessicatedGraphicPath = bestBodySrc.bodyDessicatedPaths.GetPaths(this, forceGender: apparentGender).RandomElement();
                    }
                }
                else
                {
                    var bodyPath = bestBodyDeadPaths.RandomElement();
                    using (new RandBlock(pawnRNGSeed))
                    {
                        bodyDessicatedGraphicPath = bodyPath.Item1.GetPaths(this, forceGender: apparentGender).RandomElement();
                    }
                }
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

            //PrerequisiteGeneUpdate();

            pawn.def.modExtensions?.OfType<RaceExtension>()?.FirstOrDefault()?.ApplyTrackerIfMissing(pawn, this);

            var racePawnExts = pawn.GetRacePawnExtensions();
            var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
            var otherPawnExts = ModExtHelper.GetAllExtensions<PawnExtension>(pawn, parentBlacklist: [typeof(RaceTracker)]);
            List<PawnExtension> allPawnExts = [.. racePawnExts, .. otherPawnExts];

            allPawnExts.ForEach(x => x.transformGene?.TryTransform(pawn));

            if (noFamilyRelations)
            {
                for (int i = pawn.relations.DirectRelations.Count - 1; i >= 0; i--)
                {
                    // Check so the index exists in case a chain reaction occured and removed multiple.
                    if (pawn.relations.DirectRelations.Count > i)
                    {
                        var rel = pawn.relations.DirectRelations[i];
                        bool isParentRelation = rel.def == PawnRelationDefOf.Parent || rel.def == PawnRelationDefOf.Parent;
                        if (rel.def.implied || rel.def.inbredChanceOnChild > 0)
                        {
                            pawn.relations.TryRemoveDirectRelation(rel.def, rel.otherPawn);
                        }
                        if (isParentRelation)
                        {
                            var creatorRelation = BSDefs.BS_Creator;
                            if (creatorRelation != null)
                            {
                                pawn.relations.AddDirectRelation(creatorRelation, rel.otherPawn);
                            }
                        }
                    }
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

            UpdateFineManipulationHediffs(hediffs);

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
                            catch (Exception e)
                            {
                                // If it fails, log a warning.
                                Log.Warning($"[BigAndSmall] Failed to remove apparel {apItem} from {pawn.Name}.\n{e.Message}\n{e.StackTrace}");
                            }
                        }
                    }
                }
            }
            if (!canWield)
            {
                if (pawn.Spawned && (pawn.IsColonist || pawn.IsPrisonerOfColony))
                {
                    pawn.equipment.DropAllEquipment(pawn.Position, forbid: false);
                }
                else
                {
                    pawn.equipment?.DestroyAllEquipment();
                }
            }

            banAddictions = allPawnExts.Any(x => x.banAddictionsByDefault);

            try
            {
                SimpleRaceUpdate(racePawnExts, otherPawnExts, pawn.GetRaceCompProps());

            }
            catch (Exception e)
            {
                if (!BigAndSmallCache.regenerationAttempted)
                {
                    Log.Warning($"Issue updating RaceCache of {pawn} ({id}). Cleaing and regenerating cache.\n{e.Message}\n{e.StackTrace}");
                    // Reassigning instead of clearing in case it is null for some reason.
                    HumanoidPawnScaler.Cache = new ConcurrentDictionary<Pawn, BSCache>();
                    BigAndSmallCache.ScribedCache = [];
                    BigAndSmallCache.regenerationAttempted = true;
                }
                throw;
            }
            if (genesActivated.Any() || genesDeactivated.Any())
            {
                GeneHelpers.RefreshAllGenes(pawn, genesActivated, genesDeactivated);
                genesDeactivated.Clear();
                genesActivated.Clear();
            }
            pawn.skills?.DirtyAptitudes();
            if (allPawnExts.Any(x => x.removeTattoos))
            {
                pawn.style.BodyTattoo = null;
                pawn.style.FaceTattoo = null;
            }
        }

        private void UpdateFineManipulationHediffs(List<Hediff> hediffs)
        {
            HediffDef targetManipulationHediff = null;
            List<HediffDef> manipulationHediffs = [BSDefs.BS_NoHands, BSDefs.BS_PoorHands];
            if (fineManipulation != null)
            {
                if (fineManipulation < 0.45) targetManipulationHediff = BSDefs.BS_NoHands;
                else if (fineManipulation < 0.75) targetManipulationHediff = BSDefs.BS_PoorHands;
            }

            // Remove any manipulation hediffs that are not the target one.
            List<Hediff> hediffsToRemove = [];
            foreach (var hediff in hediffs.Where(x => manipulationHediffs.Contains(x.def)))
            {
                if (hediff.def != targetManipulationHediff)
                {
                    hediffsToRemove.Add(hediff);
                }
            }
            hediffsToRemove.ForEach(pawn.health.RemoveHediff);

            // Add the target manipulation hediff if it is not already present.
            if (targetManipulationHediff != null && !hediffs.Any(x => x.def == targetManipulationHediff))
            {
                pawn.health.AddHediff(targetManipulationHediff);
            }
        }

        [Unsaved]
        public HashSet<Gene> recordedActiveGenes = [];

        // Ideally this mess should be refactored and fixed. This is a workaround for the logic mess that has built up over 2 years
        // and the various framework merges.
        //public void PrerequisiteGeneUpdate()
        //{
        //    HashSet<Gene> lastActiveGenes = [.. recordedActiveGenes];
        //    if (pawn?.genes != null)
        //    {
        //        var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
        //        var allGenes = pawn.genes.GenesListForReading;
        //        allGenes.ForEach(g => { if (g is PGene pg) pg.ForceRun = true; });

        //        foreach (var gene in pawn.genes.GenesListForReading)
        //        {
        //            bool wasActive = lastActiveGenes.Contains(gene);
        //            bool isActive = activeGenes.Contains(gene);
        //            if (wasActive != isActive)
        //            {
        //                if (gene is PGene pGene)
        //                {
        //                    pGene.ForceRun = true;
        //                }
        //                GeneEffectManager.RefreshGeneEffects(gene, active: isActive);
        //            }
        //        }

        //        recordedActiveGenes = activeGenes;
        //    }
        //}

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
