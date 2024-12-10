using UnityEngine;
using Verse;

namespace BigAndSmall
{
    internal class UltimateRenderNodeWorker : PawnRenderNodeWorker
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 scaleFor = base.ScaleFor(node, parms);
            if (node is PawnRenderNode_Ultimate ult && ult.scaleSet)
            {
                scaleFor.x *= ult.cachedScale.x;
                scaleFor.z *= ult.cachedScale.y;
            }
            return scaleFor;
        }
    }
}
