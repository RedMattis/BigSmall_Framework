
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
        public static bool loggingV = false;
        public static bool logging = false;
        public static bool debug = false;
        public const int debugTickDivider = 5000;

        public Main(ModContentPack content) : base(content)
        {
            instance = this;
            ApplyHarmonyPatches();

            settings ??= GetSettings<RedSettings>();
        }

        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.Health");
            harmony.PatchAll();
        }
    }

}
