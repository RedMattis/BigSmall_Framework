using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public class ShouldAddNodeToTree_Prefix
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRenderTree), "ShouldAddNodeToTree")]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(PawnRenderNodeProperties props, PawnRenderTree __instance, ref bool __result)
        {
            if (props?.pawnType == PawnRenderNodeProperties.RenderNodePawnType.HumanlikeOnly)
            {
                var pawn = __instance.pawn;
                if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && cache.hideHumanlikeRenderNodes && !cache.IsTempCache && cache.isHumanlike)
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
