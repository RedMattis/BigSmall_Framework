using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterPrerequisites
{
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

        public float bodyPosOffset = 0f;
        public float headPosMultiplier = 0f;

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

    
}
