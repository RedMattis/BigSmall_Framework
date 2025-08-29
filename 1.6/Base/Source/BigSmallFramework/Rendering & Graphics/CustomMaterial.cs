﻿using static BigAndSmall.RenderingLib;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class CustomMaterial
    {
        public ShaderTypeDef shader = null;
        public ColorSetting colorA = new();
        public ColorSetting colorB = new();
        public ColorSetting colorC = new();
        public bool overrideDesiccated = false;

        //public Graphic_Multi GetGraphic(Pawn pawn, Graphic oldResult, CustomMaterial data)
        //{
        //    Color colorOne = oldResult.color;
        //    Color colorTwo = oldResult.colorTwo;

        //    colorOne = data.colorA.GetColor(pawn, colorOne, ColorSetting.clrOneKey);
        //    colorTwo = data.colorB.GetColor(pawn, colorTwo, ColorSetting.clrTwoKey);
        //    Graphic_Multi graphic_Multi = GetCachableGraphics(oldResult.path, oldResult.drawSize, data.shader, colorOne, colorTwo);
        //    return graphic_Multi;
        //}

        public Graphic GetGraphic(PawnRenderNode pawnRenderNode, string path, Color colorOne, Color colorTwo, Color colorThree, Vector2 drawSize)
        {
            colorOne = colorA.GetColor(pawnRenderNode, colorOne, ColorSetting.clrOneKey);
            colorTwo = colorB.GetColor(pawnRenderNode, colorTwo, ColorSetting.clrTwoKey);
            colorThree = colorC.GetColor(pawnRenderNode, colorThree, ColorSetting.clrThreeKey);
            Graphic graphic = GetCachableGraphics(path, drawSize, shader.Shader, colorOne, colorTwo, colorThree);
            return graphic;
        }
    }
}
