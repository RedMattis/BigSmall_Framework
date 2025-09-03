using RimWorld;
using UnityEngine;
using Verse;
using static BigAndSmall.RenderingLib;

namespace BigAndSmall
{
    public class PawnComplexRenderingProps : PawnRenderNode_SimpleSwitchesProps
    {
        public ShaderTypeDef shader = null;
        public ColorSetting colorA = new();
        public ColorSetting colorB = new();
        public ColorSetting colorC = new();
        public Vector4 colorMultiplier = new(1, 1, 1, 1);

        /// <summary>
        /// Hacky but this avoid us making a seperate class for what is basically just changing the texture path.
        /// </summary>
        public bool isFurskin = false;
    }

    public class PawnRenderNode_Complex : PawnRenderNode_SimpleSwitches
    {
        PawnComplexRenderingProps ComplexProps => (PawnComplexRenderingProps)props;
        public PawnRenderNode_Complex(Pawn pawn, PawnComplexRenderingProps props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override string TexPathFor(Pawn pawn)
        {
            if (ComplexProps.isFurskin && pawn.story?.furDef.GetFurBodyGraphicPath(pawn) is string furPath)
            {
                return furPath;
            }
            else return base.TexPathFor(pawn);
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            var props = ComplexProps;
            if (props.isFurskin && pawn.story?.furDef == null)
            {
                Log.WarningOnce($"[BigAndSmall] {pawn} requested furDef, but it was null", pawn.thingIDNumber ^ 0x3F2A1B);
            }

            string text = TexPathFor(pawn);
            if (text.NullOrEmpty())
            {
                Log.Warning($"[BigAndSmall] No texture path for {pawn}");
                return null;
            }
            Color colorOne = props.colorA.GetColor(this, Color.white, ColorSetting.clrOneKey);
            Color colorTwo = props.colorB.GetColor(this, Color.white, ColorSetting.clrTwoKey);
            Color colorThree = props.colorC.GetColor(this, Color.white, ColorSetting.clrThreeKey);
            Shader shader = props.shader?.Shader ?? ShaderTypeDefOf.CutoutComplex.Shader;

            var result = GetCachableGraphics(text, Vector2.one, shader, colorOne, colorTwo, colorThree);
            return result;
        }
    }
}