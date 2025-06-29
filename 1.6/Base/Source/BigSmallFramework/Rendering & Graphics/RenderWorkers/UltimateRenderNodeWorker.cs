using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class UltimateRenderNodeWorker : PawnRenderNodeWorker
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 scaleFor = base.ScaleFor(node, parms);
            if (node is PawnRenderNode_Ultimate ult && ult.ScaleSet)
            {
                scaleFor.x *= ult.CachedScale.x;
                scaleFor.z *= ult.CachedScale.y;
            }
            return scaleFor;
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            if (parms.pawn.story.headType.narrow && node.Props.narrowCrownHorizontalOffset != 0f && parms.facing.IsHorizontal)
            {
                if (parms.facing == Rot4.East)
                {
                    result.x -= node.Props.narrowCrownHorizontalOffset;
                }
                else if (parms.facing == Rot4.West)
                {
                    result.x += node.Props.narrowCrownHorizontalOffset;
                }
                result.z -= node.Props.narrowCrownHorizontalOffset;
            }
            return result;
        }
    }

    //public class URNWorker_Eyes : URNWorker_FlipWhenCrawling
    //{
    //    public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
    //    {
    //        Vector3 scaleFor = base.ScaleFor(node, parms);
    //        var head = parms.pawn.story.headType;
    //        if (parms.facing == Rot4.East)
    //        {
    //            scaleFor.x *= (1.5f / head.eyeOffsetEastWest.x);
    //        }
    //        else if (parms.facing == Rot4.West)
    //        {
    //            scaleFor.x *= (1.5f / head.
    //        }
    //        return scaleFor;
    //    }
    //}
}
