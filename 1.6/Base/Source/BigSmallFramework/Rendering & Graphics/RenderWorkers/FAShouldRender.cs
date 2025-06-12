using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static BigAndSmall.NalsToggles;

namespace BigAndSmall
{
    public class PawnRenderNodeWorker_FAOnly : PawnRenderNodeWorker_FlipWhenCrawling
    {
        protected bool initialized = false;
        protected bool shouldDraw = false;
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!initialized)
            {
                shouldDraw = FALoaded;
                if (HumanoidPawnScaler.GetCache(node.tree.pawn) is BSCache cache && cache.facialAnimationDisabled)
                {
                    shouldDraw = false;
                }
            }

            if (shouldDraw)
            {
                return base.CanDrawNow(node, parms);
            }
            else return false;
        }
    }
}
