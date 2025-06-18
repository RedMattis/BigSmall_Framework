using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class DefTag : Def { }

    public class FacialAnimDisabler
    {
        public string headName = "NOT_";
        public string skinName = "NOT_";
        public string lidName = "NOT_";
        public string lidOptionsName = "NOT_";
        public string eyeballName = "NOT_";
        public string mouthName = "NOT_";
        public string browName = "NOT_";
    }

    public class ConsumeSoul
    {
        public float gainMultiplier = 1;
        public float? gainSkillMultiplier = null;
        public float exponentialFalloff = 2.5f;
    }

    public class HediffToBody
    {
        public HediffDef hediff;

        public List<ConditionalStatAffecter> conditionals;
    }

    public class HediffToBodyparts
    {
        public HediffDef hediff;

        public List<ConditionalStatAffecter> conditionals;

        public List<BodyPartDef> bodyparts = [];
    }

    public class GraphicPathPerBodyType
    {
        public BodyTypeDef bodyType;
        public string graphicPath;
    }

    public class LockedNeedClass
    {
        public NeedDef need;
        public float value;
        public bool minValue = false;

        public string GetLabel()
        {
            if (need == null) return "";
            return need.LabelCap + (minValue ? " Min" : "");
        }
    }

    public class TransformationGene
    {
        public List<string> genesRequired = [];
        public int genesRequiredMinCount = 1;
        public List<string> genesForbidden = [];
        public int genesForbiddenMinCount = 1;
        public List<string> genesToAdd = [];
        public string xenotypeToAdd = null;
        public List<string> genesToRemove = [];
        public bool removeSelfOnTrigger = true;

        public bool TryTransform(Pawn pawn)
        {
            if (CanTransform(pawn))
            {
                Gene parentGene = null;
                foreach(var gene in pawn.genes.GenesListForReading)
                {
                    if (gene.def.GetAllPawnExtensionsOnGene().Any(x => x.transformGene == this))
                    {
                        parentGene = gene;
                        break;
                    }
                }
                if (parentGene == null)
                {
                    Log.ErrorOnce($"[BigAndSmall] TransformationGene {this} tried to transform a pawn but no parent gene was found. This is likely a bug.", 123456789 ^ pawn.GetHashCode());
                    return false;
                }

                // Remove the parent gene. Without this we'd just keep calling this all the time.
                if (removeSelfOnTrigger) pawn?.genes?.RemoveGene(parentGene);

                // Check if parentGene is a xenogene
                bool xenoGene = pawn.genes.Xenogenes.Contains(parentGene);

                if (genesToRemove.Count > 0)
                {
                    foreach (var geneName in genesToRemove)
                    {
                        var genesToRemove = pawn?.genes?.GenesListForReading.Where(x => x.def.defName == geneName).ToList();
                        if (genesToRemove != null && genesToRemove.Any())
                        {
                            pawn?.genes?.RemoveGene(genesToRemove.First());
                        }
                    }
                }

                if (xenotypeToAdd != null)
                {
                    var xenotypeToAddDef = DefDatabase<XenotypeDef>.GetNamed(xenotypeToAdd, errorOnFail: false);
                    if (xenotypeToAddDef != null)
                    {
                        pawn.genes.xenotype = xenotypeToAddDef;
                        pawn.genes.xenotypeName = xenotypeToAddDef.LabelCap;
                        pawn.genes.iconDef = null;
                        for (int i = 0; i < xenotypeToAddDef.genes.Count; i++)
                        {
                            pawn.genes.AddGene(xenotypeToAddDef.genes[i], !xenotypeToAddDef.inheritable);
                        }
                    }
                }
                if (genesToAdd.Count > 0)
                {
                    foreach (var geneName in genesToAdd)
                    {
                        var gene = DefDatabase<GeneDef>.GetNamed(geneName, errorOnFail: false);
                        if (gene != null)
                        {
                            pawn?.genes?.AddGene(gene, xenoGene);
                        }
                    }
                }

                return true;
            }
            return false;
        }

        private bool CanTransform(Pawn pawn)
        {
            var activeGeneNames = GeneHelpers.GetAllActiveGenes(pawn).Select(x=>x.def.defName);
            if (genesRequired.Count > 0)
            {
                int countFound = genesRequired.Where(x => activeGeneNames.Contains(x)).Count();
                if (countFound < genesRequiredMinCount)
                {
                    return false;
                }
            }
            if (genesForbidden.Count > 0)
            {
                int countFound = genesForbidden.Where(x => activeGeneNames.Contains(x)).Count();
                if (countFound >= genesForbiddenMinCount)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
