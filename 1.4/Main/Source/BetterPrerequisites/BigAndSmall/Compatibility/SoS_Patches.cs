//using HarmonyLib;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Verse;

//namespace BigAndSmall
//{


//    [HarmonyPatch]
//    public static class SoS_EVAlevelSlowPatch
//    {
//        private static readonly string[] SOSMethods = new string[2]
//        {
//            "SaveOurShip2.ShipInteriorMod2:EVAlevel",
//            //"ShipInteriorMod2:EVAlevel",
//            //"SaveOurShip2.ShipInteriorMod2:EVAlevelSlow",
//            "ShipInteriorMod2:EVAlevelSlow",
//            //"SaveOurShip2:EVAlevel",
//        };

//        public static bool Prepare()
//        {
//            string[] sosMethods = SOSMethods;
//            for (int i = 0; i < sosMethods.Length; i++)
//            {
//                if (!(AccessTools.Method(sosMethods[i]) == null))
//                {
//                    //Log.Message($"Big & Small Postfixed ({sosMethods[i]})");
//                    return true;
//                }
//                else
//                {
//                    // Remove these warning later.
//                    //Log.Warning($"DEBUG: Failed to find method to patch ({sosMethods[i]})");
//                }
//            }
//            return false;
//        }

//        public static IEnumerable<MethodBase> TargetMethods()
//        {
//            string[] sosMethods = SOSMethods;
//            for (int i = 0; i < sosMethods.Length; i++)
//            {
//                MethodInfo methodInfo = AccessTools.Method(sosMethods[i]);
//                if (!(methodInfo == null))
//                {
//                    Log.Message($"Big & Small found SOS2 and Postfixed ({sosMethods[i]})");
//                    yield return methodInfo;
//                }
//                else
//                {
//                    // Remove these warning later.
//                    //Log.Warning($"DEBUG: Failed to patch method ({sosMethods[i]})");
//                }
//            }
//        }

//        //[HarmonyPrefix]
//        public static void Postfix(ref byte __result, ref Pawn pawn)
//        {
//            /*
//			8 - natural, unremovable, boosted: no rechecks
//			7 - boosted EVA: reset on equip change
//			6 - natural, unremovable: no rechecks
//			5 - proper EVA: reset on equip/hediff change
//			4 - active belt: reset on end
//			3 - inactive belt: trigger in weather
//			2 - skin only: reset on hediff change
//			1 - air only: reset on hediff change
//			0 - none: dead soon
//			*/
//            // You typically want 5.
//            if (__result < 5)
//            {
//                if (BigSmall.performScaleCalculations
//                && BigSmall.humnoidScaler != null
//                && pawn.needs != null)
//                {
                
//                    RaceProperties raceProps = pawn.RaceProps;
//                    if (raceProps != null && (raceProps.Humanlike || BigSmallMod.settings.scaleAnimals))
//                    {
//                        StatDef evaOverrideDef = StatDef.Named("SM_EVA_Level");
//                        float evaOverrideVal = pawn.GetStatValue(evaOverrideDef);
//                        if (evaOverrideVal > 0)
//                        {
//                            __result = 5;
//                        }
//                    }
//                }
//            }
//        }
//    }
//}
