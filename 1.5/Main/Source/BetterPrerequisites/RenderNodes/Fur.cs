using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class PawnRenderNode_FurSkinClr : PawnRenderNode_Fur
    {
        public PawnRenderNode_FurSkinClr(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Color ColorFor(Pawn pawn)
        {
            return pawn.story.SkinColor;
        }
    }
}