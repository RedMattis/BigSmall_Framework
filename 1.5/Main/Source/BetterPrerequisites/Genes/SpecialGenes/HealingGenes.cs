using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static partial class Helpers
    {
        public static void CureWorstInjury(Pawn pawn)
        {
            Hediff_Injury hediff_Injury = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff_Injury hediff_Injury2 = hediffs[i] as Hediff_Injury;
                if (hediff_Injury2 != null && hediff_Injury2.Visible && hediff_Injury2.def.everCurableByItem && (hediff_Injury == null || hediff_Injury2.Severity > hediff_Injury.Severity))
                {
                    hediff_Injury = hediff_Injury2;
                }
            }

            if (hediff_Injury != null)
            {
                HealthUtility.Cure(hediff_Injury);
                return;
            }

            BodyPartRecord bodyPartRecord = null;
            float coverageAbsWithChildren = ThingDefOf.Human.race.body.GetPartsWithDef(BodyPartDefOf.Hand).First().coverageAbsWithChildren;
            foreach (Hediff_MissingPart missingPartsCommonAncestor in pawn.health.hediffSet.GetMissingPartsCommonAncestors())
            {
                if (!(missingPartsCommonAncestor.Part.coverageAbsWithChildren < coverageAbsWithChildren) && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(missingPartsCommonAncestor.Part) && (bodyPartRecord == null || missingPartsCommonAncestor.Part.coverageAbsWithChildren > bodyPartRecord.coverageAbsWithChildren))
                {
                    bodyPartRecord = missingPartsCommonAncestor.Part;
                }
            }

            if (bodyPartRecord != null)
            {
                pawn.health.RestorePart(bodyPartRecord);
                return;
            }
        }
    }
    public class Gene_FastHealing : Gene_Healing
    {
        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 10000 == 0 && Verse.Rand.Chance(0.33f))
            { 
                Helpers.CureWorstInjury(pawn);
            }
        }
    }

    public class Gene_SelfRestoration : Gene_Healing
    {
        private int ticksToHeal = 100;

        private static readonly IntRange HealingIntervalTicksRange = new IntRange(2500, 30000);

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 10000 == 0 && Verse.Rand.Chance(0.33f))
            { 
                HealthUtility.FixWorstHealthCondition(pawn);
            }
        }

        private void ResetInterval()
        {
            ticksToHeal = HealingIntervalTicksRange.RandomInRange;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ticksToHeal, "ticksToHeal");
            base.ExposeData();
        }
    }
}
