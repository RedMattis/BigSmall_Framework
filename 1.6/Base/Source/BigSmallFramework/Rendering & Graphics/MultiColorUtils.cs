using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    /// <summary>
    /// From Rimdark40K collab on 3-color shaders.
    /// </summary>
    public static class MultiColorUtils
    {
        public static T GetGraphic<T>(string path, Shader shader, Vector2 drawSize, Color colorOne, Color colorTwo, Color colorThree, GraphicData data, string maskPath = null) where T : Graphic
        {
            var shaderParameter1 = new ShaderParameter();
            var traverse = Traverse.Create(shaderParameter1);
            traverse.Field("name").SetValue("_DrawColor");
            traverse.Field("type").SetValue(1);
            traverse.Field("value").SetValue(new Vector4(colorOne.r, colorOne.g, colorOne.b, colorOne.a));

            var shaderParameter2 = new ShaderParameter();
            traverse = Traverse.Create(shaderParameter2);
            traverse.Field("name").SetValue("_DrawColorTwo");
            traverse.Field("type").SetValue(1);
            traverse.Field("value").SetValue(new Vector4(colorTwo.r, colorTwo.g, colorTwo.b, colorTwo.a));

            var shaderParameter3 = new ShaderParameter();
            traverse = Traverse.Create(shaderParameter3);
            traverse.Field("name").SetValue("_DrawColorThree");
            traverse.Field("type").SetValue(1);
            traverse.Field("value").SetValue(new Vector4(colorThree.r, colorThree.g, colorThree.b, colorThree.a));

            var shaderParameters = new List<ShaderParameter>
            {
                shaderParameter1,
                shaderParameter2,
                shaderParameter3
            };

            return GraphicDatabase.Get(typeof(T), path, shader, drawSize, colorOne, colorTwo, data, shaderParameters, maskPath) as T;
        }
    }
}
