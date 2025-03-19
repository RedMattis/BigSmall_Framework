using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class GraphicSetDef : Def
    {
        public ConditionalGraphicsSet conditionalGraphics = new();
    }

    public class ConditionalGraphicsSet : ConditionalGraphic
    {
        public GraphicSetDef replacementDef = null;
        public List<GraphicSetDef> altDefs = [];
        protected ColorSetting colorA = new();
        protected ColorSetting colorB = new();
        protected ColorSettingDef colorADef = null;
        protected ColorSettingDef colorBDef = null;
        public ConditionalGraphicProperties props = new();
        protected ConditionalTexture conditionalPaths = null;
        protected AdaptivePathPathList texturePaths = [];
        protected AdaptivePawnPathDef adaptivePawnPathDef = null;
        protected ConditionalTexture conditionalMaskPaths = null;
        public AdaptivePathPathList maskPaths = [];
        public List<ConditionalGraphicsSet> alts = [];

        public List<GraphicSetDef> AltDefs => replacementDef == null ? [.. altDefs] : [.. altDefs, replacementDef];
        public ColorSetting ColorA => colorADef?.color ?? colorA;
        public ColorSetting ColorB => colorBDef?.color ?? colorB;
        public AdaptivePathPathList TexturePaths => adaptivePawnPathDef?.texturePaths ?? texturePaths;
        public string GetPath(BSCache cache, string path) => conditionalPaths?.TryGetPath(cache, ref path) == true || TexturePaths.TryGetPath(cache, ref path) ? path : path;

        public string GetMaskPath(BSCache cache, string path) => conditionalMaskPaths?.TryGetPath(cache, ref path) == true || maskPaths.TryGetPath(cache, ref path) ? path : path;

        public ConditionalGraphicsSet ReturnThis(BSCache cache)
        {
            if (replacementDef?.conditionalGraphics?.GetState(cache.pawn) == true)
            {
                return replacementDef.conditionalGraphics;
            }
            return this;
        }
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
            foreach (var altDef in AltDefs.Where(x => x.conditionalGraphics.GetState(cache.pawn)))
            {
                if (altDef.conditionalGraphics.GetGraphicsSet(cache) is ConditionalGraphicsSet altSet)
                {
                    return altSet;
                }
            }
            var target = ReturnThis(cache);
            foreach (var gfxOverride in GetGraphicOverrides(cache.pawn))
            {
                gfxOverride.graphics.OfType<ConditionalGraphicsSet>().Where(x => x != null).Do(x =>
                {
                    target = x;
                });
            }
            return ReturnThis(cache);
        }
    }
}
