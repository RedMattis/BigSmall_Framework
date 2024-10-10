using BigAndSmall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    // EDIT: This is redundant as heck now since the regular accessing does the same checks.
    // I'll clean it up... later.
    //
    // --------------------------
    // This class is for fast access to the cache. The entire thing really should move over to this instead of the utter mess it is now.
    public static class FastAcccess
    {
        public static BSCache GetCache(Pawn pawn, bool force=false, bool scheduleForce=false)
        {
            return HumanoidPawnScaler.GetCache(pawn, forceRefresh: force, scheduleForce: 1);
        }

        public static bool IsUndead(this Pawn pawn)
        {
            var cache = GetCache(pawn);
            if (cache != null)
            {
                if (cache.isUnliving)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
