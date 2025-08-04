using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    // Use harmony to prefix RemoveBond
    [HarmonyPatch(typeof(Gene_PsychicBonding), nameof(Gene_PsychicBonding.RemoveBond))]
    public static class Gene_PsychicBonding_RemoveBond
    {
        public static void Prefix(Gene_PsychicBonding __instance, ref Pawn ___bondedPawn)
        {
            // add a print on every other line
            if (GeneHelpers.GetActiveGenesByName(__instance.pawn, "VU_LethalLover").Count > 0)
            {
                //__instance.pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.PsychicBondTorn, ___bondednPawn);
                ___bondedPawn?.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.PsychicBondTorn, __instance.pawn);
                if (___bondedPawn != null)
                {
                    Hediff parasiticBond = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_SuccubusBond);
                    if (parasiticBond != null)
                    {
                        __instance.pawn.health.RemoveHediff(parasiticBond);
                    }
                    Hediff parasiticVictim = ___bondedPawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_SuccubusBond_Victim);
                    if (parasiticVictim != null)
                    {
                        ___bondedPawn.health.RemoveHediff(parasiticVictim);
                    }


                    Pawn partnerPawn = ___bondedPawn;
                    ___bondedPawn = null;
                    Hediff_PsychicBond hediff_PsychicBond = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) as Hediff_PsychicBond;
                    if (hediff_PsychicBond != null && hediff_PsychicBond.target == partnerPawn)
                    {
                        __instance.pawn.health.RemoveHediff(hediff_PsychicBond);
                    }

                    Hediff_PsychicBond hediff_PsychicBond2 = partnerPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) as Hediff_PsychicBond;
                    if (hediff_PsychicBond2 != null)
                    {
                        partnerPawn.health.RemoveHediff(hediff_PsychicBond2);
                    }
                    partnerPawn.genes?.GetFirstGeneOfType<Gene_PsychicBonding>()?.RemoveBond();
                    if (__instance.pawn.Dead)
                    {
                        if (partnerPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBondTorn) == null)
                        {
                            Hediff_PsychicBondTorn hediff_PsychicBondTorn = (Hediff_PsychicBondTorn)HediffMaker.MakeHediff(HediffDefOf.PsychicBondTorn, partnerPawn);
                            hediff_PsychicBondTorn.target = __instance.pawn;
                            partnerPawn.health.AddHediff(hediff_PsychicBondTorn);
                        }
                        if (DefDatabase<MentalBreakDef>.AllDefsListForReading.Where((MentalBreakDef d) => d.intensity == MentalBreakIntensity.Extreme && d.Worker.BreakCanOccur(partnerPawn)).TryRandomElementByWeight((MentalBreakDef d) => d.Worker.CommonalityFor(partnerPawn, moodCaused: true), out var result))
                        {
                            result.Worker.TryStart(partnerPawn, "MentalStateReason_BondedHumanDeath".Translate(__instance.pawn), causedByMood: false);
                        }
                    }
                }
            }
        }
    }
    // Postfix BondTo to add a Hediff to the bonded pawn
    [HarmonyPatch(typeof(Gene_PsychicBonding), nameof(Gene_PsychicBonding.BondTo))]
    public static class Gene_PsychicBonding_BondTo
    {
        public static void Postfix(Gene_PsychicBonding __instance, ref Pawn ___bondedPawn)
        {
            if (GeneHelpers.GetActiveGenesByName(__instance.pawn, "VU_LethalLover").Count > 0)
            {
                if (___bondedPawn != null)
                {
                    var parasiticBond = HediffMaker.MakeHediff(BSDefs.VU_SuccubusBond, __instance.pawn);
                    __instance.pawn.health.AddHediff(parasiticBond);
                    
                    var parasiticBondVictim = HediffMaker.MakeHediff(BSDefs.VU_SuccubusBond_Victim, ___bondedPawn);
                    ___bondedPawn.health.AddHediff(parasiticBondVictim);
                }
            }
        }
    }

    // Postfix Hediff so if it tries to add a hediff called "VRE_PsychicBondBloodlust" it will be removed
    [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostAdd))]
    public static class Hediff_PostAdd
    {
        public static void Postfix(Hediff __instance)
        {
            if (__instance?.def?.defName == "VRE_PsychicBondBloodlust" && __instance?.pawn?.health != null
                && GeneHelpers.GetAllActiveGenes(__instance.pawn).Any(x=>x.def?.defName == "VU_LethalLover"))
            {
                try
                {
                    __instance.pawn.health.RemoveHediff(__instance);
                }
                catch (Exception e)
                {
                    Log.Error($"Exception removing hediff:\n{e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}
