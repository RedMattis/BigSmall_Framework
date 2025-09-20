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
    public class ColorOptionList
    {
        public List<(float weight, Color color)> colors = [];

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                float weight = 1f;
                if (node.Attributes?["weight"] != null)
                {
                    if (!float.TryParse(node.Attributes["weight"].Value, out weight))
                    {
                        Log.ErrorOnce($"Failed to parse weight from '{node.Attributes["weight"].Value}' in ColorOptionList on {node}. Defaulting to 1.", 8734566);
                        weight = 1f;
                    }
                }
                if (ParseHelper.ParseColor(node.InnerText) is Color color)
                {
                    colors.Add((weight, color));
                }
                else
                {
                    Log.ErrorOnce($"Failed to parse color from '{node.InnerText}' in ColorOptionList on {node}. Skipping.", 8734567);
                    continue;
                }
            }
        }
    }
}
