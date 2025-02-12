using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class PawnRenderNodeWorker_FAOnly : PawnRenderNodeWorker_FlipWhenCrawling
    {
        public static bool? FALoaded = null;

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            FALoaded ??= (ModsConfig.IsActive("Nals.FacialAnimation"));
            if (FALoaded == true)
            {
                return base.CanDrawNow(node, parms);
            }
            else return false;
        }
    }
}
