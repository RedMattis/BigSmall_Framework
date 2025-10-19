using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class ApparelMatch
    {
        public List<string> tags = [];
        public List<BodyPartGroupDef> bodyParts = [];
        public List<ApparelLayerDef> apparelLayers = [];
        public bool requireAllParts = false;
        public bool requireAllLayers = false;

        public static bool Matches(object apparel, ApparelMatch equipTag)
        {
            if (apparel is IEnumerable<ApparelProperties> appProps)
                return equipTag.Matches(appProps);
            return false;
        }
        public bool Matches(IEnumerable<ApparelProperties> apparel)
        {
            if (tags.Any())
            {
                apparel = [.. apparel.Where(x => x.tags.Any(t => tags.Contains(t)))];
            }
            if (apparelLayers.Count > 0)
            {
                var wornLayers = apparel.SelectMany(x => x.layers).ToHashSet();
                if (requireAllLayers)
                {
                    if (!apparelLayers.All(x => wornLayers.Contains(x))) return false;
                }
                else
                {
                    if (!apparelLayers.Any(x => wornLayers.Contains(x))) return false;
                }
            }
            if (bodyParts.Count > 0)
            {
                var wornParts = apparel.SelectMany(x => x.bodyPartGroups).ToHashSet();
                if (requireAllParts)
                {
                    if (!bodyParts.All(x => wornParts.Contains(x))) return false;
                }
                else
                {
                    if (!bodyParts.Any(x => wornParts.Contains(x))) return false;
                }
            }
            return apparel.Any();
        }
    }
}
