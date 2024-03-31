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

        public bool triggerNalFaceDisable = false;

        [Unsaved(false)]
        private bool setupvars = false;
        
        public bool hasExtension = false;
        
        private GeneExtension geneExt = null;

        public override bool Active => TryGetGeneActiveCache(base.Active);

        public bool ForceRun { get; set; } = false;

        public bool postPostAddDone = false;

        public override void PostAdd()
        {
            SetupVars();
            base.PostAdd();

            if (geneExt != null)
            {
                if (base.Active && Active)
                {
                    SwapThingDef(true);
                }
                bool valid = HiddenGeneHasParent();
                // If not, remove this.
                if (!valid)
                {
                    pawn.genes.RemoveGene(this);
                }

                // Check if this is a xenogene.
                bool xenoGene = pawn.genes.Xenogenes.Any(x => x == this);

                foreach(var geneDef in geneExt.hiddenGenes)
                {
                    // Add hidden gene
                    pawn.genes.AddGene(geneDef, xenoGene);
                }
                // Disable facial animations from Nal's Facial Animation mod
                if (ModsConfig.IsActive("nals.facialanimation") && geneExt.facialDisabler != null)
                {
                    triggerNalFaceDisable = true;
                }
            }
        }

        public void PostPostAdd()
        {
            if (!postPostAddDone)
            {
                if (geneExt != null)
                {
                    SwapThingDef(true);
                }
            }
            postPostAddDone = true;
        }

        public override void PostRemove()
        {
            base.PostRemove();
            if (geneExt != null)
            {
                SwapThingDef(false, force:true);

                // If this is the last active gene of its type...
                bool lastActiveOfDef = !pawn.genes.GenesListForReading.Any(x => x.def == def && x != this);
                if (lastActiveOfDef)
                {
                    foreach (var geneDef in geneExt.hiddenGenes)
                    {
                        // Remove all genes matching def
                        var matchingGenes = pawn.genes.GenesListForReading.Where(x => x.def == geneDef).ToList(); ;
                        foreach (var gene in matchingGenes)
                        {
                            pawn.genes.RemoveGene(gene);
                        }
                    }
                }
                if (ModsConfig.IsActive("nals.facialanimation") && geneExt.facialDisabler != null)
                {
                    // Check if any other genes have facialDisabler
                    bool otherFacialDisabler = pawn.genes.GenesListForReading.Any(x => x.def.HasModExtension<GeneExtension>() && x.def.GetModExtension<GeneExtension>().facialDisabler != null);
                    if (!otherFacialDisabler)
                    {
                        try
                        {
                            NalFaceExt.DisableFacialAnimations(pawn, geneExt.facialDisabler, revert:true);
                        }
                        catch (Exception e)
                        {
                            Log.Message($"Error in PostRemove: {e.Message}");
                        }
                    }
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            // Every 5000 ticks
            if (Find.TickManager.TicksGame % 5000 == 0)
            {
                // Remove hidden gene if it has no parent. This is to catch genes added by mods that some avoid triggering the PostAdd. (Sarg. What are you doing?) ^_^;;
                if (geneExt != null && !HiddenGeneHasParent())
                {
                    pawn.genes.RemoveGene(this);
                    //Log.Message($"Removed Orphaned Gene: {def.defName} from {pawn.Name}.");
                }
                // Clear saved Hediffs. It is only to be used for the instant when a swap occurs.
                hediffsToReapply.Clear();

                // Try triggering transform genes if it exists.
                geneExt?.transformGene?.TryTransform(pawn, this);
            }
            if (Find.TickManager.TicksGame % 5000 == 5)
            {
                PostPostAdd();
            }
            if (Find.TickManager.TicksGame % 100 == 0 && triggerNalFaceDisable)
            {
                try
                {
                    if (ModsConfig.IsActive("nals.facialanimation") && geneExt != null && geneExt.facialDisabler != null)
                    {
                        triggerNalFaceDisable = false;
                        NalFaceExt.DisableFacialAnimations(pawn, geneExt.facialDisabler, revert: false);
                    }
                }
                catch (Exception e)
                {
                    Log.Message($"Error in Tick: {e.Message}");
                }
            }

        }

        public static Dictionary<Pawn, List<Hediff>> hediffsToReapply = new Dictionary<Pawn, List<Hediff>>();
        public void SwapThingDef(bool state, bool force=false)
        {
            try
            {
                if (geneExt != null && geneExt.thingDefSwap != null)
                {
                    var genesWithThingDefSwaps = pawn.genes.GenesListForReading
                        .Where(x => x != this && x is PGene && (x as PGene).geneExt != null && (x as PGene).geneExt.thingDefSwap != null)
                        .Select(x=>(PGene)x).ToList();

                    // Check if the ThingDef we CURRENTLY are is among the genesWithThingDefSwaps
                    var geneWithThingDef = genesWithThingDefSwaps.Where(x => x.geneExt.thingDefSwap.defName == pawn.def.defName);

                    bool forceSwap = geneExt.forceThingDefSwap;
                    // if all geneWithThingDef are inactive, we can swap.
                    if ( geneWithThingDef.All(x => x.Overridden))
                    {
                        force = true;
                        forceSwap = true;
                    }

                    if (!force)
                    {
                        if (state && geneExt.thingDefSwap.defName == pawn.def.defName)
                        {
                            return;
                        }
                    }

                    if (!hediffsToReapply.ContainsKey(pawn)) hediffsToReapply[pawn] = new List<Hediff>();
                    try
                    {
                        RegionListersUpdater.DeregisterInRegions(pawn, pawn.Map);
                    }
                    catch { }
                    // Check if the pawn is a human or we're forcing the swap.
                    if ((pawn.def == ThingDefOf.Human || forceSwap) && state)
                    {
                        // Don't swap to a thingDef that is already active.
                        if (pawn.def.defName != geneExt.thingDefSwap.defName)
                        {
                            // Change the pawn's thingDef to the one specified in the GeneExtension.
                            CacheAndRemoveHediffs(geneExt.thingDefSwap);
                            pawn.def = geneExt.thingDefSwap;
                            RestoreMatchingHediffs(geneExt.thingDefSwap);
                        }
                    }
                    // Check if we're turning off this ThingDef and would want to swap to another.

                    else if (!state && pawn.def.defName == geneExt.thingDefSwap.defName)
                    {
                        ThingDef target = ThingDefOf.Human;
                        if (genesWithThingDefSwaps.Count > 0) { target = genesWithThingDefSwaps.RandomElement().geneExt.thingDefSwap; }

                        CacheAndRemoveHediffs(target);
                        // Change the pawn's thingDef to a baseliner.
                        pawn.def = target;
                        RestoreMatchingHediffs(target);
                    }
                    try
                    {
                        RegionListersUpdater.RegisterInRegions(pawn, pawn.Map);
                    }
                    catch { }

                    pawn.VerbTracker.InitVerbsFromZero();

                }
            }
            catch (Exception e)
            {
                Log.Message($"Error in SwapThingDef (if this happend during world gen it is nothing to worry about): {e.Message}");
            }
        }

        public void CacheAndRemoveHediffs(ThingDef targetThingDef)
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
                            BodyPartRecord partMatchingHediff = currentParts.FirstOrDefault(x => x.def.defName == hediff.Part.def.defName && x.customLabel == hediff.Part.customLabel);

                            pawn.health.AddHediff(hediff.def, part: partMatchingHediff);
                        }
                        
                        // remove hediff from savedHediffs
                        hediffsToReapply[pawn].RemoveAt(idx);
                    }
                }
                // Find all active genes of type Gene_ChemicalDependency
                foreach(var chemGene in Helpers.GetAllActiveEndoGenes(pawn).Where(x=>x is Gene_ChemicalDependency).Select(x=> (Gene_ChemicalDependency)x).ToList())
                {
                    RestoreDependencies(chemGene, xenoGene:false);
                }
                foreach (var chemGene in Helpers.GetAllActiveXenoGenes(pawn).Where(x => x is Gene_ChemicalDependency).Select(x => (Gene_ChemicalDependency)x).ToList())
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

        /// <summary>
        /// Despite the name the gene is actually visible. It otherwise works as intended, but I should probably fix UI issue...
        /// </summary>
        private bool HiddenGeneHasParent()
        {
            if (geneExt != null && geneExt.hiddenAddon)
            {
                return pawn.genes.GenesListForReading.Any(x => x.def.HasModExtension<GeneExtension>() && x.def.GetModExtension<GeneExtension>().hiddenGenes.Any(y => y == def));
            }
            return true;
        }

        /// <summary>
        /// Mostly because PostAdd doesn't run on save load and stuff like that. But I don't think trying to fetch the ModExtension every time is a good idea.
        /// </summary>
        public void SetupVars()
        {
            if (!setupvars)
            {
                setupvars = true;
                if (def.HasModExtension<GeneSuppressor_Gene>())
                {
                    ForceRun = true;
                }
                if (def.HasModExtension<GeneExtension>())
                {
                    ForceRun = true;
                    geneExt = def.GetModExtension<GeneExtension>();
                }
            }
        }

        

        readonly float updateFrequence = 125; //1.5f;
        readonly float updateFrequenceRealTime = 2.0f; //1.5f;
        public bool TryGetGeneActiveCache(bool result)
        {
            if (pawn != null && !PawnGenerator.IsBeingGenerated(pawn))
            {
                if (result || ForceRun)
                {
                    bool newResult = result;
                    bool useCache = false;
                    if (BigSmallMod.settings.realTimeUpdates)
                    {
                        if (Time.realtimeSinceStartup - lastUpdate < updateFrequenceRealTime)
                        {
                            newResult = previouslyActive == true;
                            useCache = true;
                        }
                    }
                    else
                    {
                        if (Find.TickManager.TicksGame - lastUpdateTicks < updateFrequence)
                        {
                            newResult = previouslyActive == true;
                            useCache = true;
                        }
                    }
                    
                    if (!useCache || ForceRun)
                    {
                        SetupVars();
                        newResult = GetGeneActive(pawn);
                    }
                    result = newResult && result;
                }
            }

            return result;
        }

        // Last run state vars:
        #region Previous
        private bool prerequisitesValid = false;
        private bool conditionalsValid = false;
        //private bool isSupressedByGene = false;
        private bool isSupressedByHediff = false;
        private bool conditionalsWasValid = false;
        #endregion
        private bool GetGeneActive(Pawn pawn, bool forceUpdate=false)
        {
            bool result;
            bool prerequisitesValid = true;
            bool conditionalsValid = true;
            bool isSupressedByGene = false;
            bool refreshGraphics = false;


            // To stop infinite recursion.
            if (!supressPostfix)
            {
                try
                {
                    supressPostfix = true;
                    conditionalsValid = ConditionalManager.TestConditionals(this);
                    prerequisitesValid = PrerequisiteValidator.Validate(def, pawn) && HiddenGeneHasParent();

                    isSupressedByGene = Overridden;
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

            if (!supressPostfix2 && !supressPostfix)
            {
                supressPostfix2 = true;

                if (isSupressedByHediff != this.isSupressedByHediff) { refreshGraphics = true; }
                if (conditionalsWasValid != conditionalsValid) { refreshGraphics = true; }
                this.isSupressedByHediff = isSupressedByHediff;


                if (result != previouslyActive || forceUpdate)
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
                        if ((def.HasGraphic || refreshGraphics) && Thread.CurrentThread == BigSmall.mainThread)
                        {
                            pawn.Drawer.renderer.graphics.SetGeneGraphicsDirty();
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
                        var change = GeneEffectManager.RefreshGeneEffects(this, result, geneExt: geneExt);

                        // Check if on main thread


                        if (change && Thread.CurrentThread == BigSmall.mainThread)
                        {
                            pawn.Drawer.renderer.graphics.SetGeneGraphicsDirty();
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


    }
}
