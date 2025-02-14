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
        

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (FALoaded == true)
            {
                return base.CanDrawNow(node, parms);
            }
            else return false;
        }
    }
}
