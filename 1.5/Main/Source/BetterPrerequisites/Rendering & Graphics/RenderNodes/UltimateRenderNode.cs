using RimWorld;
using System;
using UnityEngine;
using Verse;
using static BigAndSmall.RenderingLib;

namespace BigAndSmall
{
    public class PawnRenderingProps_Ultimate : PawnRenderNodeProperties
    {
        public ShaderTypeDef shader = null;
        public ConditionalGraphicsSet conditionalGraphics = new();
        //public ColorSetting colorA = new();
        //public ColorSetting colorB = new();
        //public PathSetting paths = new();
        public Vector4 colorMultiplier = new(1, 1, 1, 1);
        public bool invertEastWest = false;
        public bool mirrorNorth = false;
    }

    public class PawnRenderNode_Ultimate : PawnRenderNode
    {
        readonly string noImage = "BS_Blank";
        PawnRenderingProps_Ultimate UProps => (PawnRenderingProps_Ultimate)props;
        public PawnRenderNode_Ultimate(Pawn pawn, PawnRenderingProps_Ultimate props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        protected override string TexPathFor(Pawn pawn)
        {
            throw new NotImplementedException($"TexPath is not meant to be used with this RenderNode." +
                $"Use {nameof(UProps.conditionalGraphics)} ({typeof(ConditionalGraphicsSet)}) instead.");
        }
        
        public override Graphic GraphicFor(Pawn pawn)
        {
            var props = UProps;
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                var graphicSet = props.conditionalGraphics.GetGraphicsSet(cache);
                var texPath = graphicSet.GetPath(cache, noImage);
                if (texPath.NullOrEmpty())
                {
                    Log.WarningOnce($"[BigAndSmall] No texture path for {pawn}. Returning empty image.", GetHashCode());
                    return GraphicDatabase.Get<Graphic_Single>(noImage);
                }
                Color colorOne = graphicSet.colorA.GetColor(pawn, Color.white, ColorSetting.clrOneKey);
                Color colorTwo = graphicSet.colorB.GetColor(pawn, Color.white, ColorSetting.clrTwoKey);
                ShaderTypeDef shader = props.shader ?? ShaderTypeDefOf.CutoutComplex;

                

                return GetCachableGraphics(texPath, Vector2.one, shader, colorOne, colorTwo);
            }

            Log.WarningOnce($"No cache found by {this} for {pawn}. Returning empty image.", GetHashCode());
            return GraphicDatabase.Get<Graphic_Single>(noImage);
        }

        public override Mesh GetMesh(PawnDrawParms parms)
        {
            if (parms.facing.IsHorizontal && UProps.invertEastWest)
            {
                parms.facing = parms.facing.Opposite;
            }
            return base.GetMesh(parms);
        }
    }
}