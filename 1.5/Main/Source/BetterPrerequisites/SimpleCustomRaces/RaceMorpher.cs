using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public const int forcePriority = 9001;
        public const int irremovablePriority = 900;
        public const int withoutSourcePriority = 200; // Means it is probably from surgery or something. High priority.
        public const int hediffPriority = 100;
        public const int genePriority = 0;
        public const int racePriority = -100;
        public const int inactiveGenePriority = -200;
        public static Dictionary<Pawn, List<Hediff>> hediffsToReapply = [];
        public static bool runningRaceSwap = false;
        public static void SwapThingDef(this Pawn pawn, ThingDef swapTarget, bool state, int targetPriority, bool force=false, object source=null, bool permitFusion=true)
        {
            static bool IsDiscardable(ThingDef def) => def == ThingDefOf.Human;
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

            hediffsToReapply.Clear();
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
                            var firstMechanical = BodyDefFusionsHelper.MergableBodies.Where(x => x.defaultMechanical).FirstOrDefault();
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
                    if (pawn.health.Dead && !wasDead)
                    {
                        ResurrectionUtility.TryResurrect(pawn);
                    }

                    pawn.VerbTracker.InitVerbsFromZero();
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
            //var pos = pawn.Position;
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
            pawn.def = swapTarget;
            //pawn.ageTracker = new Pawn_AgeTracker(pawn);

            //pawn.ageTracker.RecalculateLifeStageIndex
            // Access cachedLifeStageIndex

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

            //pawn.Drawer.renderer.SetAllGraphicsDirty();
            return true;
        }

        public static void CacheAndRemoveHediffs(Pawn pawn)
        {
            var allHediffs = pawn.health.hediffSet.hediffs.ToList();
            hediffsToReapply[pawn] = [.. allHediffs];

            // Remove all hediffs
            foreach (var hediff in allHediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public static void RestoreMatchingHediffs(Pawn pawn, ThingDef targetThingDef)
        {
            List<BodyPartRecord> currentParts = targetThingDef.race.body.AllParts.Select(x => x).ToList();

            // Go over the savedHediffs and check if any of them can attach to the current bodyparts.
            if (hediffsToReapply[pawn].Count > 0)
            {
                for (int idx = hediffsToReapply[pawn].Count - 1; idx >= 0; idx--)
                {
                    Hediff hediff = hediffsToReapply[pawn][idx];

                    bool canAttach = hediff.Part == null || currentParts.Any(x => x.def.defName == hediff.Part.def.defName && x.customLabel == hediff.Part.customLabel);

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
                                pawn.health.AddHediff(hediff.def);
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
                                        var resultHediff = pawn.health.AddHediff(hediff.def, part: partMatchingHediff);
                                        if (resultHediff is Hediff_Injury resultWound && hediff is Hediff_Injury orgInjury)
                                        {
                                            if (orgInjury.IsPermanent() && resultWound.TryGetComp<HediffComp_GetsPermanent>() is HediffComp_GetsPermanent pSetter)
                                            {
                                                pSetter.IsPermanent = true;
                                            }
                                        }
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
                            hediffsToReapply[pawn].RemoveAt(idx);
                        }
                    }
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
