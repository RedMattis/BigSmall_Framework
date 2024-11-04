
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
            HumanPatcher.PatchRecipes();
            NewFoodCategory.SetupFoodCategories();
        }
    }

    public class DefAltNamer : Def
    {
        public static Dictionary<Def, Rename> renames = DefDatabase<DefAltNamer>.AllDefs
            .SelectMany(x => x.defRenames
                .Select(y => (y.def, y))).ToDictionary(x => x.Item1, x => x.Item2);
        public class Rename
        {
            public Def def;
            public string labelMechanoid = null;
            public string labelBloodfeeder = null;
            public string labelFantasy = null;
        }
        public List<Rename> defRenames = [];
    }

    [StaticConstructorOnStartup]
    public class GlobalSettings : Def
    {
        public static Dictionary<string, GlobalSettings> globalSettings = DefDatabase<GlobalSettings>.AllDefs.ToDictionary(x => x.defName);
        public List<List<string>> alienGeneGroups = [];
        public List<XenotypeChance> returnedXenotypes = [];
        public List<XenotypeChance> returnedXenotypesColonist = [];

        [Unsaved(false)]
        private static List<List<GeneDef>> alienGeneGroupsDefs = null;

        public static XenotypeDef GetRandomReturnedXenotype => globalSettings
            .Aggregate(new List<XenotypeChance>(), (acc, x) => [.. acc, .. x.Value.returnedXenotypes])
            .TryRandomElementByWeight(x => x.chance, out var result) ? result.xenotype : null;

        public static XenotypeDef GetRandomReturnedColonistXenotype => globalSettings
            .Aggregate(new List<XenotypeChance>(), (acc, x) => [.. acc, .. x.Value.returnedXenotypesColonist])
            .TryRandomElementByWeight(x => x.chance, out var result) ? result.xenotype : null;

        public static List<List<GeneDef>> GetAlienGeneGroups()
        {
            if (alienGeneGroupsDefs == null)
            {
                alienGeneGroupsDefs = new List<List<GeneDef>>();
                foreach (var settings in globalSettings.Values.Where(x=>x.alienGeneGroups != null))
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
