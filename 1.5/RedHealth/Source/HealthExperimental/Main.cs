
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse.Noise;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using RimWorld.QuestGen;
using System.Text;
using System.Runtime;


namespace RedHealth
{
    [StaticConstructorOnStartup]
    public partial class Main : Mod
    {
        public static Main instance = null;
        public const bool loggingV = false;
        public const bool logging = false;
        public static bool debug = false;
        //public const int debugTickDivider = 5000;

        public static bool StandaloneModActive = false;

        public static bool DoCheapLogging => logging || settings?.logging == true;
        public Main(ModContentPack content) : base(content)
        {
            instance = this;
            if (ModsConfig.IsActive("RedMattis.RedHealth"))
            {
                StandaloneModActive = true;
            }
            settings ??= GetSettings<RedSettings>();
            ApplyHarmonyPatches();

            if (settings.devEventTimeAcceleration > 3f)
            {
                Log.Warning($"RedHealth: Event Acceleration is set to x{settings.devEventTimeAcceleration}.\n" +
                    $"This may increase health event frequency far more than designed for.");
            }
        }

        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.Health");
            harmony.PatchAll();
        }
    }

}
