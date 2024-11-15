using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigAndSmall
{
    public class ConditionalGraphicsSet : ConditionalGraphic
    {
        public ColorSetting colorA = new();
        public ColorSetting colorB = new();
        protected ConditionalPath conditionalPaths = null;
        public AdaptivePathPathList texturePaths = [];
        public string GetPath(BSCache cache, string path) => conditionalPaths?.TryGetPath(cache, ref path) == true || texturePaths.TryGetPath(cache, ref path) ? path : path;
        public List<ConditionalGraphicsSet> alts = [];

        public ConditionalGraphicsSet GetGraphicsSet(BSCache cache)
        {
            foreach (var alt in alts)
            {
                var result = alt.GetState(cache.pawn);
                if (result == false) { continue; }
                if (result && alt.GetGraphicsSet(cache) is ConditionalGraphicsSet altSet)
                {
                    return altSet;
                }
            }
            return this;
        }
    }
}
