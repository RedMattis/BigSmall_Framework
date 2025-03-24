using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class FilterWithScore 
    {
        public string keyTag = "";
        public float score = 1f;

        public void LoadDataFromXmlCustom(System.Xml.XmlNode xmlRoot)
        {
            keyTag = xmlRoot.Name;
            score = float.Parse(xmlRoot.FirstChild.Value);
        }

        public float? GetScoreForPawn(Pawn pawn)
        {
            var split = keyTag.Split('_');
            if (split.Contains("ThingDef"))
            {
                if (split.Contains(pawn.def.defName))
                {
                    return score;
                }
            }
            else if (split.Contains("FleshDef") && pawn.RaceProps?.FleshType is FleshTypeDef flesh)
            {
                if (split.Contains(flesh.defName))
                {
                    return score;
                }
            }
            else if (split.Contains("MutantDef") && pawn.mutant?.Def is MutantDef mutandef)
            {
                if (pawn.IsMutant && split.Contains(mutandef.defName))
                {
                    return score;
                }
            }
            return null;
        }
    }

    public abstract class FilteredCollectionDef : Def
    {
        public List<FilterWithScore> selectors = [];

        public float? GetScoreForPawn(Pawn pawn)
        {
            var scores = selectors.Select(filter => filter.GetScoreForPawn(pawn)).Where(filterScore => filterScore != null);
            return scores.Any() ? scores.Max() : null;
        }

        public static FilteredCollectionDef GetBestScoredDefForPawn<T>(Pawn pawn) where T : FilteredCollectionDef
        {
            var bestScore = float.MinValue;
            T bestDef = null;
            var defs = DefDatabase<T>.AllDefsListForReading;
            foreach (var def in defs)
            {
                
                var score = def.GetScoreForPawn(pawn);
                if (score != null && score > bestScore)
                {
                    bestScore = score.Value;
                    bestDef = def;
                }
            }
            return bestDef;
        }
    }

    public class GeneStealDef : FilteredCollectionDef
    {
        public List<GeneDef> genes = [];
        public static FilteredCollectionDef GetBestScoredDefForPawn(Pawn pawn) => GetBestScoredDefForPawn<GeneStealDef>(pawn);
    }
}
