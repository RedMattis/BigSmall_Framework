using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class SwapRaceHediffCompProperties : HediffCompProperties
    {
        public ThingDef swapTarget = null;
        public XenotypeDef xenotype = null;
        public SwapRaceHediffCompProperties()
        {
            compClass = typeof(SwapRaceHediffComp);
        }
    }

    public class SwapRaceHediffComp : HediffComp
    {
        public SwapRaceHediffCompProperties Props => (SwapRaceHediffCompProperties)props;
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            BigAndSmallCache.queuedJobs.Enqueue( new Action(() =>
            {
                if (Props.xenotype != null)
                {
                    parent.pawn.genes.SetXenotype(Props.xenotype);
                }
                RaceMorpher.SwapThingDef(parent.pawn, Props.swapTarget, true, force: true, targetPriority: 100);
            }));
            
        }
    }
    public class InstantEffect : HediffWithComps
    {
        public override bool ShouldRemove => true;
    }


    public static class RaceMorpher
    {
		public static event EventHandler<EventArgs.AnimalSwappedEventArgs> OnAnimalSwapped;
		public static event EventHandler<EventArgs.DefSwappedEventArgs> OnDefSwapped;

		public const int forcePriority = 9001;
        public const int irremovablePriority = 900;
        public const int withoutSourcePriority = 200; // Means it is probably from surgery or something. High priority.
        public const int hediffPriority = 100;
        public const int genePriority = 0;
        public const int racePriority = -100;
        public const int inactiveGenePriority = -200;
        public static Dictionary<Pawn, List<Hediff>> hediffsToReapply = [];
        public static bool runningRaceSwap = false;

        public static Pawn SwapAnimalToSapientVersion(this Pawn aniPawn)
        {
            bool oldPawnDestroyed = false;
            try
            {
                if (aniPawn.def.IsHumanlikeAnimal()) return null;

                var targetDef = HumanlikeAnimals.HumanLikeAnimalFor(aniPawn.def);
                if (targetDef == null) return null;
                // Empty inventory
                if (aniPawn.inventory != null && aniPawn.inventory?.innerContainer != null)
                {
                    if (aniPawn.Spawned)
                    {
                        aniPawn.inventory.DropAllNearPawn(aniPawn.Position);
                    }
                    else
                    {
                        aniPawn.inventory.DestroyAll(DestroyMode.Vanish);
                    }
                }
                bool shouldBeWildman = false;
                var request = new PawnGenerationRequest(PawnKindDefOf.Colonist,
                    canGeneratePawnRelations: false,
                    allowDead: false, allowDowned: false, allowAddictions: false,
                    forbidAnyTitle: true, forceGenerateNewPawn: true,
                    forceBaselinerChance: 2,
                    forceNoBackstory: true);

                var newPawn = PawnGenerator.GeneratePawn(request);
                newPawn.inventory.DestroyAll(DestroyMode.Vanish);
                newPawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                newPawn.apparel.DestroyAll(DestroyMode.Vanish);

                string oldName = aniPawn.Name?.ToStringShort;
                if (oldName == null)
                {
                    newPawn.Name = PawnBioAndNameGenerator.GeneratePawnName(newPawn, forceNoNick: true);
                }
                else
                {
                    aniPawn.Name = new NameSingle(aniPawn.Name.ToStringShort + "_Discard");
                    newPawn.Name = new NameSingle(oldName);
                }
                newPawn.relations.ClearAllRelations(); // Should add a friend relationship to any bonded pawn here later...
                newPawn.story.Adulthood = DefDatabase<BackstoryDef>.GetNamed("Colonist97");
                newPawn.story.Childhood = DefDatabase<BackstoryDef>.GetNamed("TribeChild19");
                if (aniPawn.Faction == null)
                {
                    shouldBeWildman = true;
                    newPawn.ideo?.SetIdeo(Faction.OfPlayerSilentFail?.ideos?.PrimaryIdeo);
                }
                else
                {
                    newPawn.ideo?.SetIdeo(aniPawn.Faction.ideos?.PrimaryIdeo);
                    newPawn.SetFaction(aniPawn.Faction);
                }

                if (ModsConfig.BiotechActive)
                {
                    if (newPawn.genes.Xenotype != XenotypeDefOf.Baseliner)
                    {
                        Log.Message($"[Big and Small] {newPawn} had a xenotype {newPawn.genes.Xenotype.defName} but was supossed to generate as a baseliner." +
                            $"Removing xenotype and genes.");
                        // Somehow they can still end up having a xenotype.
                        for (int idx = newPawn.genes.GenesListForReading.Count - 1; idx >= 0; idx--)
                        {
                            var gene = newPawn.genes.GenesListForReading[idx];
                            newPawn.genes.RemoveGene(gene);
                        }
                        newPawn.genes.SetXenotype(XenotypeDefOf.Baseliner);
                        GeneHelpers.ClearCachedGenes(newPawn);
                    }
                }
                CacheAndRemoveHediffs(aniPawn);
                newPawn.health.hediffSet.hediffs.Clear();
                //foreach (var hediff in pawn.health.hediffSet.hediffs)
                //{
                //    var h = newPawn.health.AddHediff(hediff.def, hediff.Part, null);
                //    h.Severity = hediff.Severity;
                //}


                // Spawn into the same position as the old pawn.
                if (aniPawn.Spawned)
                {
                    GenSpawn.Spawn(newPawn, aniPawn.Position, aniPawn.Map, aniPawn.Rotation, WipeMode.VanishOrMoveAside);
                }

				if (PawnGraphicUtils.TryGetAlternate(aniPawn, out _, out int index))
					newPawn.overrideGraphicIndex = index;

				SwapThingDef(newPawn, targetDef, true, forcePriority, force: true, permitFusion: false, clearHediffsToReapply: false);
                RestoreMatchingHediffs(newPawn, targetDef, aniPawn);

				// Wait until def is swapped to transfer age.
				newPawn.gender = aniPawn.gender == Gender.None ? newPawn.gender : aniPawn.gender;
				newPawn.ageTracker.AgeChronologicalTicks = aniPawn.ageTracker.AgeChronologicalTicks;
				if (aniPawn.ageTracker.AgeBiologicalYears < 3)
				{
					newPawn.ageTracker.AgeBiologicalTicks = 3 * GenDate.TicksPerYear;
				}
				else
				{
					float percentOfLifespan = aniPawn.ageTracker.AgeBiologicalYears / aniPawn.RaceProps.lifeExpectancy;
					newPawn.ageTracker.AgeBiologicalTicks = (long)(newPawn.RaceProps.lifeExpectancy * percentOfLifespan) * GenDate.TicksPerYear;
					//newPawn.ageTracker.AgeBiologicalTicks = aniPawn.ageTracker.AgeBiologicalTicks;
				}

				if (shouldBeWildman)
                {
                    if (newPawn.Faction != null)
                    { 
                        newPawn.SetFaction(null);
                    }
                    newPawn.ChangeKind(PawnKindDefOf.WildMan);
                    newPawn.jobs.StopAll();
                }
                if (aniPawn.RaceProps.IsMechanoid && aniPawn.kindDef?.weaponTags?.Any() == true)
                {
                    try
                    {
                        var weaponTag = aniPawn.kindDef.weaponTags.FirstOrDefault();
                        var weaponFromTag = DefDatabase<ThingDef>.AllDefsListForReading
                            .Where(x => x.IsWeapon && x.weaponTags?.Contains(weaponTag) == true)
                            .OrderByDescending(x => x.BaseMarketValue).FirstOrDefault();
                        var weapon = (ThingWithComps)ThingMaker.MakeThing(weaponFromTag);
                        newPawn.equipment.AddEquipment(weapon);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[Big and Small] Error trying to equip {newPawn} with a weapon from {aniPawn.kindDef}:\n{e.Message}\n{e.StackTrace}");
                    }
                }

				// Remove hair color gene
				GeneDef hairDef = newPawn.genes.GetHairColorGene();
				Gene hairGene = newPawn.genes.Endogenes.FirstOrDefault(x => x.def == hairDef);
				if (hairGene != null)
					newPawn.genes.RemoveGene(hairGene);
                newPawn.story.HairColor = new Color(0, 0, 0, 0);

				UnityEngine.Color? bodyColor = aniPawn.ageTracker.CurKindLifeStage?.bodyGraphicData?.color;
				if (bodyColor != null)
					newPawn.story.HairColor = bodyColor.Value;

				OnAnimalSwapped?.Invoke(null, new EventArgs.AnimalSwappedEventArgs(aniPawn, newPawn));

                aniPawn.Destroy(DestroyMode.Vanish);
                oldPawnDestroyed = true;

				//TEST
				//Log.Message($"DEBUG for {newPawn} {newPawn.def}");
				//Log.Message($"ACTIVE COMPS: {string.Join("\n", newPawn.AllComps.Select(x => x.GetType() + " " + x.ToString()))}");
				//Log.Message($"DEF PROPS: {string.Join("\n", newPawn.def.comps.Select(x => x.GetType().ToString() + " " + x.compClass.ToString()))}");


				return newPawn;
            }
            catch (Exception e)
            {
                Log.Error($"[Big and Small] Error trying to swap {aniPawn} to a sapient version: {e.Message}\n{e.StackTrace}");
                return oldPawnDestroyed ? null : aniPawn;
            }
        }

        public static void SwapThingDef(this Pawn pawn, ThingDef swapTarget, bool state, int targetPriority, bool force=false, object source=null, bool permitFusion=true, bool clearHediffsToReapply=true)
        {
            static bool IsDiscardable(ThingDef def) => def == ThingDefOf.Human || def == ThingDefOf.CreepJoiner;
            if (swapTarget == null)
            {
                Log.Error($"SwapThingDef called on {pawn} with null swapTarget.");
                return;
            }
            if (pawn == null)
            {
                Log.Error($"SwapThingDef called on a null pawn with swapTarget {swapTarget}.");
                return;
            }
            if (runningRaceSwap || pawn?.genes == null || (pawn.def == swapTarget && state)) return;

            var firstMechanical = BodyDefFusionsHelper.MergableBodies.Where(x => x.defaultMechanical).FirstOrDefault();

            if (pawn.def == firstMechanical.thingDef && swapTarget.IsMechanicalDef())
            {
                force = true;
                permitFusion = false;
                targetPriority = forcePriority;
            }

            if (clearHediffsToReapply)
            {
                hediffsToReapply.Clear();
            }
            try
            {
                runningRaceSwap = true;
                
                bool wasDead = pawn.health?.Dead == true;

                if (force)
                {
                    targetPriority = forcePriority;
                }

                var cache = HumanoidPawnScaler.GetCache(pawn, canRegenerate: false);
                if (force && swapTarget.GetModExtension<RaceExtension>()?.RaceHediffs is List<HediffDef> raceHediffs)
                {
                    // Remove everything on the raceTrackerHistory with the same substitution groups
                    foreach(var raceHediff in raceHediffs)
                    {
                        var subs = BodyDefFusionsHelper.GetSubstitutableTrackers(raceHediff).SelectMany(x => x).ToList();
                        cache.raceTrackerHistory.RemoveWhere(subs.Contains);
                    }
                }

                var thingDefHediffs = ModExtHelper.GetAllPawnExtensions(pawn, parentBlacklist: [typeof(RaceTracker)]).Where(x => x.thingDefSwap != null).Select(x => x.thingDefSwap).ToList();
                var thingDefActiveGenes = ModExtHelper.GetAllPawnExtensions(pawn).Where(x=>x.thingDefSwap != null).Select(x => x.thingDefSwap).ToList();

                var thingDefGenes = ModExtHelper.GetAllPawnExtensions(pawn, includeInactiveGenes:true).Where(x => x.thingDefSwap != null).Select(x => x.thingDefSwap).ToList();


                // Check if the ThingDef we CURRENTLY are is among the genesWithThingDefSwaps
                //var geneWithThingDef = Enumerable.Where<PGene>(genesWithThingDefSwaps, (Func<PGene, bool>)(x => x.GeneExt().thingDefSwap.defName == pawn.def.defName));
                bool didSwap = false;

                List<(int priority, ThingDef thing)> thingsToTryFusionWith = [];


                List<ThingDef> unwrappedPawnThingdef = pawn.def.GetRaceExtensions()?.Where(x => x.isFusionOf != null)?.SelectMany(x => x.isFusionOf).ToList();
                unwrappedPawnThingdef = unwrappedPawnThingdef.NullOrEmpty() ? [pawn.def] : unwrappedPawnThingdef;

                bool removingCurrent = state == false && unwrappedPawnThingdef.Contains(swapTarget);
                var finalTarget = state ? swapTarget : ThingDefOf.Human;

                if (!removingCurrent)
                {
                    unwrappedPawnThingdef.Remove(swapTarget);
                    foreach ((var tDef, List<HediffDef> rTracker) in unwrappedPawnThingdef
                        .Select(x => (x, x.ExtensionsOnDef<RaceExtension, ThingDef>()?
                            .SelectMany(x => x.RaceHediffs).Where(x => x != null).ToList())))
                    {
                        if (IsDiscardable(tDef))
                            continue;

                        var props = rTracker.SelectMany(x=>x.comps.Select(x => x as CompProperties_Race).Where(x => x != null)).ToList();
                        int priority = withoutSourcePriority;
                        
                        if (thingDefHediffs.Any(x => x == pawn.def))
                            priority = hediffPriority;
                        else if (thingDefActiveGenes.Any(x => x == pawn.def))
                            priority = genePriority;
                        else if (!IsDiscardable(tDef) && props.Any(x => x.canSwapAwayFrom == false))
                            priority = irremovablePriority;

                        thingsToTryFusionWith.Add((priority, tDef));
                    }
                }
                if (state == true)
                {
                    thingsToTryFusionWith.Add((targetPriority, swapTarget));
                }
                

                // We're removing the current def. Find another base def.
                if (state == false && removingCurrent) 
                {
                    bool foundNewDefault = false;
                    if (thingDefHediffs.Count > 0)
                    {
                        thingsToTryFusionWith.AddRange(thingDefHediffs.Select(x => (hediffPriority, x)));
                        foundNewDefault = true;
                    }
                    if (thingDefActiveGenes.Count > 0)
                    {
                        thingsToTryFusionWith.AddRange(thingDefActiveGenes.Select(x => (genePriority, x)));
                        foundNewDefault = true;
                    }
                    if (thingDefGenes.Count > 0)
                    {
                        thingsToTryFusionWith.AddRange(thingDefActiveGenes.Select(x => (inactiveGenePriority, x)));
                        foundNewDefault = true;
                    }
                    if (!foundNewDefault)
                    {
                        var originalThing = ThingDefOf.Human;

                        if (cache.isMechanical)
                        {
                            // Find first MergableBody with defaultMechanical.
                            if (firstMechanical != null && firstMechanical?.thingDef != swapTarget)
                            {
                                originalThing = firstMechanical.thingDef;
                            }
                        }
                        if (cache.originalThing != pawn.def && cache.originalThing != ThingDefOf.Human)
                        {
                            originalThing = cache.originalThing;
                        }
                        // If we ended up selected the thing we're removing as the original, select Human instead.
                        if (originalThing == null || originalThing == swapTarget)
                        {
                            originalThing = ThingDefOf.Human;
                            cache.originalThing = ThingDefOf.Human;
                        }
                        
                        if (originalThing != ThingDefOf.Human)
                        {
                            thingsToTryFusionWith.Add((racePriority, originalThing));
                        }
                    }
                }
                if (permitFusion)
                {
                    // Priority 1: Hediffs.
                    thingsToTryFusionWith.AddRange(thingDefHediffs.Select(x=> (hediffPriority, x)));
                    // Priority 2: Active genes.
                    thingsToTryFusionWith.AddRange(thingDefActiveGenes
                        .Where(x => x != swapTarget).Select(x => (genePriority, x)));
                    // Priority 4: Inactive genes.
                    thingsToTryFusionWith.AddRange(thingDefGenes
                        .Where(x => x != swapTarget).Select(x => (inactiveGenePriority, x)));


                    for (int idx = thingsToTryFusionWith.Count - 1; idx >= 0; idx--)
                    {
                        // Remove this if there are and other copies.
                        if (thingsToTryFusionWith.Count(x => x.thing == thingsToTryFusionWith[idx].thing) > 1)
                        {
                            thingsToTryFusionWith.RemoveAt(idx);
                        }
                    }

                    thingsToTryFusionWith = [.. thingsToTryFusionWith.OrderByDescending(x=>x.priority)];

                    var allPossibleBodies = thingsToTryFusionWith.Select(x => x.thing.race.body).ToList();
                    

                    if (state == false)
                    {
                        // Remove the target from all lists.
                        thingsToTryFusionWith.RemoveAll(x => x.thing == swapTarget);
                    }

                    while (allPossibleBodies.Count > 0)
                    {
                        
                        bool mechanical = cache.isMechanical;
                        var fusedBody = FusedBody.TryGetBody(mechanical, [.. allPossibleBodies]);
                        if (fusedBody != null)
                        {
                            finalTarget = fusedBody.Thing;
                            break;
                        }
                        else if (FusedBody.TryGetNonFused([.. allPossibleBodies]) is BodyDef nonFusedBody &&
                            thingsToTryFusionWith.FirstOrDefault(x => x.thing.race.body == nonFusedBody) is (int, ThingDef) nonFuse)
                        {
                            finalTarget = nonFuse.thing;
                            break;
                        }
                        allPossibleBodies.RemoveAt(allPossibleBodies.Count - 1);
                        if (allPossibleBodies.Count == 1)
                        {
                            finalTarget = thingsToTryFusionWith[0].thing;
                        }
                    }
                }

                // Don't swap to a thingDef that is already active.
                if (pawn.def.defName != finalTarget.defName)
                {
                    //Log.Message($"[DEBUG] Running defswap on {pawn} to {finalTarget} (original target: {swapTarget.defName}) with state {state} and force {force}.");

                    // Change the pawn's thingDef to the one specified in the GeneExtension.
                    didSwap = ExecuteDefSwap(cache, finalTarget);
                }
                if (didSwap)
                {
                    pawn.RecacheStatsForThing();
                    pawn.VerbTracker.InitVerbsFromZero();
                    if (pawn.health.Dead && !wasDead)
                    {
                        Log.WarningOnce($"[DEBUG] {pawn} was dead after def swap to {finalTarget}. Attempting to resurrect.", key: 921378231);
                        ResurrectionUtility.TryResurrect(pawn);
                        pawn.VerbTracker.InitVerbsFromZero();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Message($"Error trying to in SwapThingDef of {pawn} to {swapTarget} (if this happend during world gen it is likely harmless):\n{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                //Log.Warning($"[DEBUG] Running defswap without Catch.");
                runningRaceSwap = false;
                HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);

                // Call all the pawn's statdefs and request that they update.
                foreach (var stat in pawn.def.statBases)
                {
                    stat.stat.Worker.ClearCacheForThing(pawn);
                }
            }
        }

        private static bool ExecuteDefSwap(BSCache cache, ThingDef swapTarget)
        {
            Pawn pawn = cache.pawn;
            if (pawn?.def == null) return false;
            if (pawn.def == swapTarget) return false;
            bool wasRemovedFromLister = false;
            var map = pawn.Map;

            if (!hediffsToReapply.ContainsKey(pawn)) hediffsToReapply[pawn] = [];
            try
            {
                if (map != null)
                {
                    RegionListersUpdater.DeregisterInRegions(pawn, map);
                }
            }
            catch (Exception e)
            {
                Log.Message($"Error when deregistering in regions: {e.Message}");
            }
            try
            {
                if (map != null)
                {
                    if (map.listerThings.Contains(pawn))
                    {
                        map.listerThings.Remove(pawn);
                        wasRemovedFromLister = true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Message($"Error when removing from listers: {e.Message}");
            }
            int ageBiologicalYears = pawn.ageTracker.AgeBiologicalYears;

           
            var removedTrackers = RaceExtension.RemoveOldRaceTrackers(pawn);

            CacheAndRemoveHediffs(pawn);

            var oldDefType = pawn.def.GetType();
            pawn.def = swapTarget;
            pawn.RecacheStatsForThing();
            if (!pawn.def.GetType().Name.Contains("ThingDef_AlienRace"))
            {
                for (int idx = pawn.AllComps.Count - 1; idx >= 0; idx--)
                {
                    ThingComp comp = pawn.AllComps[idx];
                    if (comp.GetType().Name.Contains("AlienComp"))
                    {
                        //Log.Message($"[Big and Small] Removed AlienComp from {pawn.def.defName} due to (no longer?) being a HAR race.");
                        Log.WarningOnce($"[Big and Small] {pawn.def.defName} Swapped to an AlienRace with an AlienComp. This is somewhat untested.", key: 929972331);
                        //pawn.AllComps.Remove(comp);
                        //comp.parent = null;
                    }
                }
            }
            else if (oldDefType != pawn.def.GetType() && pawn.def.GetType().Name.Contains("ThingDef_AlienRace"))
            {
                // Might need to be reimplemented later if HAR breaks.
            }

            int lifeStageIndex = -1;

            List<LifeStageAge> lifeStageAges = pawn.RaceProps.lifeStageAges;
            for (int lifeIdx = lifeStageAges.Count - 1; lifeIdx >= 0; lifeIdx--)
            {
                if (lifeStageAges[lifeIdx].minAge <= ageBiologicalYears + 1E-06f)
                {
                    lifeStageIndex = lifeIdx;
                    break;
                }
            }
            var fieldRef = AccessTools.FieldRefAccess<Pawn_AgeTracker, int>("cachedLifeStageIndex");
            fieldRef(pawn.ageTracker) = lifeStageIndex;

            // In case any components are now missing.
            // Shouldn't happen unless moving from Humanlike to something else, but... still.
            //PawnComponentsUtility.CreateInitialComponents(pawn);
            try
            {
                if (map != null)
                {
                    if (wasRemovedFromLister || pawn.Spawned)
                    {
                        if (!map.listerThings.Contains(pawn))
                        {
                            map.listerThings.Add(pawn);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Message($"Error when restoring to listers: {e.Message}");
            }
            try
            {
                if (map != null)
                {
                    RegionListersUpdater.RegisterInRegions(pawn, pawn.Map);
                }
            }
            catch (Exception e)
            {
                Log.Message($"Error when registering in regions: {e.Message}");
            }

            RestoreMatchingHediffs(pawn, pawn.def);

            if (pawn.def.GetRaceExtensions()?.FirstOrDefault() is RaceExtension raceExtension)
            {
                raceExtension.ApplyTrackerIfMissing(pawn, cache);
            }
            if (pawn?.needs != null)
            {
                pawn.needs.AddOrRemoveNeedsAsAppropriate();
            }
            try
            {
                AddMissingComps(pawn);
            }
            catch (Exception e)
            {
                Log.Error($"[Big and Small] Error trying to add missing comps to {pawn}: {e.Message}\n{e.StackTrace}");
            }

            return true;
        }

        public static void CacheAndRemoveHediffs(Pawn pawn)
        {
            var allHediffs = pawn.health.hediffSet.hediffs.ToList();
            
            hediffsToReapply[pawn] = [.. allHediffs];

            // Remove all hediffs
            foreach (var hediff in allHediffs)
            {
                //pawn.health.RemoveHediff(hediff);
                pawn.health.hediffSet.hediffs.Remove(hediff);
            }
        }

        public static void RestoreMatchingHediffs(Pawn pawn, ThingDef targetThingDef, Pawn source = null)
        {
            List<BodyPartRecord> currentParts = targetThingDef.race.body.AllParts.Select(x => x).ToList();
            source ??= pawn;
            // Go over the savedHediffs and check if any of them can attach to the current bodyparts.
            if (hediffsToReapply[source].Count > 0)
            {
                for (int idx = hediffsToReapply[source].Count - 1; idx >= 0; idx--)
                {
                    Hediff hediff = hediffsToReapply[source][idx];
                    float severity = hediff.Severity;

                    bool canAttach = hediff.Part == null || currentParts.Any(x => x.def.defName == hediff.Part.def.defName || x.customLabel == hediff.Part.customLabel);

                    if (canAttach)
                    {
                        try
                        {
                            // Check if Hediff is a Hediff_ChemicalDependency
                            if (hediff is Hediff_ChemicalDependency chemicalDependency)
                            {
                                continue;
                            }

                            else if (hediff.Part == null)
                            {
                                hediff.pawn = pawn;
                                //hediff.loadID = Find.UniqueIDsManager.GetNextHediffID(); // Not sure if giving a new ID is better or worse.
                                pawn.health.hediffSet.hediffs.Add(hediff);
                            }
                            else
                            {
                                BodyPartRecord matchingCustomLabel = currentParts.FirstOrDefault(x => x.def.defName == hediff.Part.def.defName && x.customLabel == hediff.Part.customLabel);
                                BodyPartRecord matchingLabel = currentParts.FirstOrDefault(x => x.def.defName == hediff.Part.def.defName && x.Label == hediff.Part.Label);
                                BodyPartRecord matchingDef = currentParts.FirstOrDefault(x => x.def.defName == hediff.Part.def.defName);

                                // Prefer customLabel, then Label, then just the def.
                                BodyPartRecord partMatchingHediff = matchingCustomLabel ?? matchingLabel ?? matchingDef;

                                if (partMatchingHediff != null)
                                {
                                    try
                                    {
                                        pawn.health.hediffSet.hediffs.Add(hediff);
                                        hediff.Part = partMatchingHediff;
                                        hediff.pawn = pawn; // Just to be sure.
                                        //hediff.loadID = Find.UniqueIDsManager.GetNextHediffID();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warning($"Failed to add/transfer {hediff.def.defName} to {pawn.Name} on {partMatchingHediff.def.defName}.\n{ex.Message}");
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Usually just means it failed to check if the pawn should die due to it or something.
                            // We probably don't care. That stuff happens after it has been applied.
                        }
                        finally
                        {
                            // remove hediff from savedHediffs
                            hediffsToReapply[source].RemoveAt(idx);
                        }
                    }
                }
                pawn.health.hediffSet.DirtyCache();
                for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
                {
                    if (pawn.health.hediffSet.hediffs.Count <= i) continue;  // If the count has decreased, skip and move on.
                    pawn.health.Notify_HediffChanged(pawn.health.hediffSet.hediffs[i]);
                }


                // Find all active genes of type Gene_ChemicalDependency
                foreach (var chemGene in GeneHelpers.GetAllActiveEndoGenes(pawn).Where(x => x is Gene_ChemicalDependency).Select(x => (Gene_ChemicalDependency)x).ToList())
                {
                    RestoreDependencies(pawn, chemGene, xenoGene: false);
                }
                foreach (var chemGene in GeneHelpers.GetAllActiveXenoGenes(pawn).Where(x => x is Gene_ChemicalDependency).Select(x => (Gene_ChemicalDependency)x).ToList())
                {
                    RestoreDependencies(pawn, chemGene, xenoGene: true);
                }
            }
        }

        private static void AddMissingComps(Pawn pawn)
        {
            var def = pawn.def;
            if (!def.comps.Any())
            {
                return;
            }

            var existingComps = pawn.AllComps.ToList();
            List<CompProperties> compPropsToAdd = [..def.comps];
            for (int idx = existingComps.Count - 1; idx >= 0; idx--)
            {
                var comp = existingComps[idx];
                if (comp == null)
                {
                    Log.Warning($"Found a null comp on {pawn} ({pawn?.def?.defName})");
                    continue;
                }
                if (comp.props == null)
                {
                    continue;
                }
                var firstMatch = compPropsToAdd.FirstOrDefault(x => x.compClass != null
                    && x != null
                    && comp.props.GetType() == x.GetType()
                    && x.compClass == comp.GetType());
                if (firstMatch != null)
                {
                    compPropsToAdd.Remove(firstMatch);
                    //Log.Message($"Found existing comp {comp} on {pawn.Name} ({pawn.def.defName}), removing from list of comps to add.");
                }
                else
                {
                    pawn.AllComps.Remove(comp);
                    //Log.Message($"Removed existing comp {comp} from {pawn.Name} ({pawn.def.defName}) as it was not found in the def's comp list. {string.Join(", ", def.comps.Select(x => x.ToString() + " " + x.compClass.ToString()))}.");
                }
            }

            for (int i = 0; i < compPropsToAdd.Count; i++)
            {
                var compProp = compPropsToAdd[i];
                ThingComp thingComp = null;
                try
                {
                    thingComp = (ThingComp)Activator.CreateInstance(compProp.compClass);
                    thingComp.parent = pawn;
                    pawn.AllComps.Add(thingComp);
                    thingComp.Initialize(compProp);
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not instantiate or initialize a ThingComp: {ex}");
                    if (thingComp != null)
                    {
                        pawn.AllComps.Remove(thingComp);
                    }
                }
            }
        }
        private static void RestoreDependencies(Pawn pawn, Gene_ChemicalDependency chemGene, bool xenoGene)
        {
            int lastIngestedTick = chemGene.lastIngestedTick;
            var def = chemGene.def;

            // Remove the gene
            pawn.genes.RemoveGene(chemGene);

            if (def != null)
            {
                // Add the gene back
                pawn.genes.AddGene(def, xenoGene);
            }

            chemGene.lastIngestedTick = lastIngestedTick;
        }
    }
}
