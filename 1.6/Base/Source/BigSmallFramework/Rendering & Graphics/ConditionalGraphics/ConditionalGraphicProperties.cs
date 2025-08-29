using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class ConditionalGraphicProperties : ConditionalGraphic
    {
        public Vector2 drawSize = Vector2.one;
        public ShaderTypeDef shader = null;

        public List<ConditionalGraphicProperties> alts = [];

        public ConditionalGraphicProperties GetGraphicProperties(BSCache cache)
        {
            foreach (var alt in alts)
            {
                if (alt.GetState(cache.pawn) == false) { continue; }
                if (alt.GetGraphicProperties(cache) is ConditionalGraphicProperties altProps)
                {
                    return altProps;
                }
            }
            var target = this;
            foreach (var gfxOverride in GetGraphicOverrides(cache.pawn))
            {
                gfxOverride.graphics.OfType<ConditionalGraphicProperties>().Where(x => x != null).Do(x =>
                {
                    target = x;
                });
            }
            return target;
        }
    }
}
