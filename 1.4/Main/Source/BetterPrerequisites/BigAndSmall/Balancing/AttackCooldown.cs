using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(Tool), "AdjustedCooldown", new Type[] { typeof(Thing) })]
    public static class Tool_AdjustedCooldown_Patch
    {
        public static void Postfix(Thing ownerEquipment, ref float __result)
        {
            // Yoink!
            if (ownerEquipment?.ParentHolder is Pawn_EquipmentTracker pawn_EquipmentTracker && pawn_EquipmentTracker.pawn != null)
            {
                var sizeCache = HumanoidPawnScaler.GetBSDict(pawn_EquipmentTracker.pawn);
                if (sizeCache != null)
                {

                    __result /= sizeCache.attackSpeedMultiplier;
                }
            }
        }
    }

}
