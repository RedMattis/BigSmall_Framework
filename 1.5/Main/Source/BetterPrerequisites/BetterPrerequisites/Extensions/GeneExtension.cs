using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BetterPrerequisites
{
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

    public class GeneExtension : DefModExtension
    {
        //public HediffDef applyBodyHediff;
        public List<ConditionalStatAffecter> conditionals;
        public bool? invert;

        public List<HediffToBody> applyBodyHediff;
        public List<HediffToBodyparts> applyPartHediff;
        public List<GraphicPathPerBodyType> pathPerBodyType;

        public ThingDef thingDefSwap = null;
        public bool thingDefSwapOnlyIfSupressing = false;
        public List<ThingDef> thingDefsToSupress = new List<ThingDef>();
        public bool forceThingDefSwap = false;
        public List<GeneDef> hiddenGenes = new List<GeneDef>();
        public bool hiddenAddon = false;

        public TransformationGene transformGene = null;
        public SizeByAge sizeByAge = null;

        public float bodyPosOffset = 0f;
        public float headPosMultiplier = 0f;
        public bool preventDisfigurement = false;

        public Shader geneShader = null;
        public FacialAnimDisabler facialDisabler = null;

        public StringBuilder GetAllEffectorDescriptions()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (applyBodyHediff != null)
            {
                foreach (var hdiffToBody in applyBodyHediff)
                {
                    if (hdiffToBody.conditionals != null && hdiffToBody.hediff != null)
                    {
                        string hdiffLbl = hdiffToBody.hediff.label ?? hdiffToBody.hediff.defName;

                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(($"\"{hdiffLbl.CapitalizeFirst()}\" {"BS_ActiveIf".Translate()}:").Colorize(ColoredText.TipSectionTitleColor));
                        foreach (var conditional in hdiffToBody.conditionals)
                        {
                            stringBuilder.AppendLine($"• {conditional.Label}");
                        }
                    }
                }
            }
            if (applyPartHediff != null)
            {
                foreach (var hdiffToParts in applyPartHediff)
                {
                    if (hdiffToParts.conditionals != null && hdiffToParts.hediff != null)
                    {
                        string hdiffLbl = hdiffToParts.hediff.label ?? hdiffToParts.hediff.defName;

                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(($"\"{hdiffLbl.CapitalizeFirst()}\" {"BS_ActiveIf".Translate()}:").Colorize(ColoredText.TipSectionTitleColor));
                        foreach (var conditional in hdiffToParts.conditionals)
                        {
                            stringBuilder.AppendLine($"• {conditional.Label}");
                        }
                    }
                }
            }

            return stringBuilder;
        }
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

        public List<BodyPartDef> bodyparts;
    }

    public class GraphicPathPerBodyType
    {
        public BodyTypeDef bodyType;
        public string graphicPath;
    }

    public class TransformationGene
    {
        public List<string> genesRequired = new List<string>();
        public List<string> genesForbidden = new List<string>();
        public List<string> genesToAdd = new List<string>();
        public string xenotypeToAdd = null;
        public List<string> genesToRemove = new List<string>();

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
                        // Set pawn.genes.xenotype = xenotype; via reflection
                        var xenotypeField = typeof(Pawn_GeneTracker).GetField("xenotype", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        xenotypeField?.SetValue(pawn.genes, xenotypeToAddDef);
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

    public class SizeByAge
    {
        // Size of the pawn at the bottom of the range.
        public float minOffset = 0;
        // Size of the pawn at the top of the range.
        public float maxOffset = 0;
        // The float range
        public FloatRange range = new FloatRange(0, 0);

        public float GetSize(float? age)
        {
            if (age == null) return 0;
            return Mathf.Lerp(minOffset, maxOffset, range.InverseLerpThroughRange(age.Value));
        }
    }
}
