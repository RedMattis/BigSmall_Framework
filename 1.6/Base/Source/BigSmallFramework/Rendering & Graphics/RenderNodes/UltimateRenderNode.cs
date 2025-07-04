﻿using RimWorld;
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
        protected ConditionalGraphicsSet conditionalGraphics = new();
        protected GraphicSetDef graphicSetDef = null;
        public Vector4 colorMultiplier = new(1, 1, 1, 1);
        public bool invertEastWest = false;
        public bool mirrorNorth = false;
        public bool autoBodyTypePaths = false;
        public bool autoBodyTypeMasks = false;

        public ConditionalGraphicsSet GraphicSet => graphicSetDef != null ? graphicSetDef.conditionalGraphics : conditionalGraphics;
    }
    
    public class PawnRenderNode_Ultimate : PawnRenderNode, IUltimateRendering
    {
        public PawnRenderNode Base => this;
        public bool ScaleSet { get; set; } = false;
        public Vector2 CachedScale { get; set; } = Vector2.one;
        private readonly bool useHeadMesh;

        
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

        protected override string TexPathFor(Pawn pawn) =>
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
            if (useHeadMesh)
            {
                return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn);
            }
            return HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn);
        }
    }

    // Blame Ludeon for splitting the base class and messing up the constructors. -_-'
    public class PawnRenderNode_UltimateApparel : PawnRenderNode_Apparel, IUltimateRendering
    {
        public PawnRenderNode Base => this;
        public bool ScaleSet { get; set; } = false;
        public Vector2 CachedScale { get; set; } = Vector2.one;
        PawnRenderingProps_Ultimate UProps => (PawnRenderingProps_Ultimate)props;
        public PawnRenderNode_UltimateApparel(Pawn pawn, PawnRenderingProps_Ultimate props, PawnRenderTree tree)
            : base(pawn, props, tree, null)
        {
            Log.WarningOnce($"[BigAndSmall] THIS SHOULD NOT BE CALLED: {pawn} with props {props.GetType().Name} and tree {tree.GetType().Name}", 231239);
        }
        public PawnRenderNode_UltimateApparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel) : base(pawn, props, tree, apparel)
        {
            base.apparel = apparel;
            useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
            meshSet = MeshSetFor(pawn);
        }
        public PawnRenderNode_UltimateApparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh) : base(pawn, props, tree, apparel, useHeadMesh)
        {
            base.apparel = apparel;
            this.useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
            meshSet = MeshSetFor(pawn);
        }

        protected override string TexPathFor(Pawn pawn) => 
            throw new NotImplementedException($"TexPath is not meant to be used with this RenderNode." +
                $"Use {nameof(UProps.GraphicSet)} ({typeof(ConditionalGraphicsSet)}) instead.");

        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            if (HasGraphic(tree.pawn))
            {
                yield return GraphicFor(pawn);
            }
            else
            {
                foreach (var graphic in base.GraphicsFor(pawn))
                {
                    yield return graphic;
                }
            }
        }

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