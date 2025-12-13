using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static BigAndSmall.TransitioningHediffProps;

namespace BigAndSmall
{
    public class TransitioningHediffProps : DefModExtension
    {
        public class Trigger
        {
            public bool xenogene = false;
            public List<GeneDef> geneDefsToAdd = [];
            public List<GeneDef> geneDefsToRemove = [];
            public List<HediffDef> hediffsToAdd = [];
            public List<HediffDef> hediffsToRemove = [];
            public XenotypeDef xenoTypeToAdd = null;
            public XenotypeDef xenoTypeToReplace = null;
            public bool resurrect = false;
            public bool perfectResurrect = false; // Fixes missing parts too.
            public bool tryRestoreOriginal = false;
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

        public class GizmoTrigger
        {
            public string label;
            public string description;
            public string icon;
            public int cooldown = 300;
            public Trigger trigger;
        }

        public Trigger onHediffAdded = null;
        public Trigger onHediffRemoved = null;
        public List<SeverityTrigger> onSeverity = null;
        public ConditionalTrigger onStat = null;
        public ConditionalTrigger onStatRemoved = null;
        public GizmoTrigger onGizmo = null;
        public bool restoreOnRemove = false;
    }

    public class TransitioningHediff : HediffWithComps
    {
        public TransitioningHediffProps properties = null;
        bool? statWasActive = null;
        public XenotypeDef originalXenotypeDef = null;
        public List<GeneDef> originalXenoGenes = null;
        public List<GeneDef> originalEndoGenes = null;
        

        public int cooldownEndsOnTick = -1;

        private List<SeverityTrigger> SeverityTriggers { get; set; } = [];

        public override void ExposeData()
        {
            Scribe_Values.Look(ref statWasActive, "TransitioningHediffProps_StatWasActive");
            Scribe_Values.Look(ref cooldownEndsOnTick, "TransitioningHediffProps_CooldownEndsOnTick");
            Scribe_Defs.Look(ref originalXenotypeDef, "TransitioningHediffProps_OriginalXenotypeDef");
            Scribe_Collections.Look(ref originalXenoGenes, "TransitioningHediffProps_OriginalXenoGenes");
            Scribe_Collections.Look(ref originalEndoGenes, "TransitioningHediffProps_OriginalEndoGenes");
            
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

            SaveOriginal();

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
                    SeverityTriggers = [];
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
            if (properties.restoreOnRemove) TryRestoreOriginal();
        }

        public virtual void SaveOriginal()
        {
            originalXenotypeDef = pawn.genes.Xenotype;
            originalEndoGenes = pawn.genes.Endogenes.Select(g => g.def).ToList();
            originalXenoGenes = pawn.genes.Xenogenes.Select(g => g.def).ToList();
        }

        public virtual void TryRestoreOriginal()
        {
            if(originalXenotypeDef != null) pawn.genes.SetXenotype(originalXenotypeDef);
            if (!originalXenoGenes.NullOrEmpty())
            {
                foreach (GeneDef originalXenoGene in originalXenoGenes)
                {
                    pawn.genes.AddGene(originalXenoGene, true);
                }
            }

            if (!originalEndoGenes.NullOrEmpty())
            {
                foreach (GeneDef originalEndoGene in originalEndoGenes)
                {
                    pawn.genes.AddGene(originalEndoGene, false);
                }
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
            catch (Exception e)
            {
                Log.Error($"Error in TransitioningHediff.PostTick: {e.Message}\n{e.StackTrace}");
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
            
            if(properties.onGizmo == null) yield break;
            
            yield return new Command_ActionWithCooldown
            {
                defaultLabel = properties.onGizmo.label,
                defaultDesc = properties.onGizmo.description,
                icon = ContentFinder<Texture2D>.Get(properties.onGizmo.icon),
                action = () =>
                {
                    cooldownEndsOnTick = Find.TickManager.TicksGame + properties.onGizmo.cooldown;
                    DoEffects(properties.onGizmo.trigger);
                },
                cooldownPercentGetter = () =>
                {
                    int remainingTicks = cooldownEndsOnTick - Find.TickManager.TicksGame;

                    if (properties.onGizmo.cooldown <= 0 || cooldownEndsOnTick < 0 || remainingTicks <= 0)
                        return 1f;

                    float progress = 1f - (remainingTicks / (float)properties.onGizmo.cooldown);
                    return Mathf.Clamp01(progress);
                },
            };
        }

        private void DoEffects(Trigger trigger)
        {
            if (trigger.tryRestoreOriginal)
            {
                TryRestoreOriginal();
                return; // so we don't trigger the other effects
            }else if(trigger.perfectResurrect)
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
                hediffToRemove.TryRemoveAllOfType(pawn);
            }
        }

        
    }
}
