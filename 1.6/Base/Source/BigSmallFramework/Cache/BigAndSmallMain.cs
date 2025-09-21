
using System.Linq;
using Verse;
using HarmonyLib;
using System.Threading;

namespace BigAndSmall
{

    [StaticConstructorOnStartup]
    public static class BigSmall
    {
        public static Thread mainThread = Thread.CurrentThread;
        public static bool performScaleCalculations = true;

        private static bool? _BSGenesActive = null;
        private static bool? _BSOptionalActive = null;
        private static bool? _BSTransformGenes = null;
        private static bool? _BSTestModActive = null;
        private static bool? _BSSapientAnimalsActive_ForcedByMods = null;
        private static bool? _BSSapientAnimalsActive = null;
        private static bool? _BSSapientMechanoidsActive = null;
        private static bool? _BSAutoCombatFeatureCache = null;

        public static bool BSTestModActive =>
            _BSTestModActive ??= ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "RedMattis.TestMod");

        public static bool BSOptionalActive =>
            _BSOptionalActive ??= ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "RedMattis.Optional");

        public static bool BSTransformGenes =>
            _BSTransformGenes ??= BSGenesActive || BSOptionalActive ||
            ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "RedMattis.TransformGenes");

        public static bool BSSapientAnimalsActive_ForcedByMods => _BSSapientAnimalsActive_ForcedByMods ??= ModsConfig.ActiveModsInLoadOrder.Any(x =>
                x.PackageIdPlayerFacing == "RedMattis.SapientAnimals" ||
                x.PackageIdPlayerFacing == "RedMattis.MadApril2025") || BSTestModActive;

        public static bool BSSapientAnimalsActive => _BSSapientAnimalsActive ??=
            BSSapientAnimalsActive_ForcedByMods || GlobalSettings.IsFeatureEnabled("SapientAnimals") || BigSmallMod.settings.sapientAnimals;

        public static bool BSSapientMechanoidsActive => _BSSapientMechanoidsActive ??= GlobalSettings.IsFeatureEnabled("SapientMechanoids")
            || BigSmallMod.settings.sapientMechanoids;

        public static bool RobotsEnabled => GlobalSettings.IsFeatureEnabled("Robots") || BSSapientMechanoidsActive;

        public static bool BSGenesActive =>
            _BSGenesActive ??= ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "RedMattis.BigSmall.Core");

        public static bool DisableAllExtraWidgets => BigSmallMod.settings.disableExtraWidgets;

        public static bool ShowPalette => !DisableAllExtraWidgets &&
            (BSGenesActive || BigSmallMod.settings.showClrPaletteBtn || GlobalSettings.IsFeatureEnabled("RecolorButton"));

        public static bool ShowRaceButton => !DisableAllExtraWidgets &&
            (BSGenesActive || BigSmallMod.settings.showRaceBtn || GlobalSettings.IsFeatureEnabled("RaceButton"));

        private static bool IsAutoCombatEnabledCached => _BSAutoCombatFeatureCache ??= GlobalSettings.IsFeatureEnabled("AutoCombat");

        public static bool IsAutoCombatEnabled => BigSmallMod.settings.enableDraftedJobs || (IsAutoCombatEnabledCached && !DisableAllExtraWidgets);


        static BigSmall() { }
    }

    [HarmonyPatch]
    public static class NotifyEvents
    {
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPrefix]
        public static void PawnKillPrefix(Pawn __instance)
        {
            // Go over all hediffs
            foreach(var hediff in __instance.health.hediffSet.hediffs)
            {
                // Remove pilots from pawns if possible.
                if(hediff is Piloted piloted)
                {
                    piloted.RemovePilots(mayRemoveHediff: false);
                }
            }
        }
    }


}
