using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Verse;

namespace BetterPrerequisites
{
    public class PGene : Gene
    {
        /// <summary>
        /// Avoid recursion by supressing this method when it woudld get called from itself.
        /// </summary>
        
        private int refreshGeneEffectsCountdown = 5;

        public bool? previouslyActive = null;
        public float lastUpdateTicks = 0f;
        public float lastUpdate = 0f;
        //public bool triggerNalFaceDisable = false;

        private bool initialized = false;
        
        public bool hasExtension = false;

        private bool lookedForGeneExt = false;
        private List<PawnExtension> geneExt = null;

        private List<PawnExtension> GeneExt
        {
            get
            {
                if (geneExt == null && !lookedForGeneExt)
                {
                    geneExt = def.ExtensionsOnDef<PawnExtension, GeneDef>();
                    lookedForGeneExt = true;
                }
                return geneExt;
            }
            set => geneExt = value;
        }

        public override bool Active => TryGetGeneActiveCache(base.Active);

        public bool ForceRun { get; set; } = false;
        //public CacheTimer Timer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //public bool postPostAddDone = false;

        public override void PostAdd()
        {
            SetupVars();
            base.PostAdd();

            bool needsReevaluation = false;

            if (GeneExt != null)
            {
                if (base.Active && Active)
                {
                    GeneRequestThingSwap(true);
                }

                // Check if this is a xenogene.
                bool xenoGene = pawn.genes.Xenogenes.Any(x => x == this);

                foreach(var geneDef in GeneExt.SelectMany(x=>x.hiddenGenes))
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

                if (GeneExt.Any(x=>x.conditionals != null))
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

        public override void PostRemove()
        {
            BigAndSmallCache.pGenesThatReevaluate.Remove(this);
            base.PostRemove();
            if (GeneExt != null)
            {
                GeneRequestThingSwap(false);

                // If this is the last active gene of its type...
                bool lastActiveOfDef = !pawn.genes.GenesListForReading.Any(x => x.def == def && x != this);
                if (lastActiveOfDef)
                {
                    foreach (var geneDef in GeneExt.Where(x=>x.hiddenGenes != null).SelectMany(x => x.hiddenGenes))
                    {
                        // Remove all genes matching def
                        var matchingGenes = pawn.genes.GenesListForReading.Where(x => x.def == geneDef).ToList(); ;
                        foreach (var gene in matchingGenes)
                        {
                            pawn.genes.RemoveGene(gene);
                        }
                    }
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
                // Try triggering transform genes if it exists.
                GeneExt.ForEach(x=>x.transformGene?.TryTransform(pawn, this));
            }
            //if (currentTick % 5000 == 5)
            //{
            //    PostPostAdd();
            //}

            if (currentTick % 100 == 0 && pawn.needs != null && Active)
            {
                if (GeneExt != null && GeneExt.Where(x=>x.lockedNeeds != null).Any())
                {
                    foreach (var lockedNeed in GeneExt.Where(x => x.lockedNeeds != null)
                        .SelectMany(x => x.lockedNeeds).Where(x=>x.need != null))
                    {
                        float value = lockedNeed.value;
                        bool minValue = lockedNeed.minValue;
                        NeedDef needDef = lockedNeed.need;

                        var need = pawn.needs.TryGetNeed(needDef);

                        if (need != null)
                        {
                            if (minValue)
                            {
                                if (need.CurLevelPercentage < value)
                                {
                                    need.CurLevel = need.MaxLevel * value;
                                }
                            }
                            else
                            {
                                need.CurLevel = need.MaxLevel * value;
                            }
                        }
                    }
                }
            }
        }

        public void GeneRequestThingSwap(bool state)
        {
            if (GeneExt != null && GeneExt.Any(x=>x.thingDefSwap != null))
            {
                GeneHelpers.CheckForOverrides(pawn);
                var firstValid = GeneExt.Where(x => x.thingDefSwap != null).Select(x => x.thingDefSwap).First();
                RaceMorpher.SwapThingDef(pawn, firstValid, state, targetPriority:0, source:this);
            }
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
                if (def.HasModExtension<PawnExtension>())
                {
                    ForceRun = true;
                    GeneExt = def.ExtensionsOnDef<PawnExtension, GeneDef>();
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

        public static bool supressPostfix = false;
        public static bool supressPostfix2 = false;

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
                    var allPawnGenes = pawn.genes.GenesListForReading;
                    isSupressedByGene = isSupressedByGene && allPawnGenes.Contains(overriddenByGene);

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
            bool isSupressedByHediff = GeneSuppressorManager.IsSupressedByHediff(def, pawn);
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
                        if (result)
                        {
                            overriddenByGene = null;
                        }
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
                        var change = GeneEffectManager.RefreshGeneEffects(this, result);

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
