using Verse;
using HarmonyLib;

namespace BigAndSmall
{
    /// <summary>
    /// Main class of the "Big and Small Races" mod. This mod intends to add dwarves and ogres using the Rimworld Gene system.
    /// </summary>

    [StaticConstructorOnStartup]
    public static class BigSmallHAR
    {
        /// <summary>
        /// So we can query the BodySize from before our changes. You'll get infinite recursion without this btw. :3
        /// </summary>
        public static bool skipBodySizePostFix = false;

        static BigSmallHAR()
        {
            Log.Message($"Big and Small: HAR detected, using HAR scaling methods where available.");
            ApplyHarmonyPatches();
        }


        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.BigSmall");
            harmony.PatchAll();
        }
    }
}
