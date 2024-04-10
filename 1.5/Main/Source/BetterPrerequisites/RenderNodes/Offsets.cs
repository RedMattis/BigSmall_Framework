using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class PawnRenderNodeWorker_BodyTypeOffsets : PawnRenderNodeWorker_Body
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            var pawn = node.tree.pawn;
            var result = base.OffsetFor(node, parms, out pivot);
            result.x *= pawn?.story?.bodyType?.bodyGraphicScale.x ?? 1;
            result.z *= pawn?.story?.bodyType?.bodyGraphicScale.y ?? 1;
            return result;
        }
    }
}