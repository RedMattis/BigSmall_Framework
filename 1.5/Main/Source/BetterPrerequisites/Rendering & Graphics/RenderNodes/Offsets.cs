using RimWorld;
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

    public class PawnRenderNodeWorker_LazyCentaur : PawnRenderNodeWorker_Body
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            var bodyType = node?.tree?.pawn?.story?.bodyType;
            var facing = parms.facing;
            if (bodyType == null)
            {
                return base.OffsetFor(node, parms, out pivot);
            }

            var result = base.OffsetFor(node, parms, out pivot);
            if (bodyType == BodyTypeDefOf.Male)
            {
                result.x += facing == Rot4.East ? -0.009f : facing == Rot4.West ? 0.009f : 0;
                result.z +=  0.04f;
            }
            else if (bodyType == BodyTypeDefOf.Female)
            {
                result.x += facing == Rot4.East ? 0.01f : facing == Rot4.West ? -0.01f : 0;
                result.z += 0.005f;
            }
            else if (bodyType == BodyTypeDefOf.Hulk)
            {
                result.x += facing == Rot4.East ? -0.08f : facing == Rot4.West ? 0.08f : 0;
                result.z -= 0.08f;
            }
            else if (bodyType == BodyTypeDefOf.Thin)
            {
                result.x += facing == Rot4.East ? -0.2f : facing == Rot4.West ? 0.2f : 0;
                //result.z -= 0.04f;
            }
            else if (bodyType == BodyTypeDefOf.Fat)
            {
                result.x += facing == Rot4.East ? 0.16f : facing == Rot4.West ? -0.16f : 0;
                result.z += 0.12f;
            }
            else if (bodyType == BodyTypeDefOf.Child)
            {
                result.z += 0.1f;
            }
            else if (bodyType == BodyTypeDefOf.Baby)
            {
                result.z += 0.1f;
            }


            //result.z += (0.5f - bodyType.bodyGraphicScale.y*0.5f);
            result.x *= bodyType?.bodyGraphicScale.x ?? 1;
            result.z *= bodyType?.bodyGraphicScale.y ?? 1;

            //
            return result;
        }

        //public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        //{
        //    var result = base.ScaleFor(node, parms);

        //    var pawn = node.tree.pawn;
        //    result.x *= pawn.story.bodyType.bodyGraphicScale.y;
        //    result.z *= pawn.story.bodyType.bodyGraphicScale.y;

        //    return result;
        //}
    }
}