using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class Universum_Vacuum_Protection
    {
        private static readonly string[] SOSMethods = new string[1]
        {
            "Universum.Utilities.Caching_Handler:spacesuit_protection",
        };

        public static bool Prepare()
        {
            string[] sosMethods = SOSMethods;
            for (int i = 0; i < sosMethods.Length; i++)
            {
                if (!(AccessTools.Method(sosMethods[i]) == null))
                {
                    return true;
                }
                else
                {
                    // Remove these warning later.
                    //Log.Warning($"DEBUG: Failed to find method to patch ({sosMethods[i]}). This probably just means you're not running Universum.");
                }
            }
            return false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            string[] sosMethods = SOSMethods;
            for (int i = 0; i < sosMethods.Length; i++)
            {
                MethodInfo methodInfo = AccessTools.Method(sosMethods[i]);
                if (!(methodInfo == null))
                {
                    Log.Message($"Big & Small found Universum and Postfixed ({sosMethods[i]})");
                    yield return methodInfo;
                }
                else
                {
                    // Remove these warning later.
                    //Log.Warning($"DEBUG: Failed to patch method ({sosMethods[i]})");
                }
            }
        }

        //[HarmonyPrefix]
        public static void Postfix(ref object __result, ref Pawn pawn)
        {
            /*
			3 - All
			2 - Decompression
			1 - Oxygen Only
			0 - None
			*/
            // You typically want 3.
            
            if (pawn.needs != null)
            {
                RaceProperties raceProps = pawn.RaceProps;
                if (raceProps != null && (raceProps.Humanlike || BigSmallMod.settings.scaleAnimals))
                {
                    StatDef evaOverrideDef = StatDef.Named("SM_EVA_Level");
                    float evaOverrideVal = pawn.GetStatValue(evaOverrideDef);
                    if (evaOverrideVal > 0.0)
                    {
                        __result = Enum.Parse(__result.GetType(), "All");
                        //__result = 3;
                    }
                }
            }
        }
    }
}
