using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public static class GeneEffectManager
    {
        private static Action<Pawn_GeneTracker, GeneDef> notifyGenesChangedDelegate = null;
        public static Action<Pawn_SkillTracker> dirtyAptitudesDelegate = null;
        public static void GainOrRemovePassion(bool disabled, Gene gene)
        {

            if (gene.def.passionMod != null && gene.def.prerequisite == null)
            {
                if (disabled)
                {
                    SkillRecord skill = gene.pawn.skills.GetSkill(gene.def.passionMod.skill);
                    skill.passion = gene.NewPassionForOnRemoval(skill);
                }
                else
                {
                    SkillRecord skill = gene.pawn.skills.GetSkill(gene.def.passionMod.skill);
                    gene.passionPreAdd = skill.passion;
                    skill.passion = gene.def.passionMod.NewPassionFor(skill);
                }
                int? test = null;
                test.GetValueOrDefault();
                // public void DirtyAptitudes()
                dirtyAptitudesDelegate ??= AccessTools.MethodDelegate<Action<Pawn_SkillTracker>>(AccessTools.Method(typeof(Pawn_SkillTracker), "DirtyAptitudes"));
                dirtyAptitudesDelegate(gene.pawn.skills);

                // Uh... This is questionable. Why are we notifying gene change here?
                //notifyGenesChangedDelegate ??= AccessTools.MethodDelegate<Action<Pawn_GeneTracker, GeneDef>>(AccessTools.Method(typeof(Pawn_GeneTracker), "Notify_GenesChanged"));
                //notifyGenesChangedDelegate(gene.pawn.genes, gene.def);
            }
        }

        public static void GainOrRemoveAbilities(bool disabled, Gene gene)
        {
            try
            {
                if (gene?.pawn?.abilities != null && gene?.def?.abilities != null)
                {
                    foreach (var abillity in gene.def.abilities)
                    {
                        if (disabled)
                        {
                            // Check so no enabled gene grants the abillity.
                            bool enabled = false;
                            foreach (var gene2 in GeneHelpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene))
                            {
                                if (gene2 != gene && gene2?.def?.abilities != null && gene2.def.abilities.Contains(abillity))
                                {
                                    enabled = true;
                                    break;
                                }
                            }
                            if (!enabled)
                            {
                                gene.pawn.abilities?.RemoveAbility(abillity);
                            }

                        }
                        else
                        {
                            gene.pawn.abilities?.GainAbility(abillity);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Error in GainOrRemoveAbilities: {ex.Message}");
            }
        }

        public static void ApplyForcedTraits(bool disabled, Gene gene)
        {
            var pawn = gene.pawn;
            if (!gene.def.forcedTraits.NullOrEmpty() && pawn.story != null)
            {
                foreach (var forcedTrait in gene.def.forcedTraits)
                {
                    if (disabled)
                    {
                        for (int j = 0; j < gene.def.forcedTraits.Count; j++)
                        {
                            Trait trait = new Trait(gene.def.forcedTraits[j].def, gene.def.forcedTraits[j].degree);
                            trait.sourceGene = gene;
                            pawn.story.traits.allTraits.RemoveAll((Trait tr) => tr.def == trait.def && tr.sourceGene == gene);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < gene.def.forcedTraits.Count; j++)
                        {
                            Trait trait = new(gene.def.forcedTraits[j].def, gene.def.forcedTraits[j].degree)
                            {
                                sourceGene = gene
                            };
                            pawn.story.traits.GainTrait(trait, suppressConflicts: true);
                        }
                    }
                }
            }
        }

    }

}
