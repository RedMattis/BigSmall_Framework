using BetterPrerequisites;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
                            Trait trait = new Trait(gene.def.forcedTraits[j].def, gene.def.forcedTraits[j].degree);
                            trait.sourceGene = gene;
                            pawn.story.traits.GainTrait(trait, suppressConflicts: true);
                        }
                    }
                }
            }
        }

        private static bool? harActive = null;
        private static MethodInfo headTypeFilterMethod = null;
        public static bool canRefreshGeneEffects = false;

        public static bool RefreshGeneEffects(Gene __instance, bool activate, PawnExtension geneExt = null)
        {
            //if (__instance.pawn == null || !__instance.pawn.Spawned)
            //{
            //    return false;
            //}

            if (canRefreshGeneEffects == true && __instance is PGene pGene && __instance?.pawn != null)
            {
                canRefreshGeneEffects = false;
                bool conditionalsValid = ConditionalManager.TestConditionals(pGene);
                bool prerequisitesValid = PrerequisiteValidator.Validate(pGene.def, __instance.pawn);
                if (!conditionalsValid || !prerequisitesValid)
                {
                    activate = false;
                }
                canRefreshGeneEffects = true;
            }


            bool changeMade = false;

            var gene = __instance;

            PawnExtension extension = geneExt ?? gene.def.GetModExtension<PawnExtension>();
            if (extension == null) return false;
            try
            {
                AddHediffToPawn(activate, ref changeMade, gene, extension);
                AddHediffToPart(activate, ref changeMade, gene, extension);

            }
            catch (Exception ex)
            {
                Log.Error($"Error in RefreshGeneEffects: {ex.Message} {ex.StackTrace}");
            }
            return changeMade;
        }

        private static void AddHediffToPart(bool activate, ref bool changeMade, Gene gene, PawnExtension extension)
        {
            if (extension.applyPartHediff != null)
            {
                HashSet<BodyPartRecord> notMissingParts = gene.pawn.health.hediffSet.GetNotMissingParts().ToHashSet();
                foreach (HediffToBodyparts item in extension.applyPartHediff)
                {
                    //int num = 0;
                    if (activate && ConditionalManager.TestConditionals(gene, item.conditionals))
                    {
                        foreach (BodyPartDef bodypart in item.bodyparts)
                        {
                            if (!gene.pawn.RaceProps.body.GetPartsWithDef(bodypart).EnumerableNullOrEmpty()) //  && num <= gene.pawn.RaceProps.body.GetPartsWithDef(bodypart).Count
                            {
                                var allParts = gene.pawn.RaceProps.body.GetPartsWithDef(bodypart).ToArray();
                                foreach (var part in allParts)
                                {
                                    // Use hediffset to check if the part exists
                                    if (notMissingParts.Contains(part))
                                    {
                                        if (part != null && item.hediff != null)
                                        {
                                            // If the pawn already has the hediff on that part, abort.
                                            // Get all hediffs on pawn
                                            var hediffs = gene.pawn.health.hediffSet.hediffs;
                                            // Get all hediffs on the part
                                            var hediffsOnPart = hediffs.Where(x => x.Part == part);
                                            // Check if any of the hediffs on the part is the same as the one we want to add
                                            if (hediffsOnPart.Any(x => x.def == item.hediff))
                                            {
                                                continue;
                                            }

                                            changeMade = true;
                                            gene.pawn.health.AddHediff(item.hediff, part);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (gene.pawn.health.hediffSet.GetFirstHediffOfDefName(item.hediff.defName) != null)
                        {
                            bool found = false;
                            var otherGenes = GeneHelpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene);
                            if (otherGenes.Count() == 0) continue;
                            foreach (var otherGene in otherGenes.Select(x => x.def.GetModExtension<PawnExtension>()).Where(x => x != null))
                            {
                                if (otherGene.applyPartHediff != null)
                                {
                                    foreach (var otherItem in otherGene.applyPartHediff)
                                    {
                                        if (otherItem.hediff.defName == item.hediff.defName)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (found)
                            {
                                continue;
                            }

                            RemoveHediffByName(gene.pawn, item.hediff.defName);
                            changeMade = true;
                        }
                    }
                }
            }

            // Step through each hediff and check so its body-parts still exists. If not, remove it.
            for (int idx = gene.pawn.health.hediffSet.hediffs.Count - 1; idx >= 0; idx--)
            {
                Hediff hediff = gene.pawn.health.hediffSet.hediffs[idx];
                if (hediff.Part != null && !gene.pawn.health.hediffSet.GetNotMissingParts().Contains(hediff.Part))
                {
                    // Check so it isn't a Missing Part Hediff. If so we don't want to remove it.
                    if (hediff.def == HediffDefOf.MissingBodyPart)
                    {
                        continue;
                    }

                    gene.pawn.health.RemoveHediff(hediff);
                    changeMade = true;
                }
            }
        }

        private static void AddHediffToPawn(bool activate, ref bool changeMade, Gene gene, PawnExtension extension)
        {
            List<HediffToBody> toRemove = [];
            if (extension.applyBodyHediff != null)
            {
                foreach (HediffToBody item in extension.applyBodyHediff)
                {
                    if (item.hediff != null)
                    {
                        if (activate && ConditionalManager.TestConditionals(gene, item.conditionals))
                        {
                            // If the pawn does not have the hediff, add it.
                            if (gene.pawn.health.hediffSet.GetFirstHediffOfDefName(item.hediff.defName) == null)
                            {
                                gene.pawn.health.AddHediff(item.hediff);
                                changeMade = true;
                            }
                        }
                        else
                        {
                            if (gene.pawn.health.hediffSet.GetFirstHediffOfDefName(item.hediff.defName) != null)
                            {
                                // Check all other genes to see if they have the same hediff. If not, remove it.
                                bool found = false;
                                foreach (var otherGene in GeneHelpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene).Select(x => x.def.GetModExtension<PawnExtension>()).Where(x => x != null))
                                {
                                    if (otherGene.applyBodyHediff != null)
                                    {
                                        foreach (var otherItem in otherGene.applyBodyHediff)
                                        {
                                            if (otherItem.hediff.defName == item.hediff.defName)
                                            {
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (found)
                                {
                                    continue;
                                }
                                RemoveHediffByName(gene.pawn, item.hediff.defName);

                                //Log.Message($"DEBUG: Removed {item.hediff.defName} from {gene.pawn.Name}");
                                changeMade = true;
                            }
                        }
                    }
                    else
                    {
                        Log.Warning($"{gene.def.defName} on {gene.pawn} tried to grant a Hediff, but the Hediff was null. This is probably due to an optional \"MayRequire\" hediff, in which case it is harmless.");

                        // Remove it from the list so we don't get spammed with warnings.
                        toRemove.Add(item);
                    }
                }
            }
            if (toRemove.Count > 0)
            {
                for (int idx = toRemove.Count - 1; idx >= 0; idx--)
                {
                    HediffToBody item = toRemove[idx];
                    extension.applyBodyHediff.Remove(item);
                }
            }
        }

        public static void RemoveHediffByName(Pawn pawn, string hediffName)
        {
            var toRemove = new List<Hediff>();
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def.defName == hediffName)
                {
                    toRemove.Add(hediff);
                }
            }
            foreach (var hediff in toRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }

}
