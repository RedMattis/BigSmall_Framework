using RimWorld;
using System;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;
using static BigAndSmall.RenderingLib;

namespace BigAndSmall
{
    public class PawnRenderingProps_Ultimate : PawnRenderNodeProperties
    {
        public ShaderTypeDef shader = null;
        protected ConditionalGraphicsSet conditionalGraphics = new();
        protected GraphicSetDef graphicSetDef = null;
        public Vector4 colorMultiplier = new(1, 1, 1, 1);
        public bool invertEastWest = false;
        public bool mirrorNorth = false;
        public bool autoBodyTypePaths = false;
        public bool autoBodyTypeMasks = false;

        public ConditionalGraphicsSet GraphicSet => graphicSetDef != null ? graphicSetDef.conditionalGraphics : conditionalGraphics;
    }

    public class PawnRenderNode_Ultimate : PawnRenderNode
    {
        public Apparel apparel;
        public bool scaleSet = false;
        public Vector2 cachedScale = Vector2.one;
        private readonly bool useHeadMesh;

        readonly string noImage = "BS_Blank";
        PawnRenderingProps_Ultimate UProps => (PawnRenderingProps_Ultimate)props;
        public PawnRenderNode_Ultimate(Pawn pawn, PawnRenderingProps_Ultimate props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }
        public PawnRenderNode_Ultimate(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel) : base(pawn, props, tree)
        {
            this.apparel = apparel;
            useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
            meshSet = MeshSetFor(pawn);
        }
        public PawnRenderNode_Ultimate(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh) : base(pawn, props, tree)
        {
            this.apparel = apparel;
            this.useHeadMesh = useHeadMesh;
            meshSet = MeshSetFor(pawn);
        }

        protected override string TexPathFor(Pawn pawn)
        {
            throw new NotImplementedException($"TexPath is not meant to be used with this RenderNode." +
                $"Use {nameof(UProps.GraphicSet)} ({typeof(ConditionalGraphicsSet)}) instead.");
        }
        
        public override Graphic GraphicFor(Pawn pawn)
        {
            var props = UProps;
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                var graphicSet = props.GraphicSet.GetGraphicsSet(cache);
                var texPath = graphicSet.GetPath(cache, noImage);
                var maskPath = graphicSet.GetMaskPath(cache, null);
                var conditionalProps = graphicSet.props.GetGraphicProperties(cache);

                if (conditionalProps.drawSize != Vector2.one)
                {
                    scaleSet = true;
                    cachedScale = conditionalProps.drawSize;
                }

                if (texPath.NullOrEmpty())
                {
                    Log.WarningOnce($"[BigAndSmall] No texture path for {pawn}. Returning empty image.", GetHashCode());
                    return GraphicDatabase.Get<Graphic_Single>(noImage);
                }
                if (UProps.autoBodyTypeMasks == true)
                {
                    maskPath ??= texPath; // In the unlikely event that the masks have bodytypes but the texPath doesn't.
                    maskPath = GetBodyTypedPath(pawn.story.bodyType, maskPath);
                }
                if (UProps.autoBodyTypePaths == true)
                {
                    texPath = GetBodyTypedPath(pawn.story.bodyType, texPath);
                }
                if (maskPath == texPath)  // Ensure that the default Ludeon logic for masks gets used. (e.g. `path + "_m"`)
                {
                    maskPath = null;
                }

                Color colorOne = graphicSet.ColorA.GetColor(this, Color.white, ColorSetting.clrOneKey);
                Color colorTwo = graphicSet.ColorB.GetColor(this, Color.white, ColorSetting.clrTwoKey);
                Shader shader = props.shader?.Shader ?? ShaderTypeDefOf.CutoutComplex.Shader;
                if (UProps.useSkinShader)
                {
                    Shader skinShader = ShaderUtility.GetSkinShader(pawn);
                    if (skinShader != null)
                    {
                        shader = skinShader;
                    }
                }

                return GetCachableGraphics(texPath, Vector2.one, shader, colorOne, colorTwo, maskPath: maskPath);
            }

            Log.WarningOnce($"No cache found by {this} for {pawn}. Returning empty image.", GetHashCode());
            return GraphicDatabase.Get<Graphic_Single>(noImage);
        }


        public string GetBodyTypedPath(BodyTypeDef bodyType, string basePath)
        {
            if (bodyType == null)
            {
                Log.Error("Attempted to get graphic with undefined body type.");
                bodyType = BodyTypeDefOf.Male;
            }
            if (basePath.NullOrEmpty())
            {
                return basePath;
            }
            return basePath + "_" + bodyType.defName;
        }

        public override Mesh GetMesh(PawnDrawParms parms)
        {
            if (parms.facing.IsHorizontal && UProps.invertEastWest)
            {
                parms.facing = parms.facing.Opposite;
            }
            return base.GetMesh(parms);
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            if (apparel == null)
            {
                return base.MeshSetFor(pawn);
            }
            if (Props.overrideMeshSize.HasValue)
            {
                return MeshPool.GetMeshSetForSize(base.Props.overrideMeshSize.Value.x, base.Props.overrideMeshSize.Value.y);
            }
            if (useHeadMesh)
            {
                return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn);
            }
            return HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn);
        }
    }

    public class PawnRenderNode_UltimateHead : PawnRenderNode_Ultimate
    {
        public PawnRenderNode_UltimateHead(Pawn pawn, PawnRenderingProps_Ultimate props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }
        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            return HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(pawn);
        }
    }
}