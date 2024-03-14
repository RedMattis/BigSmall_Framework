using HarmonyLib;
using Mono.Cecil;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;
using static RimWorld.Dialog_EditIdeoStyleItems;

namespace BigAndSmall
{
    public static class LamiaAttack
    {
        

        //private static void KillTarget(Pawn attacker, Pawn victim)
        //{
        //    var killThirds = attacker.needs?.TryGetNeed<Need_KillThirst>();
        //    if (killThirds != null)
        //    {
        //        killThirds.CurLevelPercentage = 1;
        //    }

        //    DamageInfo dinfo = new DamageInfo(new DamageDef { deathMessage= "{0} was eaten" }, 0, instigator: attacker, instigatorGuilty: true, intendedTarget: victim);

        //    if (!victim.IsWildMan())
        //    {
        //        if (attacker.Faction == Faction.OfPlayer
        //            && (!PrisonBreakUtility.IsPrisonBreaking(victim) && !SlaveRebellionUtility.IsRebelling(victim) && !victim.IsSlaveOfColony && !victim.IsPrisoner))
        //        {
        //            int goodwillChange = -30;
        //            Faction.OfPlayer.TryAffectGoodwillWith(victim.Faction, goodwillChange, canSendMessage: true, !victim.Faction.temporary, HistoryEventDefOf.AttackedMember, victim);
        //        }
        //    }

        //    //victim.TakeDamage(dinfo);
        //    victim.Kill(dinfo);
        //    if (MakeCorpse_Patch.corpse != null)
        //    {
        //        MakeCorpse_Patch.corpse.Destroy();
        //        MakeCorpse_Patch.corpse = null;
        //    }

        //    var rawCannibal = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.AteHumanlikeMeatDirectCannibal);
        //    attacker.needs.mood.thoughts.memories.TryGainMemory(rawCannibal);
        //}
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.MakeCorpse), new Type[] { typeof(Building_Grave), typeof(bool), typeof(float) })]
    public static class MakeCorpse_Patch
    {
        public static Corpse corpse = null;
        public static void Postfix(ref Corpse __result, Pawn __instance)
        {
            corpse = __result;
        }
    }
}
