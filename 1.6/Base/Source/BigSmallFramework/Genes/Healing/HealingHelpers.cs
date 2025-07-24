using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public static partial class HealthHelpers
    {
        public static List<Hediff_Injury> GetAllInjuries(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            if (hediffs == null || hediffs.Count == 0)
            {
                return [];
            }
            return [.. hediffs.OfType<Hediff_Injury>().Where(x => x.def.isBad && x.Visible && x.def.everCurableByItem)];
        }

        public static Hediff_Injury GetRandomScar(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            if (hediffs == null || hediffs.Count == 0)
            {
                return null;
            }
            var scars = hediffs.OfType<Hediff_Injury>()
                .Where(x => x.def.isBad && x.Visible && x.IsPermanent() && x.def.everCurableByItem)
                .ToList();
            if (scars.Count == 0)
            {
                return null;
            }
            return scars.RandomElement();
        }

        public static Hediff_MissingPart GetMissingPart(Pawn pawn)
        {
            var allMissingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors();

            if (allMissingParts == null || allMissingParts.Count == 0)
            {
                return null;
            }
            allMissingParts = [.. allMissingParts.Where(missingPart => !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(missingPart.Part))];

            if (allMissingParts.Count == 0)
            {
                return null;
            }
            return allMissingParts.OrderByDescending(missingPart => missingPart.Part.coverageAbsWithChildren).FirstOrDefault();
        }

        public static void CureWorstInjury(Pawn pawn)
        {
            Hediff_Injury hediff_Injury = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff_Injury asInjury = hediffs[i] as Hediff_Injury;
                if (asInjury?.def?.isBad == true && asInjury.Visible && asInjury.IsPermanent() && asInjury.def.everCurableByItem && (hediff_Injury == null || asInjury.Severity > hediff_Injury.Severity))
                {
                    hediff_Injury = asInjury;
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
}
