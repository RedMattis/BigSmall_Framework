using BigAndSmall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static bool Validate(GeneDef gene, Pawn pawn)
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
                                        break;
                                    case PrerequisiteSet.PrerequisiteType.AllOf:
                                        result = prerequisiteSet.prerequisites.All(geneName => otherGenes.Any(y => y.def.defName == geneName));
                                        break;
                                    case PrerequisiteSet.PrerequisiteType.NoneOf:
                                        result = prerequisiteSet.prerequisites.All(geneName => otherGenes.All(y => y.def.defName != geneName));
                                        break;
                                }
                                if (!result) return false;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //Log.Error("Error in PrerequisiteValidator: " + e.Message);
            }
            return true;
        }
    }
}
