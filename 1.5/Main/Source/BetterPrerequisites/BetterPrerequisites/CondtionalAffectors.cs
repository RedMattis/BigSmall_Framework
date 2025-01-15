using BigAndSmall;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BetterPrerequisites
{
    public static class ConditionalManager
    {
        /// <summary>
        /// This methods fetches the Gene Extension, which makes it marginally slower than the one which just takes a List of ConditionalStatAffecters.
        /// </summary>
        public static bool TestConditionals(Gene gene, List<PawnExtension> pawnExtensions)
        {
            if (gene == null || gene.def == null) return false;
            if (pawnExtensions.NullOrEmpty()) return true;

            var geneDef = gene.def;
            foreach (var pExt in pawnExtensions.Where(x=>x.conditionals != null))
            {
                bool invert = pExt.invert != null && pExt.invert == true;
                if (TestConditionals(gene, pExt.conditionals))
                {
                    if (invert) return false;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static bool TestConditionals(Gene gene, List<ConditionalStatAffecter> conditionalStatEffectors)
        {
            if (conditionalStatEffectors != null)
                foreach (var statEffector in conditionalStatEffectors)
                {
                    var stat = StatRequest.For(gene.pawn);
                    if (!statEffector.Applies(stat))
                    {
                        return false;
                    }
                }
            return true;
        }
        public static bool TestConditionals(Pawn pawn, List<ConditionalStatAffecter> conditionalStatEffectors)
        {
            if (conditionalStatEffectors != null)
                foreach (var statEffector in conditionalStatEffectors)
                {
                    var stat = StatRequest.For(pawn);
                    if (!statEffector.Applies(stat))
                    {
                        return false;
                    }
                }
            return true;
        }
    }

}
