using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterPrerequisites
{
    public static class GeneHelpers
    {
        public static List<Gene> GetActiveGeneByName(Pawn pawn, string geneName)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].Active && genes[i].def.defName == geneName)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static List<Gene> GetGeneByName(Pawn pawn, string geneName)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].def.defName == geneName)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static List<Gene> GetAllActiveGenes(Pawn pawn)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].Active)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static List<Gene> GetAllInactiveGenes(Pawn pawn)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                if (!genes[i].Active)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static Hediff GetFirstHediffOfDefName(this HediffSet instance, string defName, bool mustBeVisible = false)
        {
            for (int i = 0; i < instance.hediffs.Count; i++)
            {
                if (instance.hediffs[i].def.defName == defName && (!mustBeVisible || instance.hediffs[i].Visible))
                {
                    return instance.hediffs[i];
                }
            }
            
            return null;
        }
    }
}
