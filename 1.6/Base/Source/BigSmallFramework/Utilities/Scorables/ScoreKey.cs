using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class ScoreKey : IScoreProvider
    {
        public string keyTag = "";
        public float value = 1f;

        public void LoadDataFromXmlCustom(System.Xml.XmlNode xmlRoot)
        {
            keyTag = xmlRoot.Name;
            value = float.Parse(xmlRoot.FirstChild.Value);
        }

        public virtual float? GetScore(object obj)
        {
            if (obj is Pawn pawn && ScorePawn(pawn) is float score)
            {
                return score;
            }
            return null;
        }

        protected virtual float? ScorePawn(Pawn pawn)
        {
            var split = keyTag.Split('_');
            if (split.Contains("ThingDef") && split.Contains(pawn.def.defName))
            {
                return value;
            }
            else if (split.Contains("FleshDef") && pawn.RaceProps?.FleshType is FleshTypeDef flesh && split.Contains(flesh.defName))
            {
                return value;
            }
            else if (split.Contains("MutantDef") && pawn.mutant?.Def is MutantDef mutandef && pawn.IsMutant && split.Contains(mutandef.defName))
            {
                return value;
            }
            return null;
        }
    }
}
