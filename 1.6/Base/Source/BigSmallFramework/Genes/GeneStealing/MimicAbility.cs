using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

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
        public List<GeneDef> genesToRetain = [];
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
            var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
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
                pawn.genes.Notify_GenesChanged(gene.def);
            }

            // Get cache
            HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
        }
    }

    public class CompPropertiesMimic : CompProperties_AbilityEffect
    {
        public List<GeneDef> genesToRetain = [];
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

        public static void DoMimic(Pawn pawn, Corpse corpse, List<GeneDef> genesToRetain, bool spawnGibblets=true, bool addCorpseGenes=true, bool addCorpseRace=true)
        {
            CompProperticesMimicOffEffect.EndMimicry(pawn, genesToRetain);

            var target = corpse.InnerPawn;
            if (target == null)
            {
                Log.Warning($"Target {corpse} is not a pawn");
                return;
            }
            if (addCorpseGenes && target.genes != null)
            {
                // Add all active genes from the corpse
                var activeGenes = GeneHelpers.GetAllActiveGenes(corpse?.InnerPawn);
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
                    var sizeTrait = target.story.traits.allTraits.Where(trait => sizeTraitDefNames.Contains(trait.def.defName));
                    // Add them to the pawn.
                    foreach (var trait in sizeTrait)
                    {
                        pawn.story.traits.GainTrait(trait);
                    }
                }
                catch (Exception e)
                {
                    // Log an error if we fail to transfer size traits
                    Log.Error($"Error transferring size traits for {pawn}: {e.Message}\n{e.StackTrace}");
                }
            }
            if (target.def?.race.Humanlike == true)
            {
                // Get/Set body-type and gender of corpse
                pawn.story.bodyType = target.story.bodyType;
                pawn.gender = target.gender;
                pawn.story.hairDef = target.story.hairDef;
                pawn.story.headType = target.story.headType;
            }

            if (addCorpseRace)
            {
                // If the race is different from the pawn, set the race to that of the target.
                var currentRace = pawn?.def;
                var targetRace = target.def;

                if (!targetRace.race.Humanlike && HumanlikeAnimals.HumanLikeAnimalFor(targetRace) is ThingDef sapientAnimal)
                {
                    targetRace = sapientAnimal;
                }
                if (targetRace.race.Humanlike)
                {
                    RaceMorpher.SwapThingDef(pawn, targetRace, state: true, targetPriority: 999, force: true, permitFusion: false);
                }
            }

            // Move the pawn to the corpse's position
            pawn.Position = corpse.Position;

            // Remove all the apparel on the corpse
            target.apparel?.DropAll(pawn.Position, forbid: true);

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
