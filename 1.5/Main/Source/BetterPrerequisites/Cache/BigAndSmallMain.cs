
using System.Linq;

using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Threading;
using BetterPrerequisites;

namespace BigAndSmall
{

    [StaticConstructorOnStartup]
    public static class BigSmall
    {
        public static Thread mainThread = null;
        public static bool performScaleCalculations = true;

        private static bool? _BSGenesActive = null;
        public static bool BSGenesActive
        {
            get
            {
                // This is the prefered way to check, because otherwise we might fail simply because the mod hasn't loaded yet.
                _BSGenesActive ??= ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "RedMattis.BigSmall.Core");
                return _BSGenesActive.Value;
            }
        }

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
