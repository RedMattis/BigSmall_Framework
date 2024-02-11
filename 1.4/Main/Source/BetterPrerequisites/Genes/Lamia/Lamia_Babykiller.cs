using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompAbilityEffect_LamiaBabyKiller : CompAbilityEffect
    {
        public new CompProperties_AbilityLamiaFeast Props => (CompProperties_AbilityLamiaFeast)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                DoFeast(parent.pawn, pawn);
                //base.Apply(target, dest);
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
            //if (parent.pawn.needs != null && parent.pawn.needs.food != null && parent.pawn.needs.food.CurLevelPercentage > Props.maxHungerPercentThreshold)
            //{
            //    // Lamia is too full.
            //    if (throwMessages)
            //    {
            //        Messages.Message("MessageUserIsNotHungry".Translate(parent.pawn.Label), enemy, MessageTypeDefOf.RejectInput, historical: false);
            //    }
            //    return false;
            //}

            if (enemy.BodySize > parent.pawn.BodySize * Props.relativeSizeThreshold)
            {
                if (throwMessages)
                {
                    Messages.Message("MessagerTargetTooBigToFeastOn".Translate(enemy.Label), enemy, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            //if (!enemy.Downed && enemy.DevelopmentalStage > DevelopmentalStage.Baby)
            //{
            //    if (throwMessages)
            //    {
            //        Messages.Message("MessageCantUseOnResistingPerson".Translate(), enemy, MessageTypeDefOf.RejectInput, historical: false);
            //    }
            //    return false;
            //}
            //if (enemy.IsWildMan() && !enemy.IsPrisonerOfColony && !enemy.Downed)
            //{
            //    if (throwMessages)
            //    {
            //        Messages.Message("MessageCantUseOnResistingPerson".Translate(), enemy, MessageTypeDefOf.RejectInput, historical: false);
            //    }
            //    return false;
            //}
            //if (enemy.InMentalState && enemy.DevelopmentalStage != DevelopmentalStage.Baby)
            //{
            //    if (throwMessages)
            //    {
            //        Messages.Message("MessageCantUseOnResistingPerson".Translate(), enemy, MessageTypeDefOf.RejectInput, historical: false);
            //    }
            //    return false;
            //}
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

                if (!text.NullOrEmpty())
                {
                    text += "\n";
                }
                text += "WillKill".Translate();

                return text;
            }
            return base.ExtraLabelMouseAttachment(target);
        }

        public override Window ConfirmationDialog(LocalTargetInfo target, Action confirmAction)
        {
            return null;
        }

        public void DoFeast(Pawn attacker, Pawn victim)
        {
            //if (attacker.needs?.food != null)
            //{
            //    attacker.needs.food.CurLevel += nutritionPerBodySize * victim.BodySize;
            //}

            float totalEnergyRegained = Props.energyRegained;
            totalEnergyRegained *= Props.energyMultipliedByBodySize ? victim.BodySize : 1;

            //GeneUtility.OffsetHemogen(attacker, totalEnergyRegained);
            BS_GeneCursedPower cursedPower = attacker.genes?.GetFirstGeneOfType<BS_GeneCursedPower>();
            if (cursedPower != null)
                ResourcePoolUtils.OffsetResource(attacker, totalEnergyRegained, cursedPower);

            // Add Psyfocus to the attacker.
            attacker.psychicEntropy?.OffsetPsyfocusDirectly(2);

            //// Get list of all hediffs in the game.
            //var lamiaHediffList = DefDatabase<HediffDef>.AllDefsListForReading.Where(x=>x.defName == "LoS_MythLamiaHD");

            //// Special case for True Lamia.
            //if (lamiaHediffList.Count() > 0 &&
            //    attacker?.genes?.Xenotype?.defName == "LoS_TrueLamia" == true ||
            //    (attacker?.genes?.GenesListForReading.Any(x=>x.def.defName == "LoS_Snake_Tail") == true)
            //    )
            //{
            //    var lamiaHediff = lamiaHediffList.First();
            //    // Add LoS_MythLamiaHD hediff, or increase its severity if it already exists.
            //    Hediff hediff = attacker.health?.hediffSet?.GetFirstHediffOfDef(lamiaHediff);
            //    if (hediff != null)
            //    {
            //        hediff.Severity += 0.1f;
            //    }
            //    else
            //    {
            //        attacker.health?.AddHediff(lamiaHediff);
            //    }
            //}

            //for (int i = 0; i < bloodFilthToSpawnRange; i++)
            //{
            //    IntVec3 c = victim.Position;
            //    if (bloodFilthToSpawnRange > 1 && Rand.Chance(0.8888f))
            //    {
            //        c = victim.Position.RandomAdjacentCell8Way();
            //    }

            //    if (c.InBounds(victim.MapHeld))
            //    {
            //        FilthMaker.TryMakeFilth(c, victim.MapHeld, victim.RaceProps.BloodDef, victim.LabelShort);
            //    }
            //}

            //KillTarget(attacker, victim);

            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BS_Engulfed");
            if (hediffDef == null)
            {
                Log.Error("Could not find hediff with name " + "BS_Engulfed");
                return;
            }

            EngulfHediff engulfHediff;

            // Check if we already have the hediff
            if (attacker.health.hediffSet.HasHediff(hediffDef))
            {
                // Get the hediff we added
                engulfHediff = (EngulfHediff)attacker.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                engulfHediff.Severity = 1;
            }
            else
            {
                attacker.health.AddHediff(hediffDef);
                engulfHediff = (EngulfHediff)attacker.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            }

            engulfHediff.selfDamageMultiplier = Props.selfDamageMultiplier;
            engulfHediff.internalBaseDamage = Props.internalBaseDamage;
            // Add the victim to the hediff's inner container
            engulfHediff.Engulf(victim);
            engulfHediff.canEject = false;

        }

        //private float BloodlossAfterBite(Pawn target)
        //{
        //    if (target.Dead || !target.RaceProps.IsFlesh)
        //    {
        //        return 0f;
        //    }
        //    float num = Props.targetBloodLoss;
        //    Hediff firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
        //    if (firstHediffOfDef != null)
        //    {
        //        num += firstHediffOfDef.Severity;
        //    }
        //    return num;
        //}
    }
}
