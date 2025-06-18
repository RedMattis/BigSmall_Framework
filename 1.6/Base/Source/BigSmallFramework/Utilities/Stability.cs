using HarmonyLib;
using RimWorld;

namespace BigAndSmall
{
    /// <summary>
    /// Avoid having scale calculations be done while needs are being added since this can cause an exception.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.AddOrRemoveNeedsAsAppropriate))]
    public static class AddOrRemoveNeedsAsAppropriate_Patch
    {
        public static void Prefix()
        {
            BigSmall.performScaleCalculations = false;
        }

        public static void Postfix()
        {
            BigSmall.performScaleCalculations = true;
        }
    }
}
