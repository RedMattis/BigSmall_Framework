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

    public class TransformationGene
    {
        public List<string> genesRequired = [];
        public List<string> genesForbidden = [];
        public List<string> genesToAdd = [];
        public string xenotypeToAdd = null;
        public List<string> genesToRemove = [];

        public bool TryTransform(Pawn pawn, Gene parentGene)
        {
            if (CanTransform(pawn))
            {
                // Remove the parent gene. Without this we'd just keep calling this all the time.
                pawn?.genes?.RemoveGene(parentGene);

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
            if (genesRequired.Count > 0)
            {
                foreach (var gene in genesRequired)
                {
                    if (!pawn?.genes?.GenesListForReading?.Any(x => x.def.defName == gene) == true)
                    {
                        return false;
                    }
                }
            }
            if (genesForbidden.Count > 0)
            {
                foreach (var gene in genesForbidden)
                {
                    if (pawn?.genes?.GenesListForReading?.Any(x => x.def.defName == gene) == true)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
