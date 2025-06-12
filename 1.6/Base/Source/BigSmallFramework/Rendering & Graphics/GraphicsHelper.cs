using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static BigAndSmall.RenderingLib;

namespace BigAndSmall
{
    public static class GraphicsHelper
    {
        public static Graphic GetBlankMaterial() => GraphicDatabase.Get<Graphic_Multi>("UI/EmptyImage");

        public static Apparel GetApparelFromNode(this PawnRenderNode node)
        {
            if (node is PawnRenderNode_Ultimate prnUltimate)
            {
                return prnUltimate.apparel;
            }
            //else if (node is PawnRenderNode_SimpleSwitches pawnRenderNode_SimpleSwitches)
            //{
            //    return pawnRenderNode_SimpleSwitches.apparel;
            //}
            else if (node is PawnRenderNode_Apparel prnApparel)
            {
                return prnApparel.apparel;
            }
            return null;
        }

        public static Color GetColorFromColourListRange(this List<Color> colorList, float rngValue, float rngValue2)
        {
            // If there is only one color, return it.
            if (colorList.Count == 1)
                return colorList[0];

            // Get two random adjacent colors from the list.
            int index1 = (int)Mathf.Lerp(0, colorList.Count - 2, rngValue);
            int index2 = index1 + 1;

            Color color1 = colorList[index1];
            Color color2 = colorList[index2];

            float interp = rngValue2;
            return color1 * (1 - interp) + color2 * interp;
        }

        public static Graphic TryGetCustomGraphics(PawnRenderNode renderNode, string path, Color colorOne, Color colorTwo, Vector2 drawSize, CustomMaterial data)
        {
            if (data != null)
            {
                return data.GetGraphic(renderNode, path, colorOne, colorTwo, drawSize, data);
            }
            else
            {
                return GetCachableGraphics(path, drawSize, ShaderTypeDefOf.Cutout.Shader, colorOne, colorTwo);
            }
        }

        public static int GetPartsWithHediff(Pawn pawn, int count, BodyPartDef targetPart, HediffDef hediffDef, bool? mirrored = null)
        {
            if (mirrored == true)
            {
                return pawn.health.hediffSet.hediffs.Sum(hediff => hediff.def == hediffDef &&
                    hediff.Part.def == targetPart && hediff.Part.flipGraphic == mirrored ? 1 : 0);
            }
            return pawn.health.hediffSet.hediffs.Sum(hediff => hediff.def == hediffDef && hediff.Part.def == targetPart ? 1 : 0);
        }
           
        public static int GetPartsReplaced(Pawn pawn, int count, BodyPartDef targetPart, bool? mirrored = null)
        {
            if (mirrored == true)
            {
                return pawn.health.hediffSet.hediffs.Sum(hediff => hediff.Part.def == targetPart
                    && hediff is Hediff_AddedPart && hediff.Part.flipGraphic == mirrored ? 1 : 0);
            }
            return pawn.health.hediffSet.hediffs.Sum(hediff => hediff.Part.def == targetPart && hediff is Hediff_AddedPart ? 1 : 0);
        }
    }
    

    public static class RenderingLib
    {
        [Unsaved(false)]
        private readonly static List<KeyValuePair<(Color, Color), Graphic>> graphics = [];
        public static Graphic GetCachableGraphics(string path, Vector2 drawSize, Shader shader, Color colorOne, Color colorTwo, string maskPath=null, Type graphicClass =null)
        {
            shader ??= ShaderTypeDefOf.CutoutComplex.Shader;

            for (int i = 0; i < graphics.Count; i++)
            {
                var grap = graphics[i];
                var grapMult = grap.Value;
                if (grapMult.path == path && colorOne.IndistinguishableFrom(graphics[i].Key.Item1) && colorTwo.IndistinguishableFrom(grap.Key.Item2) && grap.Value.Shader == shader)
                {
                    return graphics[i].Value;
                }
            }
            Graphic graphic;
            if (graphicClass == typeof(Graphic_Single))
            {
                graphic = GraphicDatabase.Get<Graphic_Single>(path, shader, drawSize, colorOne, colorTwo, data: null, maskPath: maskPath);
            }
            else
            {
                graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, drawSize, colorOne, colorTwo, data: null, maskPath: maskPath);
            }

            graphics.Add(new KeyValuePair<(Color, Color), Graphic>((colorOne, colorTwo), graphic));
            return graphic;
        }
    }
}
