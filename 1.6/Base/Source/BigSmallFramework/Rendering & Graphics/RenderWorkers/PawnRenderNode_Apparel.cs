using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
namespace BigAndSmall
{ 

    public class URenderWorker_Apparel : PawnRenderNodeWorker_Body
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
            {
                return false;
            }
            if (!parms.flags.FlagSet(PawnRenderFlags.Clothes))
            {
                return false;
            }
            return true;
        }

        public override Vector3 OffsetFor(PawnRenderNode n, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(n, parms, out pivot);
            PawnRenderNode_Ultimate pawnRenderNode_Apparel = (PawnRenderNode_Ultimate)n;
            if (pawnRenderNode_Apparel.apparel.def.apparel.wornGraphicData != null && pawnRenderNode_Apparel.apparel.RenderAsPack())
            {
                Vector2 vector = pawnRenderNode_Apparel.apparel.def.apparel.wornGraphicData.BeltOffsetAt(parms.facing, parms.pawn.story.bodyType);
                result.x += vector.x;
                result.z += vector.y;
            }
            return result;
        }

        public override Vector3 ScaleFor(PawnRenderNode n, PawnDrawParms parms)
        {
            Vector3 result = base.ScaleFor(n, parms);
            PawnRenderNode_Ultimate pawnRenderNode_Apparel = (PawnRenderNode_Ultimate)n;
            if (pawnRenderNode_Apparel.apparel.def.apparel.wornGraphicData != null && pawnRenderNode_Apparel.apparel.RenderAsPack())
            {
                Vector2 vector = pawnRenderNode_Apparel.apparel.def.apparel.wornGraphicData.BeltScaleAt(parms.facing, parms.pawn.story.bodyType);
                result.x *= vector.x;
                result.z *= vector.y;
            }
            return result;
        }

        public override float LayerFor(PawnRenderNode n, PawnDrawParms parms)
        {
            if (parms.flipHead && n.Props.oppositeFacingLayerWhenFlipped)
            {
                PawnDrawParms parms2 = parms;
                parms2.facing = parms.facing.Opposite;
                parms2.flipHead = false;
                return base.LayerFor(n, parms2);
            }
            return base.LayerFor(n, parms);
        }
    }
}
