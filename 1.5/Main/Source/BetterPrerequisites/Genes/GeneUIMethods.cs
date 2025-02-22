﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class Dialog_CreateXenotypePatches
    {
        public static HashSet<GeneDef> hiddenGenes = [];
        [HarmonyPatch(typeof(Dialog_CreateXenotype), "DrawGene")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool DrawGenePrefix(GeneDef geneDef, ref bool __result)
        {
            if (Prefs.DevMode) return true;
            if (hiddenGenes.Contains(geneDef))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
