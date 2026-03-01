using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class VanillaExpanded
    {
        private static bool? _veActive = null;
        private static bool? _veHActive = null;
        /// <summary>
        /// Checks if Vanlla Expanded is loaded.
        /// </summary>
        public static bool VEActive => _veActive ??= ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");
        public static bool VEHighMatesActive => _veHActive ??= ModsConfig.IsActive("vanillaracesexpanded.highmate");

        public static void PatchVanillaExpanded(Harmony harmony)
        {
            if (VEHighMatesActive)
                PatchVEHToils(harmony);
        }

        public static void PatchVEHToils(Harmony harmony)
        {
            MethodBase method = AccessTools.Method("VanillaRacesExpandedHighmate.JobDriver_InitiateLovin:MakeNewToils");
            HarmonyMethod postfix = new(typeof(LovinPatches).GetMethod(nameof(LovinPatches.VEHighmates_Lovin), BindingFlags.Public | BindingFlags.Static));
            harmony.Patch(method, postfix: postfix);
        }
    }
}