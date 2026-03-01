using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class Soul
    {
        public static SoulCollector GetOrAddSoulCollector(Pawn attacker)
        {
            if (BSDefs.BS_SoulCollector == null)
            {
                Log.Warning("Soul Collector Hediff is null. This is likely due to a missing mod or a missing def.");
                return null;
            }
            SoulCollector soulCollector = (SoulCollector)attacker.health.hediffSet.GetFirstHediffOfDef(BSDefs.BS_SoulCollector);
            if (soulCollector == null)
            {
                // Add the soul to the attacker.
                attacker.health.AddHediff(BSDefs.BS_SoulCollector);
                soulCollector = (SoulCollector)attacker.health.hediffSet.GetFirstHediffOfDef(BSDefs.BS_SoulCollector);
            }
            return soulCollector;
        }
    }

    public class CompProperties_ConsumeSoul : CompProperties_AbilityEffect
    {
        public SiphonSoul siphonSoul = new();
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
            if (enemy.health.hediffSet.HasHediff(BSDefs.BS_Soulless))
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
            soulCollector.AddPawnSoul(victim, Props.siphonSoul);

            // Remove 50 goodwill from the victim's faction. faction.
            victim.Faction?.TryAffectGoodwillWith(attacker.Faction, -35); // statOffsetsBySeverity

            if (Props.doKill)
            {
                ApplySoulless(victim);
                victim.Kill(null);
            }
            else if (Props.doEnslave)
            {
                ApplySoulless(victim);
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

            HumanoidPawnScaler.GetCache(attacker, forceRefresh: true);
        }

        public static void ApplySoulless(Pawn victim)
        {
            if (victim?.RaceProps?.Humanlike == true)
            {
                victim?.health.AddHediff(BSDefs.BS_Soulless);
            }
        }

        public static SoulCollector MakeGetSoulCollectorHediff(Pawn attacker) => Soul.GetOrAddSoulCollector(attacker);
    }

    public class SoulCollector : HediffWithComps
    {
        protected readonly SoulEnergyTracker soulTracker = new();
        protected SoulResourceHediff Resource => soulTracker.Resource(pawn);

        public void AddSoulPowerDirect(float amount, float exponentialFalloff = 2.5f)
        {
            if (Severity >= 1) { amount /= (Mathf.Pow(Severity, exponentialFalloff)); }

            Severity += amount;
        }

        private static int WarnedAboutLowPsyFocusCount;

        public float AddPawnSoul(Pawn target, SiphonSoul parms, bool verbose=false)
        {
            // Mostly so we get values like "1" instead of 0.01, 0.00001, etc. and other not-very-human-readable XML.
            const float asPercent = 0.01f;

            int architeGeneCount = target.genes?.GenesListForReading.Sum(x => x.def.biostatArc) ?? 0;
            float architeGeneFactor = (architeGeneCount) switch
            {
                0 => 1,
                1 => 1.4f,
                2 => 1.6f,
                3 => 1.75f,
                4 => 1.9f,
                _ => 2,
            };
            architeGeneFactor = Mathf.Lerp(1, architeGeneFactor, parms.architeGeneFactor);

            // TargetPsyFocusOffset default to -0.85. We want to make targeting high psychic sensitivity important.
            float psyFocusOffset = parms.targetPsyFocusOffset;
            if (psyFocusOffset < 0)
            {
                psyFocusOffset /= 1 + architeGeneCount;
            }

            // Get psychic sensitivity of the pawn.
            float moddedPsyfocus = target.GetStatValue(StatDefOf.PsychicSensitivity) + psyFocusOffset; 
            moddedPsyfocus = Mathf.Max(parms.minimumBaseGain, moddedPsyfocus);
            moddedPsyfocus = Mathf.Min(parms.fromTargetPsyfocusFactor_Max, moddedPsyfocus);
            moddedPsyfocus += parms.gainOffset * asPercent;
            moddedPsyfocus *= asPercent;
            float gainPS = moddedPsyfocus;

            if (target?.RaceProps?.Animal == true) { gainPS /= 5; }

            gainPS *= architeGeneFactor;
            gainPS *= parms.fromTargetPsyfocusFactor;

            float falloffStartOffset = pawn.GetAllPawnExtensions().Sum(x => x.soulFalloffStart);
            float siphonFactor = 1 + pawn.GetAllPawnExtensions().Sum(x => x.siphonFactorOffset);
            
            // Skill
            float siphonSkillMult = 1 + pawn.GetAllPawnExtensions().Sum(x => x.siphonSkillFactorOffset);
            float skillGainFactor = siphonSkillMult * parms.gainSkill;
            float skillGainBase = moddedPsyfocus;

            // Soul
            float gainFromSoulPower = target.GetStatValue(BSDefs.BS_SoulPower) * parms.fromTargetSoulFactor;
            gainFromSoulPower *= asPercent;

            float preFalloffTotalGain = (gainPS + gainFromSoulPower) * parms.gainFactor * siphonFactor;

            if (Resource != null)
            {
                Resource.Value += preFalloffTotalGain * 50;
            }

            preFalloffTotalGain *= BigSmallMod.settings.soulPowerGainMultiplier;

            if (WarnedAboutLowPsyFocusCount < 1 && parms.type == SiphonType.ConsumeSoul && pawn.Faction == Faction.OfPlayerSilentFail && preFalloffTotalGain <= 0.011f)
            {
                WarnedAboutLowPsyFocusCount++;
                Messages.Message("BS_LowPsyfocusTarget".Translate(pawn.Name.ToStringShort, target.Name.ToStringShort),
                                pawn, MessageTypeDefOf.NeutralEvent);
            }

            float actualGain = 0;
            const int itrrCount = 20;
            float softCapMod = 0.0f; // So basically at 100% we start getting falloff.
            softCapMod += falloffStartOffset;
            softCapMod += BigSmallMod.settings.soulPowerFalloffOffset;

            float severity = Severity;
            float adjustedV = severity - softCapMod;
            // This is really just and ugly-looking way to make sure the falloff gets applied reasonably if adding a huge amount at the same time.
            for (int i = 0; i < itrrCount; i++)
            {
                float itrrGain = preFalloffTotalGain / itrrCount;
                if (adjustedV > 1) { itrrGain /= Mathf.Pow(adjustedV + actualGain, 2); }
                actualGain += itrrGain;
            }
            Severity += actualGain;

            if (pawn.needs?.TryGetNeed<Need_KillThirst>() is Need_KillThirst killThirst && (parms.type == SiphonType.KillingBlow || parms.type == SiphonType.ConsumeSoul))
            {
                killThirst.CurLevelPercentage = 1;
            }
            target.psychicEntropy?.OffsetPsyfocusDirectly(moddedPsyfocus*2);

            if (verbose || actualGain > 0.2f)
            {
                Messages.Message("BS_GainedSoulPower".Translate(pawn.Name.ToStringShort, ($"{actualGain * 100:f1}%"), BSDefs.BS_SoulPower.LabelCap, target.Name.ToStringShort), pawn, MessageTypeDefOf.PositiveEvent); 
            }

            if (skillGainFactor > 0 && target.skills != null)
            {
                float percentTransfer = skillGainBase * skillGainFactor;
                percentTransfer = Mathf.Min(percentTransfer, parms.maxXpDrainPercent);
                SkillDef philophagySkillAndXpTransfer = PsychicRitualUtility.GetPhilophagySkillAndXpTransfer(pawn, target, percentTransfer, out float xpTransfer);
                xpTransfer = Mathf.Min(xpTransfer, parms.maxXPDrain * siphonSkillMult);
                if (philophagySkillAndXpTransfer == null)
                {
                    Log.Warning("Could not find a skill to transfer xp to.");
                }
                else
                {
                    SkillRecord skill = pawn.skills.GetSkill(philophagySkillAndXpTransfer);
                    skill?.Learn(xpTransfer, direct:false, ignoreLearnRate:true);

                    SkillRecord skill2 = target.skills.GetSkill(philophagySkillAndXpTransfer);
                    skill2?.Learn(-xpTransfer, direct: true, ignoreLearnRate: true);
                    if (verbose || xpTransfer >= 2500)
                    {
                        if (parms.type == SiphonType.KillingBlow)
                        {
                            Messages.Message("BS_StoleSkillAmount_Attack".Translate(pawn.Name.ToStringShort, xpTransfer, skill.def.label, target.Name.ToStringShort),
                                pawn, MessageTypeDefOf.PositiveEvent);
                        }
                        else if (parms.type == SiphonType.Influence)
                        {
                            Messages.Message("BS_StoleSkillAmount_Influence".Translate(pawn.Name.ToStringShort, xpTransfer, skill.def.label, target.Name.ToStringShort),
                                pawn, MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            Messages.Message("BS_StoleSkillAmount".Translate(pawn.Name.ToStringShort, xpTransfer, skill.def.label, target.Name.ToStringShort),
                                pawn, MessageTypeDefOf.PositiveEvent);
                        }
                    }
                }
            }

            if (target.Spawned && (parms.type == SiphonType.KillingBlow || parms.type == SiphonType.ConsumeSoul))
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
            return actualGain;
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
