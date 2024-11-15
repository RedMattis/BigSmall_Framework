using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse;
using static BigAndSmall.ConditionalGraphic;

namespace BigAndSmall
{
    public class ConditionalPath : ConditionalGraphic
    {
        public AdaptivePathPathList texturePaths = [];

        public List<ConditionalPath> alts = [];

        public bool TryGetPath(BSCache cache, ref string path)
        {
            var pawn = cache.pawn;
            foreach (var alt in alts)
            {
                var result = alt.GetState(pawn);
                if (result == false) { continue; }
                if ( alt.TryGetPath(cache, ref path))
                {
                    return true;
                }
            }
            if (texturePaths.Count == 0) { return false; }
            var paths = texturePaths.GetPaths(cache);
            if (paths.Count == 0) { return false; }

            int pawnRNGSeed = pawn.thingIDNumber + pawn.def.defName.GetHashCode();
            using (new RandBlock(pawnRNGSeed))
            {
                path = paths.RandomElement();
            }
            return true;
        }
    }
}
