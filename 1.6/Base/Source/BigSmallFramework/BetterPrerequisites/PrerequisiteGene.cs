//using BigAndSmall;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Threading;
//using UnityEngine;
//using Verse;
//using static UnityEngine.Random;

//namespace BigAndSmall
//{
//    public class PGene : Gene
//    {
//        protected bool? previouslyActive = null;
//        protected float lastUpdateTicks = 0f;
//        protected bool overridenByDummy = false;
//        protected string disabledReason = null;

//        private bool initialized = false;
        
//        public bool hasExtension = false;

//        private bool lookedForGeneExt = false;
//        private List<PawnExtension> geneExt = null;

//        private List<PawnExtension> GeneExt
//        {
//            get
//            {
//                if (geneExt == null && !lookedForGeneExt)
//                {
//                    geneExt = def.ExtensionsOnDef<PawnExtension, GeneDef>();
//                    lookedForGeneExt = true;
//                }
//                return geneExt;
//            }
//            set => geneExt = value;
//        }

//        //public override bool Active => TryGetGeneActiveCache();

//        public bool ForceRun { get; set; } = false;
        

//        //public CacheTimer Timer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

//        //public bool postPostAddDone = false;

//        /// <summary>
//        /// Deliberately generate late to avoid getting picked up by random frameworks.
//        /// </summary>
        

//        public override void PostAdd()
//        {
//            SetupVars();
//            base.PostAdd();
//        }

//        public override void PostRemove()
//        {
//            BigAndSmallCache.frequentUpdateGenes.Remove(this);
//            base.PostRemove();
//            if (GeneExt != null)
//            {
//            }
//        }

//        public override void TickInterval(int delta)
//        {
//            base.TickInterval(delta);
//            int currentTick = BS.Tick;
//            if (currentTick % 1000 == 0 && Active)
//            {
//                // Try triggering transform genes if it exists.
//                //GeneExt.ForEach(x=>x.transformGene?.TryTransform(pawn, this));
//            }

//            if (pawn.needs != null && currentTick % 100 == 0 && Active)
//            {
//                if (GeneExt != null && GeneExt.Where(x=>x.lockedNeeds != null).Any())
//                {
                    
//                }
//            }
//        }

        
        
//        /// <summary>
//        /// Mostly because PostAdd doesn't run on save load and stuff like that. But I don't think trying to fetch the ModExtension every time is a good idea.
//        /// </summary>
//        public void SetupVars()
//        {
//            if (!initialized)
//            {
//                initialized = true;
//                //if (def.HasModExtension<GeneSuppressor_Gene>())
//                //{
//                //    ForceRun = true;
//                //}
//                if (def.HasModExtension<PawnExtension>())
//                {
//                    ForceRun = true;
//                    GeneExt = def.ExtensionsOnDef<PawnExtension, GeneDef>();
//                }
//            }
//        }
//        public void RefreshEffects()
//        {
//            //if (lastUpdateTicks - Find.TickManager.TicksGame > 1000 || GeneExt.Any(x => x.frequentUpdate))
//            //{
//            //    GeneEffectManager.RefreshGeneEffects(this, Active);
//            //    lastUpdateTicks = Find.TickManager.TicksGame;
//            //}
//        }



//        public override void ExposeData()
//        {
//            base.ExposeData();
//            Scribe_Values.Look(ref initialized, "PGeneInit", false);
//            Scribe_Values.Look(ref previouslyActive, "PGeneActive", true);
//            Scribe_Values.Look(ref overridenByDummy, "PGeneOverridenByDummy", false);
//            Scribe_Values.Look(ref disabledReason, "PGeneDisabledReason", null);
//            if (Scribe.mode == LoadSaveMode.PostLoadInit)
//            {
//                BigAndSmallCache.frequentUpdateGenes.Add(this, null);
//            }
//        }
//    }
//}
