using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace BigAndSmall
{
    public class CompProperties_ConsumeSoul : CompProperties_AbilityEffect
    {
        public float gainMultiplier = 1;
        public float? gainSkillMultiplier = null;
        public float exponentialFalloff = 2.5f;
        public bool doKill = true;
        public bool doEnslave = false;

        public CompProperties_ConsumeSoul()
        {
            compClass = typeof(CompAbilityEffect_ConsumeSoul);
        }
    }

    public class CompAbilityEffect_ConsumeSoul : CompAbilityEffect
    {
        public new CompProperties_ConsumeSoul Props => (CompProperties_ConsumeSoul)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                DrainSoul(parent.pawn, pawn);
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn enemy = target.Pawn;
            if (enemy == null)
            {
                return false;
            }
            if (!AbilityUtility.ValidateMustBeHumanOrWildMan(enemy, throwMessages, parent))
            {
                return false;
            }
            if (!enemy.Downed && enemy.DevelopmentalStage > DevelopmentalStage.Baby)
            {
                if (throwMessages)
                {
                    Messages.Message("MessageCantUseOnResistingPerson".Translate(parent.def.LabelCap), enemy, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            if (enemy.IsWildMan() && !enemy.IsPrisonerOfColony && !enemy.Downed)
            {
                if (throwMessages)
                {
                    Messages.Message("MessageCantUseOnResistingPerson".Translate(parent.def.LabelCap), enemy, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BS_Soulless");
            if (enemy.health.hediffSet.TryGetHediff(hediffDef, out Hediff hediff) && hediff != null)
            {
                if (throwMessages)
                {
                    Messages.Message("BS_CannotUseOnSoulless".Translate(), enemy, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                string text = null;
                if (pawn.HostileTo(parent.pawn) && !pawn.Downed)
                {
                    text += "MessageCantUseOnResistingPerson".Translate(parent.def.Named("ABILITY"));
                }
                return text;
            }
            return base.ExtraLabelMouseAttachment(target);
        }

        public override Window ConfirmationDialog(LocalTargetInfo target, Action confirmAction)
        {
            return null;
        }

        public void DrainSoul(Pawn attacker, Pawn victim)
        {
            // Add Psyfocus to the attacker.
            attacker.psychicEntropy?.OffsetPsyfocusDirectly(1.0f);

            // Check if the attacker has Soul Collector hediff.

            SoulCollector soulCollector = MakeGetSoulCollectorHediff(attacker);
            soulCollector.AddPawnSoul(victim, false, Props.gainMultiplier, Props.exponentialFalloff = 2.5f, Props.gainSkillMultiplier);

            // Remove 50 goodwill from the victim's faction. faction.
            victim.Faction?.TryAffectGoodwillWith(attacker.Faction, -35); // statOffsetsBySeverity

            if (Props.doKill)
            {
                victim.Kill(null);
            }
            else if (Props.doEnslave)
            {
                // If Ideology is active, enslave the pawn.
                if (ModsConfig.IdeologyActive)
                {
                    victim.guest.SetGuestStatus(attacker.Faction, GuestStatus.Slave);
                }
                else
                {
                    victim.guest.resistance = 0;

                    // Set to prisoner.
                    victim.guest.CapturedBy(Faction.OfPlayer, attacker);
                }
            }
            
            ApplySoulless(victim);

            HumanoidPawnScaler.GetCache(attacker, forceRefresh: true);
        }

        public static void ApplySoulless(Pawn victim)
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BS_Soulless");
            if (hediffDef == null)
            {
                Log.Error("Could not find hediff with name " + "BS_Soulless");
                //return;
            }
            victim?.health.AddHediff(hediffDef);
        }

        public static SoulCollector MakeGetSoulCollectorHediff(Pawn attacker)
        {
            var soulCollectorHediff = DefDatabase<HediffDef>.GetNamed("BS_SoulCollector");
            SoulCollector soulCollector = (SoulCollector)attacker.health.hediffSet.GetFirstHediffOfDef(soulCollectorHediff);
            if (soulCollector == null)
            {
                // Add the soul to the attacker.
                attacker.health.AddHediff(soulCollectorHediff);
                soulCollector = (SoulCollector)attacker.health.hediffSet.GetFirstHediffOfDef(soulCollectorHediff);
            }

            return soulCollector;
        }
    }

    public class SoulCollector : HediffWithComps
    {
        public void AddSoulPowerDirect(float amount, float exponentialFalloff = 2.5f)
        {
            if (Severity >= 1) { amount /= (Mathf.Pow(Severity, exponentialFalloff)); }

            Severity += amount;
        }

        public void AddPawnSoul(Pawn target, bool attack, float multiplier = 1, float exponentialFalloff=2.5f, float? gainSkillMultiplier = null)
        {
            const float tuneSPGain = 0.25f;
            const float tunePSGain = 0.25f;
            const float skillGainCap = 0.5f;

            // Get psychic sensitivity of the pawn.
            float gainPS = target.GetStatValue(StatDefOf.PsychicSensitivity) - 0.85f; // We mostly care about sensitivity ab1ove 1.
            float originalAmount = gainPS;

            gainPS = Mathf.Max(0.1f, gainPS) * multiplier;

            float postCap = gainPS;

            if (target?.RaceProps?.Animal == true) { gainPS /= 5; }

            int? architeGeneCount = target.genes?.GenesListForReading.Sum(x=>x.def.biostatArc);

            switch (architeGeneCount)
            {
                case 0:
                    gainPS /= 4f;
                    break;
                case 1:
                    gainPS /= 3f;
                    break;
                case 2:
                    gainPS /= 2f;
                    break;
                default:
                    //gainOffset /= 1.0f;
                    break;
            }
            gainPS *= tunePSGain;

            float gainFromSoulPower = target.GetStatValue(BSDefs.BS_SoulPower) * tuneSPGain;

            float preFalloffTotalGain = gainPS + gainFromSoulPower;
            float actualGain = 0;
            const int itrrCount = 10;
            // This is really just and ugly-looking way to make sure the falloff gets applied reasonably if adding a huge amount at the same time.
            for (int i = 0; i < itrrCount; i++)
            {
                float itrrGain = gainPS / itrrCount;
                if (Severity > 1) { itrrGain /= (Mathf.Pow(Severity + actualGain, exponentialFalloff)); }
                actualGain += itrrGain;
            }
            Severity += actualGain;

            

            if (pawn.needs?.TryGetNeed<Need_KillThirst>() is Need_KillThirst killThirst)
            {
                killThirst.CurLevelPercentage = 1;
            }
            target.psychicEntropy?.OffsetPsyfocusDirectly(0.6f);

            if (gainSkillMultiplier != null && target.skills != null)
            {
                //foreach (var skill in pawn.skills.skills)
                //{
                //    skill.Learn(0.1f * gainSkillMultiplier.Value);
                //}
                float sGainMult = gainSkillMultiplier.Value * Mathf.Min(skillGainCap, preFalloffTotalGain);
                SkillDef philophagySkillAndXpTransfer = PsychicRitualUtility.GetPhilophagySkillAndXpTransfer(pawn, target, sGainMult, out float xpTransfer);
                if (philophagySkillAndXpTransfer == null)
                {
                    Log.Warning("Could not find a skill to transfer xp to.");
                }
                else
                {
                    SkillRecord skill = pawn.skills.GetSkill(philophagySkillAndXpTransfer);
                    skill?.Learn(xpTransfer);
                    if (xpTransfer > 3000)
                    {
                        if (attack)
                        {
                            Messages.Message("BS_StoleSkillAmount_Attack".Translate(pawn.Name.ToStringShort, xpTransfer, skill.def.label, target.Name.ToStringShort),
                                pawn, MessageTypeDefOf.PositiveEvent);
                        }
                        else //BS_StoleSkillAmount_Attack
                        {
                            Messages.Message("BS_StoleSkillAmount".Translate(pawn.Name.ToStringShort, xpTransfer, skill.def.label, target.Name.ToStringShort),
                                pawn, MessageTypeDefOf.PositiveEvent);
                        }
                    }
                }
            }

            if (Severity > 6) { Severity = 6; }

            if (target.Spawned)
            {
                for (int i = 0; i < 5; i++)
                {
                    IntVec3 c = target.Position + GenAdj.AdjacentCellsAndInside[Rand.Range(0, 8)];
                    if (c.InBounds(target.Map))
                    {
                        FilthMaker.TryMakeFilth(c, target.Map, ThingDefOf.Filth_Ash, 1);
                    }
                }
            }
        }

        public override string LabelInBrackets
        {
            get
            {
                // Return pawn BodySize / BodySize of contents to percent.
                try
                {
                    return Severity.ToStringPercent();
                }
                catch
                {
                    return "SPIRIT POWER CALCULATION FAILED";
                }
            }
        }
    }
}
