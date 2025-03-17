using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class ChanceByStat
    {
        public StatDef statDef;
        public SimpleCurve curve = [];

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            var firstElement = xmlRoot.FirstChild;
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "statDef", firstElement.Name);
            foreach (XmlNode node in firstElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    curve.Add(new CurvePoint(ParseHelper.FromString<Vector2>(node.InnerText)));
                }
            }
        }

        public bool Evaluate(Thing thing, int seed)
        {
            float value = thing.GetStatValue(statDef);
            float chance = curve.Evaluate(value);
            using (new RandBlock(seed))
            {
                if (Rand.Value > chance)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
