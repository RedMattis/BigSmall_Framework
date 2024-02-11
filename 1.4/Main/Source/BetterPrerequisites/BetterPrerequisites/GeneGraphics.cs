using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterPrerequisites
{
    [HarmonyPatch]
    public static class GeneGraphics
    {
        /// <summary>
        /// The genera idea is that if there is body-type specific graphics, we will use those. If not, we will use the default.
        /// It is most tactical to use the "BP_Thin" as default since it is also used by children.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GeneGraphicData), "GraphicPathFor")]
        public static void GraphicPathFor(ref GeneGraphicData __instance, ref string __result, ref Pawn pawn)
        {
            var gPaths = __instance.graphicPaths;
            if (gPaths == null || gPaths.Count == 0)
            {
                return;
            }
            var pawnBodyType = pawn.story.bodyType;
            //var keywords = new List<string> { "bp_fat", "bp_hulk", "bp_thin", "bp_child", "bp_female", "bp_male", "bp_default" };

            string bodyName = pawnBodyType.defName.ToLower();
            var validPaths = gPaths.Where(x => x.ToLower().Contains($"bp_{bodyName}")).ToList();

            if (!validPaths.NullOrEmpty())
            {
                __result = validPaths[pawn.thingIDNumber % validPaths.Count];
            }
            else
            {
                var defaultPath = gPaths.Where(x => x.ToLower().Contains("bp_default")).ToList();
                if (!defaultPath.NullOrEmpty())
                {
                    __result = defaultPath[pawn.thingIDNumber % defaultPath.Count];
                }
            }
        }
    }
}
