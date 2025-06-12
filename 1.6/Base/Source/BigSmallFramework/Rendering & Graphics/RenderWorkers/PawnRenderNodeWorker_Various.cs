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
    public class URenderWorker_FlipWhenCrawling : PawnRenderNodeWorker_FlipWhenCrawling
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

    public class URenderWorker_AverageEyes : URenderWorker_FlipWhenCrawling
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            List<Vector3> anchorAverage = [];
            if (TryGetWoundAnchor("RightEye", parms, out var anchorR))
            {
                PawnDrawUtility.CalcAnchorData(parms.pawn, anchorR, parms.facing, out var anchorOffset, out var _);
                anchorAverage.Add(anchorOffset);
            }
            if (TryGetWoundAnchor("LeftEye", parms, out var anchorL))
            {
                PawnDrawUtility.CalcAnchorData(parms.pawn, anchorL, parms.facing, out var anchorOffset, out var _);
                anchorAverage.Add(anchorOffset);
            }
            if (anchorAverage.Count > 0)
            {
                result += anchorAverage.Aggregate((acc, x) => acc + x) / anchorAverage.Count;
            }

            return result;
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.ScaleFor(node, parms) * parms.pawn.ageTracker.CurLifeStage.eyeSizeFactor.GetValueOrDefault(1f);
        }

        protected bool TryGetWoundAnchor(string anchorTag, PawnDrawParms parms, out BodyTypeDef.WoundAnchor anchor)
        {
            anchor = null;
            if (anchorTag.NullOrEmpty())
            {
                return false;
            }
            List<BodyTypeDef.WoundAnchor> woundAnchors = parms.pawn.story.bodyType.woundAnchors;
            for (int i = 0; i < woundAnchors.Count; i++)
            {
                BodyTypeDef.WoundAnchor woundAnchor = woundAnchors[i];
                if (woundAnchor.tag == anchorTag)
                {
                    Rot4? rotation = woundAnchor.rotation;
                    Rot4 facing = parms.facing;
                    if (rotation.HasValue && (!rotation.HasValue || rotation.GetValueOrDefault() == facing) && (parms.facing == Rot4.South || woundAnchor.narrowCrown.GetValueOrDefault() == parms.pawn.story.headType.narrow))
                    {
                        anchor = woundAnchor;
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class URenderWorker_Mouth : URenderWorker_FlipWhenCrawling
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 scaleFor = base.ScaleFor(node, parms);
            var head = parms.pawn.story.headType;
            if (parms.facing == Rot4.East || parms.facing == Rot4.West)
            {
                scaleFor.x *= (head.beardMeshSize.x / 1.5f);
                scaleFor.z *= (head.beardMeshSize.y / 1.5f);
            }
            return scaleFor;
        }
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            var head = parms.pawn.story.headType;
            if (parms.facing == Rot4.East)
            {
                result += head.beardOffset/2;
            }
            else if (parms.facing == Rot4.West)
            {
                result += head.beardOffset/2;
            }
            return result;
        }
    }

    public class URenderWorker_Beard : URenderWorker_FlipWhenCrawling
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 scaleFor = base.ScaleFor(node, parms);
            var head = parms.pawn.story.headType;
            if (parms.facing == Rot4.East || parms.facing == Rot4.West)
            {
                scaleFor.x *= (head.beardMeshSize.x / 1.5f);
                scaleFor.z *= (head.beardMeshSize.y / 1.5f);
            }
            return scaleFor;
        }
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            var head = parms.pawn.story.headType;
            if (parms.facing == Rot4.East)
            {
                result.x += head.beardOffsetXEast;
            }
            else if (parms.facing == Rot4.West)
            {
                result.x -= head.beardOffsetXEast;
            }
            result += head.beardOffset;
            return result;
        }
    }

    public class URenderWorker_Hair : URenderWorker_FlipWhenCrawling
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 scaleFor = base.ScaleFor(node, parms);
            var head = parms.pawn.story.headType;
            if (parms.facing == Rot4.East || parms.facing == Rot4.West)
            {
                scaleFor.x *= (head.hairMeshSize.x / 1.5f);
            }
            scaleFor.z *= (head.hairMeshSize.y / 1.5f);
            return scaleFor;
        }
    }
}
