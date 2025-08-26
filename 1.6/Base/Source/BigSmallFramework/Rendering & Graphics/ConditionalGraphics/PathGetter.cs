using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class PathGetter : ConditionalGraphic
    {
        public enum TextureSource
        {
            None,
            IdeologyIcon,

        }
        public const string BlankPath = "BS_Blank";

        public TextureSource source = TextureSource.None;
        public Vector2 drawSize = Vector2.one;

        public List<PathGetter> alts = [];

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
            if (source == TextureSource.IdeologyIcon && ModsConfig.IdeologyActive && pawn.Ideo is RimWorld.Ideo ideology)
            {
                path = ideology.iconDef.iconPath;
            }
            else
            {
                path = BlankPath;
            }

            return true;
        }
    }
}
