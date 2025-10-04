using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public class CanBeStunnedByDamagePatch
    {
        [HarmonyPatch(typeof(StunHandler), "CanBeStunnedByDamage")]
        [HarmonyPrefix]
        public static bool CanBeStunnedByDamage_Prefix(ref bool __result, StunHandler __instance, DamageDef def)
        {
            if (def.causeStun && __instance.parent is Pawn pawn && pawn.GetCachePrepatched() is BSCache cache)
            {
                if (def == DamageDefOf.EMP && cache.empVulnerable)
                {
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }
}
