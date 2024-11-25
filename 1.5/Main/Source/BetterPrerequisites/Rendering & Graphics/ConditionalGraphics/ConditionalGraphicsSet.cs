using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class ConditionalGraphicsSet : ConditionalGraphic
    {
        public ColorSetting colorA = new();
        public ColorSetting colorB = new();
        public ConditionalGraphicProperties props = new();
        protected ConditionalTexture conditionalPaths = null;
        public AdaptivePathPathList texturePaths = [];
        public string GetPath(BSCache cache, string path) => conditionalPaths?.TryGetPath(cache, ref path) == true || texturePaths.TryGetPath(cache, ref path) ? path : path;

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
