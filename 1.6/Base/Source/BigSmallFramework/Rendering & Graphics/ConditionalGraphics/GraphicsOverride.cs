using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class GraphicsOverride : DefModExtension
    {
        public FlagStringList replaceFlags = [];

        public List<GraphicsOverride> overrideList = [];
        public float priority = 0;
        public List<ConditionalGraphic> graphics = [];
        public Vector2 drawSize = Vector2.one;

        public List<GraphicsOverride> Overrides
        {
            get
            {
                if (overrideList.Any())
                {
                    return [.. overrideList.SelectMany(x => x.Overrides).OrderByDescending(x => x.priority)];
                }
                return [this];
            }
        }
    }
}
