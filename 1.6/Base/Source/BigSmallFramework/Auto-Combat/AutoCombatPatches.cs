using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    //[HarmonyPatch]
    //public class AutoCombatPatches
    //{

    //    [HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse), "TryGetAttackNearbyEnemyJob")]
    //    [HarmonyPostfix]
    //    [HarmonyPriority(Priority.Last)]
    //    public static void TryGetAttackNearbyEnemyJob_Postfix(ref Job __result, Pawn pawn)
    //    {
    //        if (__result != null && DraftGizmos.AutoCombatEnabled
    //            && pawn.Faction == Faction.OfPlayerSilentFail
    //            && DraftedActionHolder.GetData(pawn) is DraftedActionData data && data.hunt
    //            && pawn.mindState.duty?.def != BSDefs.BS_AutoCombatDuty
    //            )
    //        {
    //            pawn.drafter.Drafted = true;
    //            //var pawnDuty = new PawnDuty(BSDefs.BS_AutoCombatDuty);
    //            //pawn.mindState.duty = pawnDuty;
    //            //__result = null;
    //        }
    //    }
    //}
}
