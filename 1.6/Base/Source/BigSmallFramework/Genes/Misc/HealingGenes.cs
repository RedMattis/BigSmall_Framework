using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public static partial class GeneHelpers
    {
        public static void CureWorstInjury(Pawn pawn)
        {
            Hediff_Injury hediff_Injury = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff_Injury hediff_Injury2 = hediffs[i] as Hediff_Injury;
                if (hediff_Injury2?.def?.isBad == true && hediff_Injury2.Visible && hediff_Injury2.IsPermanent() && hediff_Injury2.def.everCurableByItem && (hediff_Injury == null || hediff_Injury2.Severity > hediff_Injury.Severity))
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

            if (bodyPartRecord == null)
            {
                var anyCurableHediff = pawn.health.hediffSet.hediffs.Where(x => x?.def?.isBad == true && x.def.everCurableByItem);
                if (anyCurableHediff.Any())
                {
                    HealthUtility.Cure(anyCurableHediff.RandomElement());
                    return;
                }
            }
        }
    }
    public class Gene_FastHealing : TickdownGene
    {
        public override void ResetCountdown()
        {
            tickDown = Rand.Range(30000, 90000);
        }

        public override void TickEvent()
        {
            GeneHelpers.CureWorstInjury(pawn);
        }
    }

    public class Gene_SelfRestoration : TickdownGene
    {
        public override void ResetCountdown()
        {
            tickDown = Rand.Range(30000, 90000);
        }

        public override void TickEvent()
        {
            HealthUtility.FixWorstHealthCondition(pawn);
        }
    }
}
