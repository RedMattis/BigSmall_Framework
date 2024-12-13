using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RedHealth
{
    [HarmonyPatch]
    public class HarmonyPatches
    {
        //[HarmonyPatch(typeof(Gene), nameof(Gene.ExposeData))]
        //[HarmonyPostfix]
        //public static void Post_ExposeData(Gene __instance)
        //{
        //    if (__instance != null && __instance.pawn != null && PawnGenerator.IsBeingGenerated(__instance.pawn) is false && __instance.Active)
        //    {
        //    }
        //}
        //[HarmonyPatch(typeof(Gene), nameof(Gene.PostAdd))]
        //[HarmonyPostfix]
        //public static void Post_GenePostAdd(Gene __instance)
        //{
        //    if (PawnGenerator.IsBeingGenerated(__instance.pawn) is false && __instance.Active)
        //    {
        //    }
        //}
        //[HarmonyPatch(typeof(PawnGenerator), "GenerateGenes")]
        //[HarmonyPostfix]
        //public static void Post_GenerateGenes(Pawn pawn)
        //{
        //    if (pawn.genes != null)
        //    {
        //        List<Gene> genes = pawn.genes.GenesListForReading;
        //        foreach (Gene gene in genes.Where(x => x != null && x.Active))
        //        {
        //        }
        //    }
        //}

        //public static void AddHealthCompToPawn(Pawn pawn)
        //{
        //    if (pawn != null && pawn.health != null && pawn.health.hediffSet != null)
        //    {
        //        // 
        //    }
        //}
    }
}
