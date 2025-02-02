using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace BigAndSmall
{
    public class ConditionalGraphicsSet : ConditionalGraphic
    {
        public ColorSetting colorA = new();
        public ColorSetting colorB = new();
        public ConditionalGraphicProperties props = new();
        protected ConditionalTexture conditionalPaths = null;
        public AdaptivePathPathList texturePaths = [];

        protected ConditionalTexture conditionalMaskPaths = null;
        public AdaptivePathPathList maskPaths = [];
        public string GetPath(BSCache cache, string path) => conditionalPaths?.TryGetPath(cache, ref path) == true || texturePaths.TryGetPath(cache, ref path) ? path : path;

        public string GetMaskPath(BSCache cache, string path) => conditionalMaskPaths?.TryGetPath(cache, ref path) == true || maskPaths.TryGetPath(cache, ref path) ? path : path;

        public List<ConditionalGraphicsSet> alts = [];

        public ConditionalGraphicsSet GetGraphicsSet(BSCache cache)
        {
            foreach (var alt in alts)
            {
                if (alt.GetState(cache.pawn) == false) { continue; }
                if (alt.GetGraphicsSet(cache) is ConditionalGraphicsSet altSet)
                {
                    return altSet;
                }
            }
            var target = this;
            foreach (var gfxOverride in GetGraphicOverrides(cache.pawn))
            {
                gfxOverride.graphics.OfType<ConditionalGraphicsSet>().Where(x => x != null).Do(x =>
                {
                    target = x;
                });
            }



            return this;
        }
    }
}
