using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BetterPrerequisites
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
                if (gene.HasModExtension<GenePrerequisites>())
                {
                    var geneExtension = gene.GetModExtension<GenePrerequisites>();
                    if (geneExtension.prerequisiteSets != null)
                    {
                        //List<Gene> otherGenes = Helpers.GetAllActiveGenes(pawn);
                        List <Gene> otherGenes = pawn.genes.GenesListForReading.Where(x=>x.overriddenByGene == null).ToList();
                        foreach (var prerequisiteSet in geneExtension.prerequisiteSets)
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
                                        result = prerequisiteSet.prerequisites.All(geneName => otherGenes.Any(y => y.def.defName == geneName));
                                        if (!result)
                                        {
                                            return "BS_PrerequisitesNotMetAllOf".Translate($"{string.Join(", ", prerequisiteSet.prerequisites.Select(GeneDefLabelDefName))}");
                                        }
                                        break;
                                    case PrerequisiteSet.PrerequisiteType.NoneOf:
                                        result = prerequisiteSet.prerequisites.All(geneName => otherGenes.All(y => y.def.defName != geneName));
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
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Caught Exception in PrerequisiteValidator: " + e.Message);
            }
            return "";
        }
    }
}
