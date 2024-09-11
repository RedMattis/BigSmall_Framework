
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
using System.Diagnostics.Eventing.Reader;
using RimWorld.BaseGen;
//using VariedBodySizes;

namespace BetterPrerequisites
{
    /// <summary>
    /// Main class
    /// </summary>
    //internal class BetterPrerequisites : Mod
    //{

    //    public BetterPrerequisites(ModContentPack content) : base(content)
    //    {
    //        ApplyHarmonyPatches();
    //    }

    //    static void ApplyHarmonyPatches()
    //    {

    //    }
    //}

    [StaticConstructorOnStartup]
    public static class BSCore
    {
        private static readonly Type patchType;
        public static Harmony harmony = new("RedMattis.BetterPrerequisites");
        static BSCore()
        {
            patchType = typeof(BSCore);
            harmony.PatchAll();
            PregnancyPatches.ApplyPatches();
            GeneDefPatcher.PatchDefs();
            XenotypeDefPatcher.PatchDefs();
            ModDefPatcher.PatchDefs();
        }
    }

    public class GlobalSettings : Def
    {
        public List<List<string>> alienGeneGroups = new();

        [Unsaved(false)]
        private static List<List<GeneDef>> alienGeneGroupsDefs = null;

        public static List<List<GeneDef>> GetAlienGeneGroups()
        {
            if (alienGeneGroupsDefs == null)
            {
                alienGeneGroupsDefs = new List<List<GeneDef>>();
                var globalSettings = DefDatabase<GlobalSettings>.AllDefs;
                foreach(var settings in globalSettings.Where(x=>x.alienGeneGroups != null))
                {
                    foreach (var group in settings.alienGeneGroups)
                    {
                        if (group.NullOrEmpty())
                        {
                            continue;
                        }
                        var geneGroup = new List<GeneDef>();
                        foreach (var geneDef in group.Select(x=> DefDatabase<GeneDef>.GetNamed(x, false)))
                        {
                            if (geneDef != null)
                            {
                                geneGroup.Add(geneDef);
                            }
                        }
                        if (geneGroup.Count > 0)
                        {
                            alienGeneGroupsDefs.Add(geneGroup);
                        }
                    }
                }
            }
            return alienGeneGroupsDefs;
        }
    }
}
