using BigAndSmall;
using HarmonyLib;
using MonoMod.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BetterPrerequisites
{
    public class PGene : Gene
    {
        /// <summary>
        /// Avoid recursion by supressing this method when it woudld get called from itself.
        /// </summary>
        public static bool supressPostfix = false;
        public static bool supressPostfix2 = false;

        private int refreshGeneEffectsCountdown = 5;

        public bool? previouslyActive = null;
        public float lastUpdateTicks = 0f;
        public float lastUpdate = 0f;
        //public bool triggerNalFaceDisable = false;

        private bool initialized = false;
        
        public bool hasExtension = false;

        private bool lookedForGeneExt = false;
        private GeneExtension geneExt = null;

        private GeneExtension GeneExt
        {
            get
            {
                if (geneExt == null && !lookedForGeneExt)
                {
                    geneExt = def.GetModExtension<GeneExtension>();
                    lookedForGeneExt = true;
                }
                return geneExt;
            }
            set => geneExt = value;
        }

        public override bool Active => TryGetGeneActiveCache(base.Active);

        public bool ForceRun { get; set; } = false;
        public CacheTimer Timer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool postPostAddDone = false;

        public override void PostAdd()
        {
            SetupVars();
            base.PostAdd();

            bool needsReevaluation = false;

            if (GeneExt != null)
            {
                if (base.Active && Active)
                {
                    SwapThingDef(true);
                }

                // Check if this is a xenogene.
                bool xenoGene = pawn.genes.Xenogenes.Any(x => x == this);

                foreach(var geneDef in GeneExt.hiddenGenes)
                {
                    // Add hidden gene
                    pawn.genes.AddGene(geneDef, xenoGene);
                }
                // Disable facial animations from Nal's Facial Animation mod
                //try
                //{
                //    if (ModsConfig.IsActive("nals.facialanimation") && geneExt.facialDisabler != null)
                //    {
                //        triggerNalFaceDisable = true;
                //    }
                //}
                //catch (Exception e)
                //{
                //    Log.Message($"Error in PostAdd: {e.Message}");
                //}

                if (GeneExt.conditionals != null)
                {
                    needsReevaluation = true;
                }
                
            }

            if (def.GetModExtension<GenePrerequisites>() is GenePrerequisites gPrerequisites)
            {
                needsReevaluation = true;
            }
            if (def.GetModExtension<GeneSuppressor_Gene>() is GeneSuppressor_Gene gSuppressor)
            {
                needsReevaluation = true;
            }

            if (needsReevaluation)
            {
                BigAndSmallCache.pGenesThatReevaluate.Add(this);
            }
        }

        public void PostPostAdd()
        {
            if (!postPostAddDone)
            {
                if (GeneExt != null)
                {
                    SwapThingDef(true);
                }
            }
            postPostAddDone = true;
        }

        public override void PostRemove()
        {
            BigAndSmallCache.pGenesThatReevaluate.Remove(this);
            base.PostRemove();
            if (GeneExt != null)
            {
                SwapThingDef(false, force:true);

                // If this is the last active gene of its type...
                bool lastActiveOfDef = !pawn.genes.GenesListForReading.Any(x => x.def == def && x != this);
                if (lastActiveOfDef)
                {
                    foreach (var geneDef in GeneExt.hiddenGenes)
                    {
                        // Remove all genes matching def
                        var matchingGenes = pawn.genes.GenesListForReading.Where(x => x.def == geneDef).ToList(); ;
                        foreach (var gene in matchingGenes)
                        {
                            pawn.genes.RemoveGene(gene);
                        }
                    }
                }
                if (ModsConfig.IsActive("nals.facialanimation") && GeneExt.facialDisabler != null)
                {
                    // Check if any other genes have facialDisabler

                    //bool otherFacialDisabler = pawn.genes.GenesListForReading.Any(x => x.def.HasModExtension<GeneExtension>() && x.def.GetModExtension<GeneExtension>().facialDisabler != null);
                    //if (!otherFacialDisabler)
                    //{
                    //    try
                    //    {
                    //        NalFaceExt.DisableFacialAnimations(pawn, geneExt.facialDisabler, revert:true);
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        Log.Message($"Error in PostRemove: {e.Message}");
                    //    }
                    //}
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            int currentTick = Find.TickManager.TicksGame;
            // Every 5000 ticks
            if (currentTick % 5000 == 0)
            {
                // Clear saved Hediffs. It is only to be used for the instant when a swap occurs.
                hediffsToReapply.Clear();

                // Try triggering transform genes if it exists.
                GeneExt?.transformGene?.TryTransform(pawn, this);
            }
            if (currentTick % 5000 == 5)
            {
                PostPostAdd();
            }

            if (currentTick % 100 == 0 && pawn.needs != null && Active)
            {
                if (GeneExt != null && GeneExt.lockedNeeds != null)
                {
                    foreach (var lockedNeed in GeneExt.lockedNeeds.Where(x=>x.need != null))
                    {
                        float value = lockedNeed.value;
                        NeedDef needDef = lockedNeed.need;

                        var need = pawn.needs.TryGetNeed(needDef);

                        if (need != null)
                        {
                            need.CurLevel = need.MaxLevel * value;
                        }
                    }
                }
            }
            //if (Find.TickManager.TicksGame % 100 == 0 && triggerNalFaceDisable)
            //{
            //    try
            //    {
            //        if (ModsConfig.IsActive("nals.facialanimation") && geneExt != null && geneExt.facialDisabler != null)
            //        {
            //            triggerNalFaceDisable = false;
            //            NalFaceExt.DisableFacialAnimations(pawn, geneExt.facialDisabler, revert: false);
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        Log.Message($"Error in Tick: {e.Message}");
            //    }
            //}
        }

        public static Dictionary<Pawn, List<Hediff>> hediffsToReapply = new Dictionary<Pawn, List<Hediff>>();
        public void SwapThingDef(bool state, bool force=false)
        {
            try
            {
                if (GeneExt != null && GeneExt.thingDefSwap != null)
                {
                    bool wasDead = pawn.health.Dead;

                    var genesWithThingDefSwaps = pawn.genes.GenesListForReading
                        .Where(x => x != this && x is PGene && (x as PGene).GeneExt != null && (x as PGene).GeneExt.thingDefSwap != null)
                        .Select(x=>(PGene)x).ToList();

                    //var pos = pawn.Position;
                    var map = pawn.Map;

                    // Check if the ThingDef we CURRENTLY are is among the genesWithThingDefSwaps
                    var geneWithThingDef = genesWithThingDefSwaps.Where(x => x.GeneExt.thingDefSwap.defName == pawn.def.defName);
                    bool didSwap = false;
                    bool wasRemovedFromLister = false;
                    bool forceSwap = GeneExt.forceThingDefSwap;
                    // if all geneWithThingDef are inactive, we can swap.
                    if ( geneWithThingDef.All(x => x.Overridden))
                    {
                        force = true;
                        forceSwap = true;
                    }

                    if (!force)
                    {
                        if (state && GeneExt.thingDefSwap.defName == pawn.def.defName)
                        {
                            return;
                        }
                    }

                    if (!hediffsToReapply.ContainsKey(pawn)) hediffsToReapply[pawn] = new List<Hediff>();
                    try
                    {
                        RegionListersUpdater.DeregisterInRegions(pawn, pawn.Map);
                        if (map.listerThings.Contains(pawn))
                        {
                            map.listerThings.Remove(pawn);
                            wasRemovedFromLister = true;
                        }
                        
                    }
                    catch { }
                    // Check if the pawn is a human or we're forcing the swap.
                    if ((pawn.def == ThingDefOf.Human || forceSwap) && state)
                    {
                        // Don't swap to a thingDef that is already active.
                        if (pawn.def.defName != GeneExt.thingDefSwap.defName)
                        {
                            // Change the pawn's thingDef to the one specified in the GeneExtension.
                            CacheAndRemoveHediffs();
                            pawn.def = GeneExt.thingDefSwap;
                            RestoreMatchingHediffs(pawn.def);
                            didSwap = true;
                        }
                    }
                    // Check if we're turning off this ThingDef and would want to swap to another.

                    else if (!state && pawn.def.defName == GeneExt.thingDefSwap.defName)
                    {
                        ThingDef target = ThingDefOf.Human;
                        if (genesWithThingDefSwaps.Count > 0) { target = genesWithThingDefSwaps.RandomElement().GeneExt.thingDefSwap; }

                        CacheAndRemoveHediffs();
                        // Change the pawn's thingDef to a baseliner.
                        pawn.def = ThingDefOf.Human;
                        RestoreMatchingHediffs(pawn.def);
                        didSwap = true;
                    }
                    try
                    {
                        if (wasRemovedFromLister || pawn.Spawned)
                        {
                            if (!map.listerThings.Contains(pawn))
                            {
                                map.listerThings.Add(pawn);
                            }
                        }
                        //if (pawn.Spawned)
                        //{
                        //    pawn.DeSpawn();
                        //    GenPlace.TryPlaceThing(pawn, pos, map, ThingPlaceMode.Direct);
                        //}
                        RegionListersUpdater.RegisterInRegions(pawn, pawn.Map);
                    }
                    catch { }

                    if (pawn.health.Dead && !wasDead && didSwap)
                    {
                        ResurrectionUtility.TryResurrect(pawn);
                    }

                    pawn.VerbTracker.InitVerbsFromZero();

                }
            }
            catch (Exception e)
            {
                Log.Message($"Error in SwapThingDef (if this happend during world gen it is nothing to worry about): {e.Message}");
            }
        }

        public void CacheAndRemoveHediffs()
        {
            var allHediffs = pawn.health.hediffSet.hediffs.ToList();
            hediffsToReapply[pawn] = allHediffs.ToList();

            // Remove all hediffs
            foreach (var hediff in allHediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public void RestoreMatchingHediffs(ThingDef targetThingDef)
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
                                    pawn.health.AddHediff(hediff.def, part: partMatchingHediff);
                                }
                                catch (Exception ex)
                                {
                                    Log.Warning($"Failed to add transfer {hediff.def.defName} to {pawn.Name} on {partMatchingHediff.def.defName}.\n{ex.Message}");
                                }
                            }
                        }
                        
                        // remove hediff from savedHediffs
                        hediffsToReapply[pawn].RemoveAt(idx);
                    }
                }
                // Find all active genes of type Gene_ChemicalDependency
                foreach(var chemGene in GeneHelpers.GetAllActiveEndoGenes(pawn).Where(x=>x is Gene_ChemicalDependency).Select(x=> (Gene_ChemicalDependency)x).ToList())
                {
                    RestoreDependencies(chemGene, xenoGene:false);
                }
                foreach (var chemGene in GeneHelpers.GetAllActiveXenoGenes(pawn).Where(x => x is Gene_ChemicalDependency).Select(x => (Gene_ChemicalDependency)x).ToList())
                {
                    RestoreDependencies(chemGene, xenoGene: true);
                }
            }
        }

        private void RestoreDependencies(Gene_ChemicalDependency chemGene, bool xenoGene)
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
        
        readonly float updateFrequence = 125; //1.5f;
        readonly float updateFrequenceRealTime = 2.0f; //1.5f;
        public static object locker = new object();

        public bool TryGetGeneActiveCache(bool result)
        {
            // Skip dead pawns and non-spawned pawns.
            if (pawn != null && !PawnGenerator.IsBeingGenerated(pawn) && !pawn.Dead && pawn.Spawned)
            {
                if (result || ForceRun)
                {
                    bool useCache = initialized;
                    SetupVars();
                    if (!useCache || ForceRun)
                    {
                        ForceRun = false;
                        RegenerateState();
                    }
                    bool newResult = previouslyActive != false;
                    result = newResult && result;
                }
            }

            return result;
        }

        /// <summary>
        /// Mostly because PostAdd doesn't run on save load and stuff like that. But I don't think trying to fetch the ModExtension every time is a good idea.
        /// </summary>
        public void SetupVars()
        {
            if (!initialized)
            {
                initialized = true;
                if (def.HasModExtension<GeneSuppressor_Gene>())
                {
                    ForceRun = true;
                }
                if (def.HasModExtension<GeneExtension>())
                {
                    ForceRun = true;
                    GeneExt = def.GetModExtension<GeneExtension>();
                }
            }
        }

        // Last run state vars:
        #region Previous
        private bool prerequisitesValid = false;
        private bool conditionalsValid = false;
        //private bool isSupressedByGene = false;
        private bool isSupressedByHediff = false;
        private bool conditionalsWasValid = false;
        #endregion
        public bool RegenerateState()
        {
            bool result;
            bool prerequisitesValid = true;
            bool conditionalsValid = true;
            bool isSupressedByGene = Overridden;
            bool refreshGraphics = false;

            // To stop infinite recursion.
            if (!supressPostfix)
            {
                try
                {
                    supressPostfix = true;
                    conditionalsValid = ConditionalManager.TestConditionals(this);
                    prerequisitesValid = PrerequisiteValidator.Validate(def, pawn);
                    if (conditionalsValid != this.conditionalsValid || prerequisitesValid != this.prerequisitesValid)
                    {
                        refreshGraphics = true;
                    }
                    this.conditionalsValid = conditionalsValid;
                    this.prerequisitesValid = prerequisitesValid;

                }
                finally
                {
                    supressPostfix = false;
                }
            }
            // Outside of the loop so it can supress genes which supresses other genes.
            bool isSupressedByHediff = GeneSuppressorManager.IsSupressedByHediff(def.defName, pawn);
            result = prerequisitesValid && conditionalsValid && !isSupressedByGene && !isSupressedByHediff;

            if (!supressPostfix && !supressPostfix2)
            {
                supressPostfix2 = true;

                if (isSupressedByHediff != this.isSupressedByHediff) { refreshGraphics = true; }
                if (conditionalsWasValid != conditionalsValid) { refreshGraphics = true; }
                this.isSupressedByHediff = isSupressedByHediff;


                if (result != previouslyActive)
                {
                    UpdateOverridenGenes(def, pawn.genes);
                    GeneEffectManager.GainOrRemovePassion(!result, this);
                    GeneEffectManager.GainOrRemoveAbilities(!result, this);
                    GeneEffectManager.ApplyForcedTraits(!result, this);
                }
                
                try
                {
                    if (previouslyActive != result || refreshGraphics)
                    {
                        //lastUpdate = Time.realtimeSinceStartup;
                        //previouslyActive = result;

                        //PostActivateOrDeactivate();
                        if ((def.HasDefinedGraphicProperties || refreshGraphics) && Thread.CurrentThread == BigSmall.mainThread)
                        {
                            pawn.Drawer.renderer.SetAllGraphicsDirty();
                        }
                        // Count down faster if the status changed, but still not every frame.
                        refreshGeneEffectsCountdown-=2;
                    }
                    // Updating effects could get expensive, so only do it either when something changed or just plain
                    // rarely. The rare check is for changes to equipment, injuries, implants, and stuff like that.
                    refreshGeneEffectsCountdown--;
                    if (refreshGeneEffectsCountdown <= 0)
                    {
                        refreshGeneEffectsCountdown = 5;
                        var change = GeneEffectManager.RefreshGeneEffects(this, result, geneExt: GeneExt);

                        // Check if on main thread
                        if (change && Thread.CurrentThread == BigSmall.mainThread)
                        {
                            pawn.Drawer.renderer.SetAllGraphicsDirty();
                        }
                    }
                }
                finally
                {
                    conditionalsWasValid = conditionalsValid;
                    supressPostfix2 = false;
                }

                lastUpdate = Time.realtimeSinceStartup;
                lastUpdateTicks = Find.TickManager.TicksGame;// Time.realtimeSinceStartup;
                //if (previouslyActive != result)
                //{
                //    Log.Message($"CHANGED PREVIOUSLY ACTIVE from {previouslyActive} to {result}: {def.defName} - {result} && {prerequisitesValid} && {conditionalsValid} && {Overridden}: {overriddenByGene?.def.defName} && {isSupressedByHediff}");
                //}

                previouslyActive = result;
            }

            

            return result;
        }

        public static void UpdateOverridenGenes(GeneDef supresserDef, Pawn_GeneTracker __instance, bool forceRemove = false)
        {
            try
            {
                foreach (var supresserGene in __instance.GenesListForReading)
                {
                    if (supresserGene.def.HasModExtension<GeneSuppressor_Gene>())
                    {
                        var geneExtension = supresserGene.def.GetModExtension<GeneSuppressor_Gene>();
                        //var supresserGeneLst = Helpers.GetActiveGeneByName(__instance.pawn, supresserDef.defName);
                        //var supresserGene = supresserGeneLst.Count > 0 ? supresserGeneLst.First() : null;

                        foreach (string supressedGene in geneExtension.supressedGenes)
                        {
                            foreach (var item in GeneHelpers.GetGeneByName(__instance.pawn, supressedGene))
                            {

                                if (supresserGene != null && supresserGene.Active)
                                {
                                    if (!item.Overridden)
                                    {
                                        item.OverrideBy(supresserGene);
                                        //Log.Message($"{supresserGene.def.defName} is suppressing {item.def.defName}");
                                    }
                                }
                                else
                                {
                                    if (item.overriddenByGene == supresserGene)
                                    {
                                        item.OverrideBy(null);
                                        //Log.Message($"{supresserGene.def.defName} is no longer suppressing {item.def.defName}");
                                    }
                                }
                            }
                        }
                    }
                }

                // If the supressor gene was removed, remove the supression.
                if (__instance.GenesListForReading.Where(x => x.def == supresserDef).ToList().Count == 0 && supresserDef.HasModExtension<GeneSuppressor_Gene>())
                {
                    var geneExtension = supresserDef.GetModExtension<GeneSuppressor_Gene>();
                    foreach (string supressedGene in geneExtension.supressedGenes)
                    {
                        foreach (var item in GeneHelpers.GetActiveGeneByName(__instance.pawn, supressedGene))
                        {
                            if (item.overriddenByGene.def == supresserDef)
                            {
                                item.OverrideBy(null);
                                //Log.Message($"{supresserDef.defName} is no longer suprrssing {item}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Message("An Error occured when updating overriden genes. If this happened during loading it is 99% likely to be harmless, and can be ignored.");
                Log.Message($"Caught in {MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialized, "PGeneInit", false);
            Scribe_Values.Look(ref previouslyActive, "PGeneActive", true);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                BigAndSmallCache.pGenesThatReevaluate.Add(this);
            }
        }
    }
}
