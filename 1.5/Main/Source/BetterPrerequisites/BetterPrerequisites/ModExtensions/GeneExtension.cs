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
        public bool renderCacheOff = false;

        public List<HediffToBody> applyBodyHediff;
        public List<HediffToBodyparts> applyPartHediff;
        public List<GraphicPathPerBodyType> pathPerBodyType;

        public ThingDef thingDefSwap = null;
        public bool thingDefSwapOnlyIfSupressing = false;
        public List<ThingDef> thingDefsToSupress = new List<ThingDef>();
        public bool forceThingDefSwap = false;
        public List<GeneDef> hiddenGenes = new List<GeneDef>();

        public TransformationGene transformGene = null;
        public SimpleCurve sizeByAge = null;
        public SimpleCurve sizeByAgeMult = null;

        public List<int> babyBirthCount = null;
        public int? babyStartAge = null;

        public float bodyPosOffset = 0f;
        public float headPosMultiplier = 0f;
        public bool preventDisfigurement = false;
        public bool unarmedOnly = false;
        public bool canWalkOnCreep = false;

        public bool hideBody = false;
        public bool hideHead = false;

        public bool hideInXenotypeUI = false;

        public ConsumeSoul consumeSoulOnHit = null;

        public bool isDrone = false;

        // Metamorph Stuff.
        public XenotypeDef metamorphTarget = null;
        public XenotypeDef retromorphTarget = null;
        public int? metamorphAtAge = null;
        public int? retromorphUnderAge = null;
        public bool metamorphIfPregnant = false;
        public bool metamorphIfNight = false;
        public bool metamorphIfDay = false;

        // Locked Needs.
        public List<LockedNeedClass> lockedNeeds;
        //

        public Shader geneShader = null;
        public FacialAnimDisabler facialDisabler = null;

        public bool disableFacialAnimations = false;

        public float GetSizeFromSizeByAge(float? age)
        {
            if (sizeByAge == null || age == null) return 1f;
            if (age == null) return 0;
            return sizeByAge.Evaluate(age.Value);
        }

        public float GetSizeMultiplierFromSizeByAge(float? age)
        {
            if (sizeByAgeMult == null || age == null) return 1f;
            if (age == null) return 1;
            return sizeByAgeMult.Evaluate(age.Value);
        }

        public bool CanMorphDown => retromorphUnderAge != null;
        public bool CanMorphUp => metamorphAtAge != null || metamorphIfPregnant || metamorphIfNight || metamorphIfDay;
        public bool CanMorphAtAll => CanMorphDown || CanMorphUp;
        public bool HasMorphTarget => metamorphTarget != null || retromorphTarget != null;
        public bool MorphRelated => CanMorphAtAll || HasMorphTarget;

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

        public List<BodyPartDef> bodyparts = new List<BodyPartDef>();
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
}
