
using System.Linq;
using Verse;
using HarmonyLib;
using System.Threading;
using BigAndSmall.Settings;

namespace BigAndSmall
{

    [StaticConstructorOnStartup]
    public static class BigSmall
    {
        public static Thread mainThread = null;
        public static bool performScaleCalculations = true;

        private static bool? _BSGenesActive = null;
        private static bool? _BSOptionalActive = null;
        private static bool? _BSTransformGenes = null;
        private static bool? _BSTestModActive = null;
        private static bool? _BSSapientAnimalsActive_ForcedByMods = null;
        private static bool? _BSSapientAnimalsActive = null;
        private static bool? _BSSapientMechanoidsActive = null;
		private static bool? _BSAndroidsActive = null;
		private static bool? _BSGenesPatchesActive = null;
		private static bool? _BSTransformGenesActive = null;
		private static bool? _BSShowRaceButton = null;
		private static bool? _BSShowPalette = null;

		public static bool BSTestModActive =>
			_BSTestModActive ??= ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "RedMattis.TestMod");

		public static bool BSOptionalActive =>
			_BSOptionalActive ??= ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "RedMattis.Optional");

		public static bool BSTransformGenes =>
			_BSTransformGenes ??= BSGenesActive || BSOptionalActive ||
			ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "RedMattis.TransformGenes");

		public static bool BSSapientAnimalsActive => _BSSapientAnimalsActive ??= ModFeatures.IsFeatureEnabled("SapientAnimals");
		public static bool BSSapientMechanoidsActive => _BSSapientMechanoidsActive ??= ModFeatures.IsFeatureEnabled("SapientMechanoids");
		public static bool AndroidsEnabled => _BSAndroidsActive ??= ModFeatures.IsFeatureEnabled("Androids");
		public static bool BSGenesActive => _BSGenesPatchesActive ??= ModFeatures.IsFeatureEnabled("GenePatches");
		public static bool BSTransformGenesActive => _BSTransformGenesActive ??= ModFeatures.IsFeatureEnabled("TransformGenes");
		public static bool ShowRaceButton => _BSShowRaceButton ??= ModFeatures.IsFeatureEnabled("ShowRaceButton");
		public static bool ShowPalette => _BSShowPalette ??= ModFeatures.IsFeatureEnabled("EditPawnAppearance");

		static BigSmall()
        {
            mainThread = Thread.CurrentThread;
        }
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
                    piloted.RemovePilots();
                }
            }
        }
    }


}
