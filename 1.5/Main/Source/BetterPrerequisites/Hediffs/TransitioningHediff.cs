using BetterPrerequisites;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static BigAndSmall.TransitioningHediffProps;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    public class TransitioningHediffProps : DefModExtension
    {
        public class Trigger
        {
            public bool xenogene = false;
            public List<GeneDef> geneDefsToAdd = new List<GeneDef>();
            public List<GeneDef> geneDefsToRemove = new List<GeneDef>();
            public List<HediffDef> hediffsToAdd = new List<HediffDef>();
            public List<HediffDef> hediffsToRemove = new List<HediffDef>();
            public XenotypeDef xenoTypeToAdd = null;
            public XenotypeDef xenoTypeToReplace = null;
            public bool resurrect = false;
            public bool perfectResurrect = false; // Fixes missing parts too.
        }

        public class ConditionalTrigger
        {
            public List<ConditionalStatAffecter> conditionals;
            public Trigger trigger;
        }

        public class SeverityTrigger
        {
            public float severity;
            public Trigger trigger;
        }

        public Trigger onHediffAdded = null;
        public Trigger onHediffRemoved = null;
        public List<SeverityTrigger> onSeverity = null;
        public ConditionalTrigger onStat = null;
        public ConditionalTrigger onStatRemoved = null;
    }

    public class TransitioningHediff : HediffWithComps
    {
        public TransitioningHediffProps properties = null;
        bool? statWasActive = null;

        private List<SeverityTrigger> SeverityTriggers { get; set; } = new List<SeverityTrigger>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref statWasActive, "TransitioningHediffProps_StatWasActive");
            base.ExposeData();

            TrySetup();

            // Remove already triggered triggers.
            for (int i = SeverityTriggers.Count - 1; i >= 0; i--)
            {
                SeverityTrigger trigger = SeverityTriggers[i];
                if (trigger.severity < Severity)
                {
                    SeverityTriggers.Remove(trigger);
                }
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            TrySetup();
            var hAdded = properties.onHediffAdded;
            if (properties.onHediffAdded != null)
            {
                DoEffects(hAdded);
            }

        }

        private void TrySetup()
        {
            if (properties == null)
            {
                properties = def.GetModExtension<TransitioningHediffProps>();
                if (properties == null)
                {
                    // Raise error
                    throw (new Exception("TransitioningHediff class has no TransitioningHediffProps. It needs to be added to the XML."));
                }


                if (properties.onSeverity != null)
                {
                    SeverityTriggers = properties.onSeverity.Where(x => true).ToList();
                }
                else
                {
                    SeverityTriggers = new List<SeverityTrigger>();
                }
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            TrySetup();
            var hRemoved = properties.onHediffRemoved;
            if (properties.onHediffAdded != null)
            {
                DoEffects(hRemoved);
            }
        }

        public override void PostTick()
        {
            try
            {
                base.PostTick();
            
                TrySetup();
                
                if (properties.onSeverity != null)
                {
                    for (int i = SeverityTriggers.Count - 1; i >= 0; i--)
                    {
                        SeverityTrigger severityDef = SeverityTriggers[i];
                        if (Severity >= severityDef.severity)
                        {
                            DoEffects(severityDef.trigger);
                            SeverityTriggers.Remove(severityDef);
                        }
                    }
                }
                bool? onStat = null;
                if (properties.onStat != null || properties.onStatRemoved != null)
                {
                    onStat = ConditionalManager.TestConditionals(pawn, properties.onStat.conditionals);
                }

                // Make sure they only trigger when switching, not continiously as long as the condition is true.
                if (properties.onStat != null && statWasActive != true)
                {
                    if (onStat == true) DoEffects(properties.onStat.trigger);
                }
                if (properties.onStatRemoved != null && statWasActive != false)
                {
                    if (onStat == false) DoEffects(properties.onStat.trigger);
                }
                statWasActive = onStat;
                //TestConditionals
            }
            catch
            {
                throw;
            }
        }

        private void DoEffects(TransitioningHediffProps.Trigger trigger)
        {
            if(trigger.perfectResurrect)
            {
                ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams
                {
                    restoreMissingParts = true,
                });
            }
            else if (trigger.resurrect)
            {
                ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams
                {
                    restoreMissingParts = false,
                });
            }
            else
            if (trigger.xenoTypeToAdd != null)
            {
                GeneHelpers.AddAllXenotypeGenes(pawn, trigger.xenoTypeToAdd, trigger.xenoTypeToAdd.label, xenogene:trigger.xenogene);

            }
            else if (trigger.xenoTypeToReplace!= null)
            {
                pawn.genes.SetXenotype(trigger.xenoTypeToAdd);
            }

            foreach (var gene in trigger.geneDefsToAdd)
            {
                if (pawn.genes.GenesListForReading.Any(x => x.def.defName == gene.defName)) { continue; }
                pawn.genes.AddGene(gene, trigger.xenogene);
            }
            foreach (var gene in trigger.geneDefsToRemove)
            {
                // Select all genes with the same defname
                var toRemove = pawn.genes.GenesListForReading.Where(x => x.def.defName == gene.defName);
                foreach (var gToRemove in toRemove)
                {
                    pawn.genes.RemoveGene(gToRemove);
                }
            }
            foreach(var hediffToAdd in trigger.hediffsToAdd)
            {
                pawn.health.AddHediff(hediffToAdd);
            }
            foreach (var hediffToRemove in trigger.hediffsToRemove)
            {
                GeneEffectManager.RemoveHediffByName(pawn, hediffToRemove.defName);
            }
        }

        
    }
}
