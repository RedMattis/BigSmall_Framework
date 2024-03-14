using Verse;
using HarmonyLib;
using System.Diagnostics.Eventing.Reader;
using System;

namespace BigAndSmall
{
    /// <summary>
    /// Main class of the "Big and Small Races" mod. This mod intends to add dwarves and ogres using the Rimworld Gene system.
    /// </summary>

    [StaticConstructorOnStartup]
    public static partial class BigSmallLegacy
    {
        /// <summary>
        /// So we can query the BodySize from before our changes. You'll get infinite recursion without this btw. :3
        /// </summary>
        public static bool skipBodySizePostFix = false;
        public static bool VEFActive = false;

        //public static bool VFEModFound = false;

        static BigSmallLegacy()
        {
            Log.Message($"Big and Small: HAR not found in active mod-list. Big and Small is taking control of humanoid pawn scaling.");

            VEFActive = ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");

            // Check if the VFE mod is active.
            //VFEModFound = ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");

            ApplyHarmonyPatches();
        }


        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.BigSmall");
            harmony.PatchAll();
            //harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeHeadSetForPawn"), null, null, new HarmonyMethod(typeof(BigSmallLegacy), "GetHumanlikeHeadSetForPawnTranspiler"));
        }
    }
}
