using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace BigAndSmall
{
   
    [HarmonyPatch(typeof(Verb_MeleeAttack), "GetDodgeChance")]
    public static class VerbMeleeAttack_GetDodgeChance
    {
        public static void Postfix(ref float __result, LocalTargetInfo target)
        {
            if (target.Thing is Pawn pawn && __result < 0.99f && pawn.GetCachePrepatched() is BSCache sizeCache)
            {
                __result /= sizeCache.scaleMultiplier.linear;
                if (__result >= 0.96)
                    __result = 0.96f;
            }
        }
    }

}
