using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
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
                    Log.Message("Big and Small: Cleaned up rendering cache list.");
                }

                cache = new PawnRenderingCache(pawn);
                renderingCacheDict.Add(pawn, cache);
                renderingScribe.Add(cache);
                if (cache == null)
                {
                    Log.Warning("Big and Small: Failed to create rendering cache for pawn " + pawn);
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
