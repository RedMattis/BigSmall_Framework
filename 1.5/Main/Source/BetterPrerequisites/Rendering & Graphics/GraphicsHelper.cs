using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static BigAndSmall.RenderingLib;

namespace BigAndSmall
{
    public static class GraphicsHelper
    {
        public static Graphic GetBlankMaterial(Pawn pawn) => GraphicDatabase.Get<Graphic_Multi>("UI/EmptyImage", ShaderUtility.GetSkinShader(pawn), Vector2.one, pawn.story.SkinColor);

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

        public static Graphic_Multi TryGetCustomGraphics(Pawn pawn, string path, Color colorOne, Color colorTwo, Vector2 drawSize, CustomMaterial data)
        {
            if (data != null)
            {
                return data.GetGraphic(pawn, path, colorOne, colorTwo, drawSize, data);
            }
            else
            {
                return GetCachableGraphics(path, drawSize, ShaderTypeDefOf.Cutout, colorOne, colorTwo);
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
        private readonly static List<KeyValuePair<(Color, Color), Graphic_Multi>> graphics = [];
        public static Graphic_Multi GetCachableGraphics(string path, Vector2 drawSize, ShaderTypeDef shader, Color colorOne, Color colorTwo)
        {
            shader ??= ShaderTypeDefOf.CutoutComplex;

            for (int i = 0; i < graphics.Count; i++)
            {
                var grap = graphics[i];
                var grapMult = grap.Value;
                if (grapMult.path == path && colorOne.IndistinguishableFrom(graphics[i].Key.Item1) && colorTwo.IndistinguishableFrom(grap.Key.Item2) && grap.Value.Shader == shader.Shader)
                {
                    return graphics[i].Value;
                }
            }

            Graphic_Multi graphic_Multi = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(path, shader.Shader, drawSize, colorOne, colorTwo);//, data:null, maskPath: null);

            //<T>(string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, GraphicData data, string maskPath = null) where T : Graphic, new()
            //Graphic Get(Type graphicClass, string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, string maskPath = null)
            graphics.Add(new KeyValuePair<(Color, Color), Graphic_Multi>((colorOne, colorTwo), graphic_Multi));
            return graphic_Multi;
        }
    }
}
