using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;
using static BigAndSmall.RenderingLib;
using static System.Net.Mime.MediaTypeNames;

namespace BigAndSmall
{
    public class PawnRenderingProps_Ultimate : PawnRenderNodeProperties
    {
        public ShaderTypeDef shader = null;
        protected ConditionalGraphicsSet conditionalGraphics = null;
        protected GraphicSetDef graphicSetDef = null;
        public Vector4 colorMultiplier = new(1, 1, 1, 1);
        public bool invertEastWest = false;
        public bool mirrorNorth = false;
        public bool autoBodyTypePaths = false;
        public bool autoBodyTypeMasks = false;
        public bool useHeadMesh = false;

        // Never set this from XML.
        public ConditionalGraphicsSet generated = null;

        public ConditionalGraphicsSet GraphicSet => graphicSetDef != null ? graphicSetDef.conditionalGraphics : conditionalGraphics;
    }
    
    public class PawnRenderNode_Ultimate : PawnRenderNode, IUltimateRendering
    {
        protected readonly bool useHeadMesh;
        

        public virtual bool AllowTexPathFor => false;
        public PawnRenderNode Base => this;
        public bool ScaleSet { get; set; } = false;
        public Vector2 CachedScale { get; set; } = Vector2.one;
        public ShaderTypeDef ShaderOverride { get; set; } = null;
        
        
        PawnRenderingProps_Ultimate UProps => (PawnRenderingProps_Ultimate)props;
        public PawnRenderNode_Ultimate(Pawn pawn, PawnRenderingProps_Ultimate props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }
        public PawnRenderNode_Ultimate(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel) : base(pawn, props, tree)
        {
            base.apparel = apparel;
            useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
            meshSet = MeshSetFor(pawn);
        }
        public PawnRenderNode_Ultimate(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh) : base(pawn, props, tree)
        {
            base.apparel = apparel;
            this.useHeadMesh = useHeadMesh;
            meshSet = MeshSetFor(pawn);
        }

        public override string TexPathFor(Pawn pawn) => AllowTexPathFor == true ? base.TexPathFor(pawn) :
            throw new NotImplementedException($"TexPath is not meant to be used with this RenderNode." +
                $"Use {nameof(UProps.GraphicSet)} ({typeof(ConditionalGraphicsSet)}) instead.");

        public override Graphic GraphicFor(Pawn pawn) => PRN_Ultimate.GraphicFor(pawn, this, UProps);

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
                return MeshPool.GetMeshSetForSize(Props.overrideMeshSize.Value.x, Props.overrideMeshSize.Value.y);
            }
            if (useHeadMesh || UProps.useHeadMesh)
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