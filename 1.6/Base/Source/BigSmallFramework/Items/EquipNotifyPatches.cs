using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class EquipNotifyPatches
    {
        [HarmonyPatch(typeof(Thing), nameof(Thing.Notify_Equipped))]
        [HarmonyPostfix]
        public static void Notify_Notify_Equipped(Thing __instance, Pawn pawn)
        {
            HumanoidPawnScaler.GetInvalidateLater(pawn, scheduleForce:1);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Thing), nameof(Thing.Notify_Unequipped))]
        public static void Notify_Notify_Unequipped(Thing __instance, Pawn pawn)
        {
            HumanoidPawnScaler.GetInvalidateLater(pawn, scheduleForce: 1);
        }

        [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded))]
        [HarmonyPostfix]
        public static void Notify_EquipmentAdded(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            if (__instance.pawn is Pawn p)
                HumanoidPawnScaler.GetInvalidateLater(p, scheduleForce: 1);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentRemoved))]
        public static void Notify_Notify_EquipmentRemoved(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            if (__instance.pawn is Pawn p)
                HumanoidPawnScaler.GetInvalidateLater(p, scheduleForce: 1);
        }
    }
}
