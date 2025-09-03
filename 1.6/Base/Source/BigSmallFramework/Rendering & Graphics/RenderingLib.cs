using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class RenderingLib
    {
        // Same deal as Ludeon's method, but it checks each channel.
        public static bool IndistinguishableFromExact(this Color colA, Color colB)
        {
            if (GenColor.Colors32Equal(colA, colB))
            {
                return true;
            }
            Color color = colA - colB;
            return Mathf.Abs(color.r) < 0.005f &&  Mathf.Abs(color.g) < 0.005f  && Mathf.Abs(color.b) < 0.005f && Mathf.Abs(color.a) < 0.005f;
        }


        [Unsaved(false)]
        private readonly static List<KeyValuePair<(Color, Color, Color), Graphic>> graphics = [];
        public static Graphic GetCachableGraphics(string path, Vector2 drawSize, Shader shader, Color colorOne, Color colorTwo, Color colorThree, string maskPath=null, Type graphicClass =null)
        {
            shader ??= BSDefs.BS_CutoutThreeColor.Shader;

            for (int i = 0; i < graphics.Count; i++)
            {
                var grap = graphics[i];
                var grapMult = grap.Value;
                if (grapMult.path == path
                    && grapMult.maskPath == maskPath
                    && colorOne.IndistinguishableFromExact(grap.Key.Item1)
                    && colorTwo.IndistinguishableFromExact(grap.Key.Item2)
                    && colorThree.IndistinguishableFromExact(grap.Key.Item3)
                    && grap.Value.Shader == shader)
                {
                    return graphics[i].Value;
                }
            }
            Graphic graphic;
            if (graphicClass == typeof(Graphic_Single))
            {
                if (BSDefs.IsBSShader(shader))
                {
                    graphic = MultiColorUtils.GetGraphic<Graphic_Single>(path, shader, drawSize, colorOne, colorTwo, colorThree, data: null, maskPath: maskPath);
                }
                else
                {
                    graphic = GraphicDatabase.Get<Graphic_Single>(path, shader, drawSize, colorOne, colorTwo, data: null, maskPath: maskPath);
                }
            }
            else
            {
                if (BSDefs.IsBSShader(shader))
                {
                    graphic = MultiColorUtils.GetGraphic<Graphic_Multi>(path, shader, drawSize, colorOne, colorTwo, colorThree, data: null, maskPath: maskPath);
                }
                else
                {
                    graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, drawSize, colorOne, colorTwo, data: null, maskPath: maskPath);
                }
            }

            graphics.Add(new KeyValuePair<(Color, Color, Color), Graphic>((colorOne, colorTwo, colorThree), graphic));
            return graphic;
        }
    }
}
