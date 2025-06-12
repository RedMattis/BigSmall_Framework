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
