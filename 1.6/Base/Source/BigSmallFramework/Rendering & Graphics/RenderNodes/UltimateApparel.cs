using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    // Blame Ludeon for splitting the base class and messing up the constructors. -_-'
    public class PawnRenderNode_UltimateApparel : PawnRenderNode_Apparel, IUltimateRendering
    {
        public PawnRenderNode Base => this;
        public bool ScaleSet { get; set; } = false;
        public Vector2 CachedScale { get; set; } = Vector2.one;
        public ShaderTypeDef ShaderOverride { get; set; } = null;
        PawnRenderingProps_Ultimate UProps => (PawnRenderingProps_Ultimate)props;
        public PawnRenderNode_UltimateApparel(Pawn pawn, PawnRenderingProps_Ultimate props, PawnRenderTree tree)
            : base(pawn, props, tree, null)
        {
            useHeadMesh = props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead;
            meshSet = MeshSetFor(pawn);
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
}
