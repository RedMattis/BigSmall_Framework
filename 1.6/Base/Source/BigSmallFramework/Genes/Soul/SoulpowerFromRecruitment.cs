using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    [HarmonyPatch]
    public class SoulpowerFromRecruitment
    {
        [HarmonyPatch(typeof(RecruitUtility), nameof(RimWorld.RecruitUtility.Recruit))]
        [HarmonyPostfix]
        public static void DoRecruit(Pawn pawn, Faction faction, Pawn recruiter)
        {
            if (recruiter != null)
            {
                try
                {
                    TryApply(pawn, recruiter);
                }
                catch (Exception e)
                {
                    Log.Error($"Error when checking for soul-on-recruit: {e}\n{e.StackTrace}");
                }
            }
        }

        public static void TryApply(Pawn pawn, Pawn recruiter)
        {
            var pawnExts = recruiter.GetAllPawnExtensions();
            var siphons = pawnExts
                .Select(x => x.siphonSoul)
                .Where(x => x != null && x.type == SiphonType.Influence);
            if (siphons.Any())
            {
                var fused = siphons.FuseAll(SiphonType.Influence);
                SoulCollector soulCollector = Soul.GetOrAddSoulCollector(recruiter);
                soulCollector.AddPawnSoul(pawn, fused, verbose: true);
            }
        }
    }
}
