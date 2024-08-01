using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterPrerequisites
{
    public static class ConditionalManager
    {
        /// <summary>
        /// This methods fetches the Gene Extension, which makes it marginally slower than the one which just takes a List of ConditionalStatAffecters.
        /// </summary>
        public static bool TestConditionals(Gene gene)
        {
            if (gene == null || gene.def == null) return false;
            var geneDef = gene.def;
            if (geneDef.HasModExtension<GeneExtension>())
            {
                var geneExtension = geneDef.GetModExtension<GeneExtension>();
                bool invert = geneExtension.invert != null && geneExtension.invert == true;
                if (TestConditionals(gene, geneExtension.conditionals))
                {
                    return true != invert;
                }
                else
                {
                    return false != invert;
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
                        //Log.Message($"DEBUG: Stat {statEffector.Label} does not apply to {gene.pawn.Name}");
                        return false;
                    }
                    //Log.Message($"DEBUG: Stat {statEffector.Label} applies to {gene.pawn.Name}");
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
