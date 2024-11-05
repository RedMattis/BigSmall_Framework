using BetterPrerequisites;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace BigAndSmall
{
    public class SwapRaceHediffCompProperties : HediffCompProperties
    {
        public ThingDef swapTarget = null;
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
                RaceMorpher.SwapThingDef(parent.pawn, Props.swapTarget, true, force: true);
            }));
            
        }
    }
    public class InstantEffect : HediffWithComps
    {
        public override bool ShouldRemove => true;
    }


    public static class RaceMorpher
    {
        public static Dictionary<Pawn, List<Hediff>> hediffsToReapply = [];
        public static bool runningRaceSwap = false;
        public static void SwapThingDef(this Pawn pawn, ThingDef swapTarget, bool state, bool force=false, object source=null)
        {
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
                var currentProps = pawn.GetRaceCompProps();
                bool wasDead = pawn.health?.Dead == true;

                var genesWithThingDefSwaps = pawn.genes.GenesListForReading
                    .Where(x => x != source && x is PGene pg && pg.GetPawnExt() != null && (x as PGene).GetPawnExt().thingDefSwap != null)
                    .Select(x => (PGene)x).ToList();

                // Check if the ThingDef we CURRENTLY are is among the genesWithThingDefSwaps
                //var geneWithThingDef = Enumerable.Where<PGene>(genesWithThingDefSwaps, (Func<PGene, bool>)(x => x.GeneExt().thingDefSwap.defName == pawn.def.defName));
                bool didSwap = false;
                
                var activeGenesWithSwap = genesWithThingDefSwaps.Where(x => !x.Overridden).ToList();

                // Check if the pawn is a human, or came from a gene.
                // This is mostly to prevent accidentally swapping from say, a HAR-based robot to a human.
                bool souceDefIsValid = pawn.def == ThingDefOf.Human || force || currentProps.canSwapAwayFrom;
                if (!souceDefIsValid)
                {
                    souceDefIsValid = genesWithThingDefSwaps.Any(x => x.GetPawnExt().thingDefSwap == pawn.def);
                }

                if (souceDefIsValid && state)
                {
                    // Don't swap to a thingDef that is already active.
                    if (pawn.def.defName != swapTarget.defName)
                    {
                        // Change the pawn's thingDef to the one specified in the GeneExtension.
                        didSwap = ExecuteDefSwap(pawn, swapTarget);
                    }
                }

                // Check if we're turning off this ThingDef and would want to swap to another.
                else if (!state && pawn.def.defName == swapTarget.defName)
                {
                    ThingDef target = ThingDefOf.Human;
                    if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn, canRegenerate: false) is BSCache cache && cache.originalThing != null &&
                        cache.originalThing != pawn.def)
                    {
                        target = cache.originalThing;
                    }
                    if (activeGenesWithSwap.Count > 0) { target = activeGenesWithSwap.Where(x=>x != source).RandomElement().GetPawnExt().thingDefSwap; }

                    didSwap = ExecuteDefSwap(pawn, target);
                }
                //try
                //{
                    
                //    //if (pawn.Spawned)
                //    //{
                //    //    pawn.DeSpawn();
                //    //    GenPlace.TryPlaceThing(pawn, pos, map, ThingPlaceMode.Direct);
                //    //}
                    
                //}
                //catch { }

                if (didSwap)
                {
                    if (pawn.health.Dead && !wasDead)
                    {
                        ResurrectionUtility.TryResurrect(pawn);
                    }

                    pawn.VerbTracker.InitVerbsFromZero();
                    if (pawn.def.GetModExtension<RaceExtension>() is RaceExtension raceExtension)
                    {
                        raceExtension.ApplyTrackerIfMissing(pawn);
                    }
                    
                }
            }
            catch (Exception e)
            {
                Log.Message($"Error trying to in SwapThingDef of {pawn} to {swapTarget} (if this happend during world gen it is likely harmless):\n{e.Message}");
            }
            finally
            {
                //Log.Message($"[DEBUG] Running defswap without Catch.");
                runningRaceSwap = false;
                HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);

                // Call all the pawn's statdefs and request that they update.
                foreach (var stat in pawn.def.statBases)
                {
                    stat.stat.Worker.ClearCacheForThing(pawn);
                }
            }
            
        }

        private static bool ExecuteDefSwap(Pawn pawn, ThingDef swapTarget)
        {
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

            RaceExtension.RemoveOldRaceTrackers(pawn);
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
            //pawn.Drawer.renderer.SetAllGraphicsDirty();
            return true;
        }

        public static void CacheAndRemoveHediffs(Pawn pawn)
        {
            var allHediffs = pawn.health.hediffSet.hediffs.ToList();
            hediffsToReapply[pawn] = allHediffs.ToList();

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
