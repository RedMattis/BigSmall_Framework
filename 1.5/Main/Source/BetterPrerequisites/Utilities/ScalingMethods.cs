using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class ScalingMethods
    {
        public static bool CheckForSizeAffliction(Pawn pawn)
        {
            HediffSet hediffSet = pawn.health.hediffSet;
            if (hediffSet != null)
            {
                foreach (Hediff hediff in hediffSet.hediffs)
                {
                    if (hediff.def.defName.StartsWith("BS_Affliction"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
