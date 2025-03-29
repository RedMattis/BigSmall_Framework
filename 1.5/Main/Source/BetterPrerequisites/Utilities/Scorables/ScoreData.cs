using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    /// <summary>
    /// This is a class which calculates a numeric score for a given object.
    /// </summary>
    public class ScoreData : IScoreProvider
    {
        public class StatDefRange
        {
            public StatDef statDef;
            public FloatRange range;
            public void LoadDataFromXmlCustom(System.Xml.XmlNode xmlRoot)
            {
                string mayRequireMod = xmlRoot.Attributes?["MayRequire"]?.Value;
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(statDef), xmlRoot.Name, mayRequireMod: mayRequireMod);

                var split = xmlRoot.FirstChild.Value.Split('~');
                range = new FloatRange(float.Parse(split[0]), float.Parse(split[1]));
            }
        }
        public float value = 0f;
        public string ThingDef;
        public string FleshDef;
        public string MutantDef;
        public string PawnKindDef;
        public FloatRange? sizeRange;
        public FloatRange? wealthValueRange;
        public List<StatDefRange> statDefRanges = [];

        /// <summary>
        /// If -1, all filters must match. Otherwise, this sets how many filters must match.
        /// </summary>
        public int requiredMatchCount = -1;

        

        public virtual float? GetScore(object obj)
        {
            bool matchAll = requiredMatchCount == -1;
            bool allMached = true;
            int matchCount = 0;

            MatchObj(obj, ref allMached, ref matchCount);
            if (matchAll && allMached || matchCount >= requiredMatchCount)
            {
                return value;
            }
            return null;
        }

        protected virtual void MatchObj(object obj, ref bool allMached, ref int matchCount)
        {
            if (obj is Thing thing)
            {
                MatchThing(thing, ref allMached, ref matchCount);
                
            }
        }

        protected virtual void MatchThing(Thing thing, ref bool allMached, ref int matchCount)
        {
            if (!string.IsNullOrEmpty(ThingDef))
            {
                if (ThingDef != thing.def.defName) allMached = false;
                else matchCount++;
            }
            if (wealthValueRange != null)
            {
                if (!wealthValueRange.Value.Includes(thing.GetStatValue(StatDefOf.MarketValue))) allMached = false;
                else matchCount++;
            }
            foreach (var statDefRange in statDefRanges)
            {
                if (!statDefRange.range.Includes(thing.GetStatValue(statDefRange.statDef))) allMached = false;
                else matchCount++;
            }
            if (thing is Pawn pawn)
            {
                MatchPawn(pawn, ref allMached, ref matchCount);
            }
        }

        protected virtual void MatchPawn(Pawn pawn, ref bool allMached, ref int matchCount)
        {
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
            if (!string.IsNullOrEmpty(PawnKindDef))
            {
                if (PawnKindDef != pawn.kindDef.defName) allMached = false;
                else matchCount++;
            }
            if (sizeRange != null)
            {
                if (!sizeRange.Value.Includes(pawn.BodySize)) allMached = false;
                else matchCount++;
            }
        }
    }
}
