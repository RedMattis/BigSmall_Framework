using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class GenePrerequisites : DefModExtension
    {
        public List<PrerequisiteSet> prerequisiteSets;
    }

    public class PrerequisiteSet
    {
        public enum PrerequisiteType
        {
            AnyOf, AllOf, NoneOf
        }
        //[Flags]
        //public enum PrerequisiteCheck // Not yet implemented. Always uses Genes right now.
        //{
        //    Gene,
        //    Hediff,
        //    PawnExtension,
        //}

        public float allOfPerecntage = 1.0f; // Percentage of genes that must be present for AllOf to be considered met.
        public float noneOfPercentage = 0f;
        public List<string> prerequisites;
        public PrerequisiteType type;
    }


    
    // Class which validates so all prerequisites are met.
    // If there are no prequisites it will return true. Otherwise it will return true if the perequisites are met.
    public class PrerequisiteValidator
    {
        public static string GeneDefLabelDefName(string defName)
        {
            var gDef = DefDatabase<GeneDef>.GetNamedSilentFail(defName);
            return gDef == null ? defName : gDef.LabelCap;

        }

        public static string Validate(GeneDef gene, Pawn pawn)
        {
            try
            {
                if (gene.prerequisite is GeneDef prerequsiteGene && !GeneHelpers.GetAllActiveGenes(pawn).Any(x => x.def == prerequsiteGene))
                {
                    return "BS_PrerequisitesNotMetAnyOf".Translate($"{string.Join(", ", prerequsiteGene.LabelCap)}");
                }

                if (gene.HasModExtension<GenePrerequisites>())
                {
                    var geneExtension = gene.GetModExtension<GenePrerequisites>();
                    if (geneExtension.prerequisiteSets != null)
                    {
                        return ValidationDescription(pawn, geneExtension.prerequisiteSets);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Caught Exception in PrerequisiteValidator: {e.Message}\n{e.StackTrace}");
            }
            return "";
        }

        public static bool SetIsValid(Pawn pawn, List<PrerequisiteSet> prerequisiteSets) =>
            ValidationDescription(pawn, prerequisiteSets) == "";

        public static string ValidationDescription(Pawn pawn, List<PrerequisiteSet> prerequisiteSets)
        {
            if (prerequisiteSets == null || prerequisiteSets.Count == 0)
            {
                return "";
            }
            try
            {
                List<Gene> otherGenes = pawn.genes.GenesListForReading.Where(x => x.overriddenByGene == null).ToList();
                foreach (var prerequisiteSet in prerequisiteSets)
                {
                    if (prerequisiteSet.prerequisites != null)
                    {
                        bool result = false;
                        switch (prerequisiteSet.type)
                        {
                            case PrerequisiteSet.PrerequisiteType.AnyOf:
                                result = prerequisiteSet.prerequisites.Any(geneName => otherGenes.Any(y => y.def.defName == geneName));
                                if (!result)
                                {
                                    return "BS_PrerequisitesNotMetAnyOf".Translate($"{string.Join(", ", prerequisiteSet.prerequisites.Select(GeneDefLabelDefName))}");
                                }
                                break;
                            case PrerequisiteSet.PrerequisiteType.AllOf:
                                int matches = prerequisiteSet.prerequisites.Count(geneName => otherGenes.Any(y => y.def.defName == geneName));
                                float percentage = (float)matches / prerequisiteSet.prerequisites.Count;
                                result = percentage >= prerequisiteSet.allOfPerecntage;
                                if (!result)
                                {
                                    return "BS_PrerequisitesNotMetAllOf".Translate($"{string.Join(", ", prerequisiteSet.prerequisites.Select(GeneDefLabelDefName))}");
                                }
                                break;
                            case PrerequisiteSet.PrerequisiteType.NoneOf:
                                int bannedMatches = prerequisiteSet.prerequisites.Count(geneName => otherGenes.Any(y => y.def.defName == geneName));
                                float bannedPercentage = (float)bannedMatches / prerequisiteSet.prerequisites.Count;
                                result = bannedPercentage <= prerequisiteSet.noneOfPercentage;
                                if (!result)
                                {
                                    var bannedGenesPresent = prerequisiteSet.prerequisites.Where(geneName => otherGenes.Any(y => y.def.defName == geneName)).ToList();
                                    return "BS_PrerequisitesNotMetNoneOf".Translate($"{string.Join(", ", bannedGenesPresent.Select(GeneDefLabelDefName))}");
                                }
                                break;
                        }
                        if (!result)
                        {
                            return "";
                        }
                    }
                }
                return "";
            }
            catch (Exception e)
            {
                Log.Error($"Caught Exception in PrerequisiteValidator.ValidationDescription: {e.Message}\n{e.StackTrace}");
                return "ERROR";
            }
        }
    }
}
