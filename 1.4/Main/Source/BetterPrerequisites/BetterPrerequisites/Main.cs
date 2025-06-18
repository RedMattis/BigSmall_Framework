
using System.Linq;

using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Runtime;
using BigAndSmall;
//using VariedBodySizes;

namespace BetterPrerequisites
{
    /// <summary>
    /// Main class
    /// </summary>
    [StaticConstructorOnStartup]
    internal class BetterPrerequisites : Mod
    {
        public BetterPrerequisites(ModContentPack content) : base(content)
        {
            ApplyHarmonyPatches();

            
        }

        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.BetterPrerequisites");
            harmony.PatchAll();

        }
    }
}
