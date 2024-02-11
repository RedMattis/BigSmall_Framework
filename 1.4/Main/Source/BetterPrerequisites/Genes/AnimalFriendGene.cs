using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.IsAcceptablePreyFor), MethodType.Normal)]
    public static class IsAcceptablePreyFor_Patch
    {
        public static void Postfix(ref bool __result, ref Pawn predator, Pawn prey)
        {
            if (prey != null
                && prey.needs != null)
            {
                var cache = HumanoidPawnScaler.GetBSDict(prey);
                if (cache != null && cache.animalFriend)
                {
                    __result = false;
                }
            }
        }
    }
}
