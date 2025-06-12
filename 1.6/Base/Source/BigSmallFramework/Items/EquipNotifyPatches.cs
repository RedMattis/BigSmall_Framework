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
    }
}
