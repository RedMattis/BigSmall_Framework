using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
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
            if (node is IUltimateRendering prnUltimate)
            {
                return prnUltimate.Base.apparel;
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

        public static Color GetColorFromColorListRange(this List<Color> colorList, float rngValue, float rngValue2)
        {
            // If there is only one color, return it.
            if (colorList.Count == 1)
            {
                return colorList[0];
            }
            if (colorList.Count == 0)
            {
                Log.WarningOnce("Tried to get color from empty color list. Returning White.", 92345);
                return Color.white;
            }

            // Get two random adjacent colors from the list.
            int index1 = (int)Mathf.Lerp(0, colorList.Count - 2, rngValue);
            int index2 = index1 + 1;

            Color color1 = colorList[index1];
            Color color2 = colorList[index2];

            float interp = rngValue2;
            return color1 * (1 - interp) + color2 * interp;
        }

        public static Color GetColorFromColorListRangeWithWeights(this ColorOptionList colorList, float rngValue)
        {
            var colors = colorList.colors;
            if (colors.Count == 0)
            {
                Log.WarningOnce("Tried to get color from empty color list. Returning White.", 92345);
                return Color.white;
            }
            if (colors.Count == 1)
            {
                return colors[0].color;
            }

            float totalWeight = colors.Sum(c => c.weight);
            float target = rngValue * totalWeight;

            float cumulative = 0f;
            for (int i = 0; i < colors.Count - 1; i++)
            {
                float nextCumulative = cumulative + colors[i].weight;
                if (target <= nextCumulative)
                {
                    float interp = (target - cumulative) / colors[i].weight;
                    return Color.Lerp(colors[i].color, colors[i + 1].color, interp);
                }
                cumulative = nextCumulative;
            }
            return colors.Last().color;
        }




        public static Graphic TryGetCustomGraphics(PawnRenderNode renderNode, string path, Color colorOne, Color colorTwo, Color colorThree, Vector2 drawSize, CustomMaterial data)
        {
            if (data != null)
            {
                return data.GetGraphic(renderNode, path, colorOne, colorTwo, colorThree, drawSize);
            }
            else
            {
                return GetCachableGraphics(path, drawSize, ShaderTypeDefOf.Cutout.Shader, colorOne, colorTwo, colorThree);
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
}
