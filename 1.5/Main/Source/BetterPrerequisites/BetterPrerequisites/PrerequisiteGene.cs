using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Verse;
using static UnityEngine.Random;

namespace BetterPrerequisites
{
    public class PGene : Gene
    {
        protected bool? previouslyActive = null;
        protected float lastUpdateTicks = 0f;
        protected bool overridenByDummy = false;
        protected string disabledReason = null;

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

        //public override bool Active => TryGetGeneActiveCache();

        public bool ForceRun { get; set; } = false;
        

        //public CacheTimer Timer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //public bool postPostAddDone = false;

        /// <summary>
        /// Deliberately generate late to avoid getting picked up by random frameworks.
        /// </summary>
        

        public override void PostAdd()
        {
            SetupVars();
            base.PostAdd();

            bool needsReevaluation = false;
            bool state = true;
            if (GeneExt != null)
            {
                if (base.Active && Active)
                {
                    state = true;
                    GeneRequestThingSwap(true);
                }
                else { state = false; }

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
                BigAndSmallCache.frequentUpdateGenes[this] = state;
            }
        }

        public override void PostRemove()
        {
            BigAndSmallCache.frequentUpdateGenes.Remove(this);
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



        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialized, "PGeneInit", false);
            Scribe_Values.Look(ref previouslyActive, "PGeneActive", true);
            Scribe_Values.Look(ref overridenByDummy, "PGeneOverridenByDummy", false);
            Scribe_Values.Look(ref disabledReason, "PGeneDisabledReason", null);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                BigAndSmallCache.frequentUpdateGenes.Add(this, null);
            }
        }
    }
}
