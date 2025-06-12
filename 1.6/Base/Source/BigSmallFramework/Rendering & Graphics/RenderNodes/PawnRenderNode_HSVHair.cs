using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class PawnRenderNodeProps_HSVHair : PawnRenderNodeProperties
    {
        public float saturation = 1f;
        public float value = 1f;

        public SimpleCurve valueGradientRemap = null;
        //public float hue = 1f;
    }

    internal class PawnRenderNode_HSVHair(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : PawnRenderNode_Hair(pawn, props, tree)
    {
        public PawnRenderNodeProps_HSVHair HProps => (PawnRenderNodeProps_HSVHair)props;

        public override Graphic GraphicFor(Pawn pawn)
        {
            var result = base.GraphicFor(pawn);
            if (result == null)
            {
                return null;
            }
            var baseColor = ColorFor(pawn);
            Color.RGBToHSV(baseColor, out float hue, out float sat, out float val);
            if (HProps.valueGradientRemap != null)
            {
                val = HProps.valueGradientRemap.Evaluate(val);
            }

            var newColor = Color.HSVToRGB(hue, Mathf.Clamp01(sat * HProps.saturation), Mathf.Clamp01(val*HProps.value));

            return pawn.story.hairDef.GraphicFor(pawn, newColor);
        }
    }
}
