using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(SanguophageUtility), nameof(SanguophageUtility.DoBite))]
    public static class SanguophageUtility_DoBite_Patch
    {
        public static void Postfix(Pawn biter, Pawn victim, float targetHemogenGain, float nutritionGain, float targetBloodLoss, float victimResistanceGain, IntRange bloodFilthToSpawnRange, ThoughtDef thoughtDefToGiveTarget = null, ThoughtDef opinionThoughtToGiveTarget = null)
        {
            var biterGenes = Helpers.GetAllActiveEndoGenes(biter);

            // Check if biters has white rose bite
            var whiteRoseBite = biterGenes.Any(x => x.def.defName == "VU_WhiteRoseBite");
            //var draculBite = biterGenes.Any(x => x.def.defName == "VU_DraculBite");
            var succubusBite = biterGenes.Any(x => x.def.defName == "VU_SuccubusBloodFeeder");

            if (whiteRoseBite)
            {
                CompAbilityEffect_WhiteRoseBite.WhiteRoseBite(victim);
            }

            if (succubusBite)
            {
                if (victim.IsPrisoner)
                {
                    // Resistance effectively evens it out instead.
                    victim.guest.resistance = Mathf.Min(victim.guest.resistance - 1, victim.kindDef.initialResistanceRange.Value.TrueMax);
                    victim.guest.will = Mathf.Min(victim.guest.will - 2, victim.kindDef.initialWillRange.Value.TrueMax);
                }
            }

        }
    }

    public class CompProperties_WhiteRoseBite : CompProperties_AbilityBloodfeederBite
    {
        public CompProperties_WhiteRoseBite()
        {
            compClass = typeof(CompAbilityEffect_WhiteRoseBite);
        }
    }

    public class CompAbilityEffect_WhiteRoseBite : CompAbilityEffect_BloodfeederBite
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null)
            {
                return;
            }

            WhiteRoseBite(pawn);
            base.Apply(target, dest);
        }
        
        public static void WhiteRoseBite(Pawn pawn)
        {
            // Insert White-rose Specific code here.
            var whBitemark = pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_WhiteRoseBite);
            var thrallHediff = pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_WhiteRoseThrall);

            if (thrallHediff == null)
            {
                if (whBitemark == null)
                {
                    whBitemark = HediffMaker.MakeHediff(BSDefs.VU_WhiteRoseBite, pawn);
                    pawn.health.AddHediff(whBitemark);
                }
                else
                {
                    whBitemark.Severity += 0.5f;
                }
                if (whBitemark.Severity >= 1)
                {
                    thrallHediff = HediffMaker.MakeHediff(BSDefs.VU_WhiteRoseThrall, pawn);
                    pawn.health.AddHediff(thrallHediff);
                    pawn.health.RemoveHediff(whBitemark);

                }
            }
            else
            {
                thrallHediff.Severity += 0.7f;
            }
            var euHediff = pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_Euphoria);
            if (euHediff == null)
            {
                euHediff = HediffMaker.MakeHediff(BSDefs.VU_Euphoria, pawn);
                pawn.health.AddHediff(euHediff);
            }
            else
            {
                euHediff.Severity = 1;
            }

            if (pawn.IsPrisoner)
            {
                pawn.guest.resistance = Mathf.Min(pawn.guest.resistance - 2, pawn.kindDef.initialResistanceRange.Value.TrueMax);
                pawn.guest.will = Mathf.Min(pawn.guest.will - 2, pawn.kindDef.initialWillRange.Value.TrueMax);
            }
        }
    }

    


    public class CompProperties_DraculBite : CompProperties_AbilityBloodfeederBite
    {
        public CompProperties_DraculBite()
        {
            compClass = typeof(CompAbilityEffect_DraculBite);
        }
    }
    /// <summary>
    /// Bite and apply Vampirism
    /// </summary>
    public class CompAbilityEffect_DraculBite : CompAbilityEffect_BloodfeederBite
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null)
            {
                return;
            }

            IncreaseVampirism(pawn);
            base.Apply(target, dest);
        }

        private void IncreaseVampirism(Pawn pawn)
        {
            var attacker = parent.pawn;
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_DraculVampirism);

            // Get level of vampire from DraculStageExtension
            (int stage, _) = DraculStageExtension.TryGetDraculStage(attacker);

            if (hediff is DraculVampirism || hediff == null)
            {
                DraculVampirism dVampirism;
                if (hediff == null)
                {
                    dVampirism = (DraculVampirism)HediffMaker.MakeHediff(BSDefs.VU_DraculVampirism, pawn);
                    pawn.health.AddHediff(dVampirism);
                }
                else
                {
                    dVampirism = (DraculVampirism)hediff;
                    dVampirism.Severity += 0.15f;
                }
                
                dVampirism.stageOfMostPowerfulDracul = Mathf.Max(dVampirism.stageOfMostPowerfulDracul, stage);
                dVampirism.factionOfMaster = attacker.Faction;
            }
            else
            {
                Log.Warning($"Something went wrong, {pawn} hediff should be DraculVampirism but is {hediff.GetType()}");
            }
        }
    }

    public class CompProperties_DraculInfect : CompProperties_AbilityBloodfeederBite
    {
        public CompProperties_DraculInfect()
        {
            compClass = typeof(CompAbilityEffect_DraculInfect);
        }
    }

    /// <summary>
    /// Just apply a bunch of Vampirism.
    /// </summary>
    public class CompAbilityEffect_DraculInfect : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = target.Pawn ?? (target.Thing as Corpse)?.InnerPawn;
            
            if (pawn == null)
            {
                return;
            }

            IncreaseVampirism(pawn);
            base.Apply(target, dest);
        }

        private void IncreaseVampirism(Pawn pawn)
        {
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(BSDefs.VU_DraculVampirism);

            var attacker = parent.pawn;
            
            // Get level of vampire from DraculStageExtension
            (int stage, _) = DraculStageExtension.TryGetDraculStage(attacker);

            if (hediff is DraculVampirism || hediff == null)
            {
                DraculVampirism dVampirism;
                if (hediff == null)
                {
                    dVampirism = (DraculVampirism)HediffMaker.MakeHediff(BSDefs.VU_DraculVampirism, pawn);
                    pawn.health.AddHediff(dVampirism);
                }
                else
                {
                    dVampirism = (DraculVampirism)hediff;
                }
                dVampirism.Severity += 0.45f;
                dVampirism.stageOfMostPowerfulDracul = Mathf.Max(dVampirism.stageOfMostPowerfulDracul, stage);
                dVampirism.factionOfMaster = attacker.Faction;
            }
            else
            {
                Log.Warning($"Something went wrong, {pawn} hediff should be DraculVampirism but is {hediff.GetType()}");
            }
        }
    }
}
