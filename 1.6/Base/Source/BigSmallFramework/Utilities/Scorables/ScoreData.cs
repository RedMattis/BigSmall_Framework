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
        public float score = 1f;
        public List<string> thingDef;
        public List<string> fleshDef;
        public List<string> mutantDef;
        public List<string> pawnKindDef;
        public List<string> pawnType;
        public FloatRange? sizeRange;
        public FloatRange? wealthValueRange;
        public List<StatDefRange> statDefRanges = [];

        /// <summary>
        /// If -1, all filters must match. Otherwise, this sets how many filters must match.
        /// </summary>
        public int requiredMatchCount = -1;
        public bool nullOnFail = true;

        /// <summary>
        /// Gets the score for a given object.
        /// </summary>
        /// <returns>Returns null if the match fails. Otherwise returns 0->100% based on match quality.</returns>
        public virtual float? GetScore(object obj)
        {
            bool matchAll = requiredMatchCount == -1;
            bool allMached = true;
            int matchCount = 0;

            MatchObj(obj, ref allMached, ref matchCount);
            if (matchAll && allMached || matchCount >= requiredMatchCount)
            {
                return score;
            }
            else
            {
                return nullOnFail ? null : 0;
            }
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
            if (thingDef != null && thingDef.Count > 0)
            {
                if (!thingDef.Contains(thing.def.defName)) allMached = false;
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
            if (fleshDef != null && fleshDef.Count > 0)
            {
                if (pawn.RaceProps?.FleshType is FleshTypeDef flesh && fleshDef.Contains(flesh.defName)) matchCount++;
                else allMached = false;
            }
            if (mutantDef != null && mutantDef.Count > 0)
            {
                if (pawn.mutant?.Def is MutantDef mutandef && pawn.IsMutant && mutantDef.Contains(mutandef.defName)) matchCount++;
                else allMached = false;
            }
            if (pawnKindDef != null && pawnKindDef.Count > 0)
            {
                if (!pawnKindDef.Contains(pawn.kindDef.defName)) allMached = false;
                else matchCount++;
            }
            if (sizeRange != null)
            {
                if (!sizeRange.Value.Includes(pawn.BodySize)) allMached = false;
                else matchCount++;
            }
            if (pawnType != null && pawnType.Count > 0)
            {
                bool matched = false;
                foreach (var type in pawnType)
                {
                    if (type == "Animal" && pawn.RaceProps.Animal) { matched = true; break; }
                    else if (type == "Humanlike" && pawn.RaceProps.Humanlike) { matched = true; break; }
                    else if (type == "Mechanoid" && pawn.RaceProps.IsMechanoid) { matched = true; break; }
                    else if (type == "ToolUser" && pawn.RaceProps.ToolUser) { matched = true; break; }
                    else if (type == "HumanlikeAnimal" && HumanlikeAnimals.IsHumanlikeAnimal(pawn.def)) { matched = true; break; }
                }
                if (matched) matchCount++;
                else allMached = false;
            }
        }
    }
}
