using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class GenesFromSpecial
    {
        public static List<GeneDef> GetGenesFromAnomalyCreature(Pawn pawn)
        {
            if (GeneStealDef.GetBestGenesOnPawn(pawn) is GeneStealDef geneCollection)
            {
                return geneCollection.genes;
            }
            return [];
        }
    }
}

