using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public interface IFilterWithScore
    {
        float? GetScoreForPawn(Pawn pawn);
    }
    public class FilterWithScore : IFilterWithScore
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
            if (split.Contains("ThingDef") && split.Contains(pawn.def.defName))
            {
                return score;
            }
            else if (split.Contains("FleshDef") && pawn.RaceProps?.FleshType is FleshTypeDef flesh && split.Contains(flesh.defName))
            {
                return score;
            }
            else if (split.Contains("MutantDef") && pawn.mutant?.Def is MutantDef mutandef && pawn.IsMutant && split.Contains(mutandef.defName))
            {
                return score;
            }
            return null;
        }
    }

    public class FilterScoreComplex : IFilterWithScore
    {
        public class StatDefRange
        {
            public StatDef statDef;
            public FloatRange range;
            public void LoadDataFromXmlCustom(System.Xml.XmlNode xmlRoot)
            {
                statDef = DefDatabase<StatDef>.GetNamed(xmlRoot.Name);
                var split = xmlRoot.FirstChild.Value.Split('~');
                range = new FloatRange(float.Parse(split[0]), float.Parse(split[1]));
            }
        }
        public string ThingDef;
        public string FleshDef;
        public string MutantDef;
        public FloatRange? sizeRange;
        public FloatRange? wealthValueRange;
        public List<StatDefRange> statDefRanges = [];

        /// <summary>
        /// If -1, all filters must match. Otherwise, this sets how many filters must match.
        /// </summary>
        public int requiredMatchCount = -1;

        public float score = 0f;

        public float? GetScoreForPawn(Pawn pawn)
        {
            bool matchAll = requiredMatchCount == -1;
            bool allMached = true;
            int matchCount = 0;
            if (!string.IsNullOrEmpty(ThingDef))
            {
                if (ThingDef != pawn.def.defName) allMached = false;
                else matchCount++;
            }
            if (!string.IsNullOrEmpty(FleshDef))
            {
               if (pawn.RaceProps?.FleshType is FleshTypeDef flesh && FleshDef == flesh.defName) matchCount++;
               else allMached = false;
            }
            if (!string.IsNullOrEmpty(MutantDef))
            {
                if (pawn.mutant?.Def is MutantDef mutandef && pawn.IsMutant && MutantDef == mutandef.defName) matchCount++;
                else allMached = false;
            }
            if (sizeRange != null)
            {
                if (!sizeRange.Value.Includes(pawn.BodySize)) allMached = false;
                else matchCount++;
            }
            if (wealthValueRange != null)
            {
                if (!wealthValueRange.Value.Includes(pawn.GetStatValue(StatDefOf.MarketValue))) allMached = false;
                else matchCount++;
            }
            foreach (var statDefRange in statDefRanges)
            {
                if (!statDefRange.range.Includes(pawn.GetStatValue(statDefRange.statDef))) allMached = false;
                else matchCount++;
            }
            if (matchAll && allMached || matchCount >= requiredMatchCount)
            {
                return score;
            }
            return null;
        }
    }

    public abstract class FilteredCollectionDef : Def
    {
        public List<FilterWithScore> selectors = [];
        public List<FilterScoreComplex> complexSelectors = [];

        public virtual float? GetScoreForPawn(Pawn pawn)
        {
            List<IFilterWithScore> sel = [.. selectors, .. complexSelectors];
            var scores = sel.Select(filter => filter.GetScoreForPawn(pawn)).Where(filterScore => filterScore != null);
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
