using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class ConditionalTextureDef : Def
    {
        public ConditionalTexture graphic = new();
    }
    public class ConditionalTexture : ConditionalGraphic
    {
        public ConditionalTextureDef replacementDef = null;
        public AdaptivePathPathList texturePaths = [];
        public Vector2 drawSize = Vector2.one;

        public List<ConditionalTexture> alts = [];

        public bool TryGetPath(BSCache cache, ref string path)
        {
            var pawn = cache.pawn;
            foreach (var alt in alts)
            {
                if (alt.GetState(pawn) == false) { continue; }
                if (alt.TryGetPath(cache, ref path))
                {
                    return true;
                }
            }
            if (replacementDef != null)
            {
                if (replacementDef.graphic.TryGetPath(cache, ref path))
                {
                    return true;
                }
            }
            var pathsSrc = texturePaths;

            foreach (var gfxOverride in GetGraphicOverrides(pawn))
            {
                gfxOverride.graphics.OfType<ConditionalTexture>().Where(x => x != null).Do(x =>
                {
                    pathsSrc = x.texturePaths;
                });
            }

            if (texturePaths.Count == 0) { return false; }
            var paths = pathsSrc.GetPaths(cache);
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
