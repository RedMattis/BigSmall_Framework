using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace BigAndSmall
{
    //[HarmonyPatch]
    //public static class BE_Production
    //{
    //    private static readonly string[] BE_Properties = new string[]
    //    {
    //        "CompMilkableGene:ResourceAmount",
    //        "CompShearableGene:ResourceAmount",
    //    };

    //    public static bool Prepare()
    //    {
    //        try
    //        {
    //            string[] beProps = BE_Properties;
    //            for (int i = 0; i < beProps.Length; i++)
    //            {
    //                if (!(AccessTools.PropertyGetter(beProps[i]) == null)) return true;
    //            }
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //        return false;
    //    }

    //    public static IEnumerable<MethodBase> TargetMethods()
    //    {
    //        string[] beProps = BE_Properties;
    //        for (int i = 0; i < beProps.Length; i++)
    //        {
    //            MethodInfo propGetter = AccessTools.PropertyGetter(beProps[i]);

    //            if (!(propGetter == null))
    //                yield return propGetter;
    //        }
    //    }

    //    public static void Postfix(ref int __result, ref CompHasGatherableBodyResource __instance)
    //    {
    //        if (__instance.parent is Pawn pawn)
    //            __result = ProductionGene.ModifyProductionBasedOnSize(__result, pawn);
    //    }
    //}
}
