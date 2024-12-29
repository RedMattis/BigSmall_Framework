using HarmonyLib;
using System;
using Verse;

namespace BigAndSmall
{
    //[HarmonyPatch(typeof(Tool), "AdjustedCooldown", new Type[] { typeof(Thing) })]
    //public static class Tool_AdjustedCooldown_Patch
    //{
    //    public static void Postfix(Thing ownerEquipment, ref float __result)
    //    {
    //        // Yoink!
    //        if (ownerEquipment?.ParentHolder is Pawn_EquipmentTracker pawn_EquipmentTracker && pawn_EquipmentTracker.pawn != null)
    //        {
    //            var sizeCache = HumanoidPawnScaler.GetBSDict(pawn_EquipmentTracker.pawn);
    //            if (sizeCache != null)
    //            {
    //                float oldResult = __result;
    //                __result /= sizeCache.attackSpeedMultiplier;
    //                Log.Message($"New attack speed: {oldResult} -> {__result}");
    //            }
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(VerbProperties), "AdjustedCooldown", [typeof(Tool), typeof(Pawn), typeof(Thing)])]
    public static class VerbProperties_AdjustedCooldown_Patch
    {
        public static void Postfix(Tool tool, Pawn attacker, Thing equipment, ref float __result)
        {
            var sizeCache = HumanoidPawnScaler.GetCache(attacker);
            if (sizeCache != null)
            {
                if (equipment == null)
                {
                    __result /= (sizeCache.attackSpeedUnarmedMultiplier + sizeCache.attackSpeedMultiplier - 1);
                }
                else
                {
                    __result /= sizeCache.attackSpeedMultiplier;
                }
                
            }
        }
    }

}
