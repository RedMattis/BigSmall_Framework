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
    [HarmonyPatch]
    public class HealthPatches
    {
        [HarmonyPatch(typeof(MedicalRecipesUtility), nameof(MedicalRecipesUtility.IsCleanAndDroppable))]
        public static class IsCleanAndDroppable_Patch
        {
            public static void Postfix(ref bool __result, Pawn pawn, BodyPartRecord part)
            {
                if (__result == true)
                {
                    if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache && !cache.partsCanBeHarvested)
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}
