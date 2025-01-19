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
        private static Gene dummyGene = null;
        private static bool dummyGeneCreated = false;
        /// <summary>
        /// Avoid recursion by supressing this method when it woudld get called from itself.
        /// </summary>

        public bool? previouslyActive = null;
        public float lastUpdateTicks = 0f;
        public bool overridenByDummy = false;
        public string disabledReason = null;

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

        public override bool Active => TryGetGeneActiveCache();

        public bool ForceRun { get; set; } = false;
        public static Gene DummyGene { get => !dummyGeneCreated ? MakeDummyGene() : dummyGene; set => dummyGene = value; }

        //public CacheTimer Timer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //public bool postPostAddDone = false;

        /// <summary>
        /// Deliberately generate late to avoid getting picked up by random frameworks.
        /// </summary>
        public static Gene MakeDummyGene()
        {
            dummyGene ??= new Gene
            {
                def = new GeneDef()
                {
                    defName = "BS_PDummyGene",
                    label = "BS_RequirementNotMet".Translate().CapitalizeFirst(),
                    description = "System gene..",
                    displayCategory = GeneCategoryDefOf.Miscellaneous,
                    canGenerateInGeneSet = false,
                    selectionWeight = 0,
                    selectionWeightCultist = 0,
                }
            };
            dummyGeneCreated = true;
            return DummyGene;
        }

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
                    pawn.genes.AddGene(geneDef, xenoGene);
                }

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
                pawn.genes.CheckForOverrides();
                var firstValid = GeneExt.Where(x => x.thingDefSwap != null).Select(x => x.thingDefSwap).First();
                RaceMorpher.SwapThingDef(pawn, firstValid, state, targetPriority:0, source:this);
            }
        }
        
        readonly float updateFrequence = 125; //1.5f;
        readonly float updateFrequenceRealTime = 2.0f; //1.5f;
        public static object locker = new object();

        public bool TryGetGeneActiveCache()
        {
            bool result;
            try
            {
                result = base.Active;
            }
            catch (Exception e)
            {
                Log.Warning($"Prerequisite Gene caught an exception when trying to check the Gene Base active state in " +
                    $"{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}.\nStackTrace:\n{e.Message}\n{e.StackTrace}");
                return false;
            }
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
        public void RefreshEffects()
        {
            if (lastUpdateTicks - Find.TickManager.TicksGame > 1000 || GeneExt.Any(x => x.frequentUpdate))
            {
                GeneEffectManager.RefreshGeneEffects(this, Active);
                lastUpdateTicks = Find.TickManager.TicksGame;
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
            if (isSupressedByGene && overriddenByGene == dummyGene)
            {
                isSupressedByGene = false;
            }
            bool refreshGraphics = false;

            // To stop infinite recursion.
            if (!supressPostfix)
            {
                try
                {
                    supressPostfix = true;
                    var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
                    isSupressedByGene = isSupressedByGene && activeGenes.Contains(overriddenByGene);

                    conditionalsValid = ConditionalManager.TestConditionals(this, GeneExt);
                    prerequisitesValid = PrerequisiteValidator.Validate(def, pawn, ref disabledReason);

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
                try
                {
                    if (isSupressedByHediff != this.isSupressedByHediff) { refreshGraphics = true; }
                    if (conditionalsWasValid != conditionalsValid) { refreshGraphics = true; }
                    this.isSupressedByHediff = isSupressedByHediff;

                    if (result != previouslyActive)
                    {
                        UpdateOverridenGenes(def, pawn.genes);
                        GeneEffectManager.GainOrRemovePassion(!result, this);
                        GeneEffectManager.GainOrRemoveAbilities(!result, this);
                        GeneEffectManager.ApplyForcedTraits(!result, this);
                        GeneEffectManager.RefreshGeneEffects(this, result);
                        HumanoidPawnScaler.ShedueleForceRegenerateSafe(pawn, 2);
                    }
                }
                finally
                {
                    conditionalsWasValid = conditionalsValid;
                    supressPostfix2 = false;
                }

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
            Scribe_Values.Look(ref overridenByDummy, "PGeneOverridenByDummy", false);
            Scribe_Values.Look(ref disabledReason, "PGeneDisabledReason", null);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                BigAndSmallCache.pGenesThatReevaluate.Add(this);
            }
        }
    }
}
