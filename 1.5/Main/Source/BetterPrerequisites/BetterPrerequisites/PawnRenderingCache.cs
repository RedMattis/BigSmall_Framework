using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterPrerequisites
{
    public class PawnRendering : GameComponent
    {
        public static PawnRendering instance = null;

        private List<PawnRenderingCache> renderingScribe = null;

        private Dictionary<Pawn, PawnRenderingCache> renderingCacheDict = [];

        public static HashSet<Pawn> pawnsQueueForRendering = [];

        public PawnRendering(Game game)
        {
            instance = this;
        }

        public PawnRendering()
        {
            instance = this;
        }

        //public override void GameComponentTick()
        //{
        //    base.GameComponentTick();
        //    if (Find.TickManager.TicksGame % 50 == 1)
        //    {
        //        if (pawnsQueueForRendering.Count > 0)
        //        {
        //            try
        //            {
        //                // For loop instead of while loop to prevent infinite loop if something goes horribly wrong.
        //                for (int i = 0; i < 10; i++)
        //                {
        //                    if (pawnsQueueForRendering.Count == 0)
        //                    {
        //                        break;
        //                    }
        //                    var pawn = pawnsQueueForRendering.RandomElement();
        //                    if (pawn?.Drawer?.renderer != null)
        //                    {
        //                        pawn.Drawer.renderer.SetAllGraphicsDirty();
        //                    }
        //                    pawnsQueueForRendering.Remove(pawn);
        //                }
        //            }
        //            catch
        //            {
        //                Log.Error($"Error when setting graphics dirty. Actual error below.");
        //                pawnsQueueForRendering.Clear();
        //                throw;
        //            }
        //        }
        //        else
        //        {
        //            pawnsQueueForRendering.AddRange(renderingCacheDict.Keys);
        //        }
        //    }
        //}

        public PawnRenderingCache GetCache(Pawn pawn)
        {
            if (renderingScribe == null)
            {
                renderingScribe = [];
                renderingCacheDict = [];
            }

            if (renderingCacheDict.TryGetValue(pawn, out var cache))
            {
                return cache;
            }
            else
            {
                // Try to get the cache from the list
                foreach (var ca in renderingScribe.Where(x=>x != null))
                {
                    if (ca.pawnHash == pawn.GetHashCode())
                    {
                        renderingCacheDict.Add(pawn, ca);
                        renderingScribe.Add(cache);
                        return ca;
                    }
                }

                // Clean up the list from empty entries
                if (renderingScribe.RemoveAll(x => x == null) > 0)
                {
                    Log.Message("BetterPrerequisites: Cleaned up rendering cache list.");
                }

                cache = new PawnRenderingCache(pawn);
                renderingCacheDict.Add(pawn, cache);
                renderingScribe.Add(cache);
                if (cache == null)
                {
                    Log.Warning("BetterPrerequisites: Failed to create rendering cache for pawn " + pawn);
                }
                return cache;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref renderingScribe, "BetterPrerequisites.renderingCache", LookMode.Deep);
        }
    }

    public class PawnRenderingCache : IExposable
    {
        public int? pawnHash = null;

        public bool hasFur = false;

        private List<string> headDefNames = [];

        public List<string> HeadDefNames
        {
            get
            {
                if (headDefNames == null) headDefNames = [];
                return headDefNames;
            }
            set => headDefNames = value;
        }

        public void AddHeadDefName(string name)
        {
            if (HeadDefNames == null) HeadDefNames = [];
            if (!HeadDefNames.Contains(name))
            {
                HeadDefNames.Add(name);
            }
        }

        //public HashSet<string> geneColorDef = new HashSet<string>();

        public PawnRenderingCache() { }

        public PawnRenderingCache(Pawn pawn)
        {
            var pawnHash = pawn.GetHashCode();

            this.pawnHash = pawnHash;
        }


        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnHash, "BP.renderingPawn");
            Scribe_Values.Look(ref hasFur, "BP.hasFur");
            Scribe_Collections.Look(ref headDefNames, "BP.cachedHeadDefs", LookMode.Value);
        }
    }
}
