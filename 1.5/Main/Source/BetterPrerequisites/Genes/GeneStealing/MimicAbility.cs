using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace BigAndSmall
{
    public class CompPropertiesIntegrateGenes : CompProperties_AbilityEffect
    {
        public CompPropertiesIntegrateGenes()
        {
            compClass = typeof(CompProperticesIntegrateGenesEffect);
        }
    }

    public class CompProperticesIntegrateGenesEffect : CompAbilityEffect
    {
        public new CompPropertiesMimicOff Props => (CompPropertiesMimicOff)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            // Remove all inactive genes.
            //var inactiveGenes = Helpers.GetAllInactiveGenes(parent.pawn);
            //foreach (var gene in inactiveGenes)
            //{
            //    parent.pawn?.genes?.RemoveGene(gene);
            //}

            Discombobulator.IntegrateGenes(parent.pawn, removeOverriden:true);
        }
    }


    public class CompPropertiesMimicOff : CompProperties_AbilityEffect
    {
        public List<GeneDef> genesToRetain = new List<GeneDef>();
        public bool spawnFilth = true;
        public CompPropertiesMimicOff()
        {
            compClass = typeof(CompProperticesMimicOffEffect);
        }
    }

    public class CompProperticesMimicOffEffect : CompAbilityEffect
    {
        public new CompPropertiesMimicOff Props => (CompPropertiesMimicOff)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            EndMimicry(parent.pawn, Props.genesToRetain, spawnFilith:Props.spawnFilth);
        }

        public static void EndMimicry(Pawn pawn, List<GeneDef> genesToRetain, bool spawnFilith=false)
        {
            if (spawnFilith)
            {
                Gibblets.SpawnGibblets(pawn, pawn.Position, pawn.Map, gibbletChance:0);
            }

            var previousBodyType = pawn.story.bodyType;

            var anyGene = pawn?.genes?.GenesListForReading?.FirstOrDefault();

            // Remove all xenogenes which are not in properties list.
            var genesToRemove = pawn?.genes?.Xenogenes?.Where(gene => !genesToRetain.Contains(gene.def)).ToList();
            if (genesToRemove != null)
            {
                foreach (var gene in genesToRemove)
                {
                    pawn?.genes?.RemoveGene(gene);
                }
            }
            //var geneDefsToRemove = pawn?.genes?.Xenogenes?.Where(gene => !genesToRetain.Contains(gene.def)).Select(x => x.def);
            //pawn?.genes?.Xenogenes.RemoveAll(gene => !genesToRetain.Contains(gene.def));

            // Check for a skin-color gene
            var activeGenes = Helpers.GetAllActiveGenes(pawn);
            var skinColorGenes = activeGenes.Where(gene => gene.def.skinColorBase != null).Select(x => x.def.skinColorBase);
            var skinColorOverrides = activeGenes.Where(gene => gene.def.skinColorOverride != null).Select(x => x.def.skinColorOverride);

            if (skinColorOverrides.Any())
            {
                pawn.story.skinColorOverride = skinColorOverrides.First().Value;
            }
            else if (skinColorGenes.Any())
            {
                pawn.story.skinColorOverride = null;
                pawn.story.SkinColorBase = skinColorGenes.First().Value;
            }

            pawn.story.bodyType = previousBodyType;

            // Notify genes changed on all genes, via reflection since Notify_GenesChanged is private.
            foreach (var gene in genesToRemove)
            {
                // Run method on each gene
                Helpers.NotifyGenesUpdated(pawn, gene.def);
            }

            // Get cache
            HumanoidPawnScaler.GetBSDict(pawn, forceRefresh: true);
        }
    }

    public class CompPropertiesMimic : CompProperties_AbilityEffect
    {
        public List<GeneDef> genesToRetain = new List<GeneDef>();
        public CompPropertiesMimic()
        {
            compClass = typeof(CompPropertiesMimicffect);
        }
    }

    public class CompPropertiesMimicffect : CompAbilityEffect
    {
        public new CompPropertiesMimic Props => (CompPropertiesMimic)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;
            var corpse = (Corpse)target.Thing;
            if (corpse == null)
            {
                Log.Warning($"Target {target.Thing} is not a corpse");
                return;
            }

            DoMimic(pawn, corpse, genesToRetain:Props.genesToRetain);
        }

        public static void DoMimic(Pawn pawn, Corpse corpse, List<GeneDef> genesToRetain, bool spawnGibblets=true, bool addCorpseGenes=true)
        {
            CompProperticesMimicOffEffect.EndMimicry(pawn, genesToRetain);

            if (addCorpseGenes)
            {
                // Add all active genes from the corpse
                var activeGenes = Helpers.GetAllActiveGenes(corpse?.InnerPawn);
                var genesToPick = activeGenes.Select(gene => gene.def).ToList();

                // Remove all xenogenes except those in genesToRetain
                pawn.genes.Xenogenes.RemoveAll(gene => !genesToRetain.Contains(gene.def));

                // Add all genes from the corpse
                foreach (var geneDef in genesToPick)
                {
                    pawn.genes.AddGene(geneDef, xenogene: true);
                }

                try
                {
                    // Remove the size traits from the pawn
                    var sizeTraitDefNames = new List<string> { "Gigantism", "Large", "Small", "Dwarfism" };
                    var traitsToRemove = pawn.story.traits.allTraits.Where(trait => sizeTraitDefNames.Contains(trait.def.defName)).ToList();
                    for (int idx = traitsToRemove.Count - 1; idx >= 0; idx--)
                    {
                        Trait trait = traitsToRemove[idx];
                        pawn.story.traits.RemoveTrait(trait);
                    }

                    // Check if the corpse has any size traits
                    var sizeTrait = corpse.InnerPawn.story.traits.allTraits.Where(trait => sizeTraitDefNames.Contains(trait.def.defName));
                    // Add them to the pawn.
                    foreach (var trait in sizeTrait)
                    {
                        pawn.story.traits.GainTrait(trait);
                    }
                }
                catch
                {
                    Log.Error($"Failed to transfer size traits for {pawn}");
                }
            }

            // Get/Set body-type and gender of corpse
            pawn.story.bodyType = corpse.InnerPawn.story.bodyType;
            pawn.gender = corpse.InnerPawn.gender;
            pawn.story.hairDef = corpse.InnerPawn.story.hairDef;
            pawn.story.headType = corpse.InnerPawn.story.headType;

            // Move the pawn to the corpse's position
            pawn.Position = corpse.Position;

            // Remove all the apparel on the corpse
            corpse.InnerPawn.apparel.DropAll(pawn.Position, forbid: true);

            if (spawnGibblets)
            {
                // Spread blood everywhere.
                Gibblets.SpawnGibblets(corpse.InnerPawn, pawn.Position, pawn.Map);
            }
        }

        public override void PostApplied(List<LocalTargetInfo> targets, Map map)
        {
            base.PostApplied(targets, map);
            foreach (var target in targets)
            {
                var corpse = (Corpse)target.Thing;
                corpse?.Destroy();
            }
            if (CompProperties_IncorporateEffect.RemoveGenesOverLimit(parent.pawn, -15))
            {
                Messages.Message("MessageMimicryGenesRemoved".Translate(parent.pawn.LabelShort), parent.pawn, MessageTypeDefOf.NegativeEvent);
            }
        }
    }

}
