using BetterPrerequisites;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Verse;

namespace BigAndSmall
{
    public partial class GeneHelpers
    {
        public static MethodInfo Notify_GenesChanged_MethodInfo = null;
        /// <summary>
        /// Note that this does _not_ cover every case. See Pawn_GeneTracker.Removed, and Pawn_GeneTracker.Added.
        /// </summary>
        public static void NotifyGenesUpdated(Pawn pawn, GeneDef geneDef)
        {
            if (Notify_GenesChanged_MethodInfo == null)
            {
                Notify_GenesChanged_MethodInfo = typeof(Pawn_GeneTracker).GetMethod("Notify_GenesChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            Notify_GenesChanged_MethodInfo.Invoke(pawn.genes, new object[] { geneDef });

            //Log.Message($"Notified genes updated for {pawn.Name} with gene {geneDef.defName}.");
        }

        public static MethodInfo CheckForOverrides_MethodInfo = null;

        public static void RefreshAllGenes(Pawn pawn, List<Gene> genesAdded, List<Gene> genesRemoved)
        {
            if (pawn?.genes == null) return;
            BigSmall.performScaleCalculations = false;
            try
            {
                foreach (var rGene in genesRemoved)
                {
                    // Removes all abilities with missing genes.
                    if (rGene.def.abilities.NullOrEmpty() == false)
                    {
                        foreach (var ab in rGene.def.abilities)
                        {
                            pawn.abilities.RemoveAbility(ab);
                        }
                    }
                    // Remove all passions/skills with missing genes.
                    if (rGene.def.passionMod != null)
                    {
                        SkillRecord skill = pawn.skills.GetSkill(rGene.def.passionMod.skill);
                        skill.passion = rGene.NewPassionForOnRemoval(skill);
                    }
                }
                // Remove all traits with missing genes.
                for (int tIdx = pawn.story.traits.allTraits.Count - 1; tIdx >= 0; tIdx--)
                {
                    Trait trait = pawn.story.traits.allTraits[tIdx];
                    if (trait.sourceGene != null)
                    {
                        if (genesRemoved.Any(x => x.def == trait.sourceGene.def))
                        {
                            pawn.story.traits.RemoveTrait(trait);
                        }
                    }
                }

                CheckForOverrides(pawn);

                // Add all abilities from active genes.
                foreach (var aGene in genesAdded.Where(g=>g.Active))
                {
                    if (aGene.def.abilities.NullOrEmpty() == false)
                    {
                        foreach (var ab in aGene.def.abilities)
                        {
                            pawn.abilities.GainAbility(ab);
                        }
                    }
                    // Traits
                    if (!aGene.def.forcedTraits.NullOrEmpty() && pawn.story != null)
                    {
                        for (int j = 0; j < aGene.def.forcedTraits.Count; j++)
                        {
                            Trait trait = new(aGene.def.forcedTraits[j].def, aGene.def.forcedTraits[j].degree)
                            {
                                sourceGene = aGene
                            };
                            pawn.story.traits.GainTrait(trait, suppressConflicts: true);
                        }
                    }
                    // Add all passions/skills from active genes.
                    if (aGene.def.passionMod != null)
                    {
                        SkillRecord skill = pawn.skills.GetSkill(aGene.def.passionMod.skill);
                        aGene.passionPreAdd = skill.passion;
                        skill.passion = aGene.def.passionMod.NewPassionFor(skill);
                    }
                    pawn.story?.traits?.RecalculateSuppression();
                }
                foreach (var geneAdded in genesAdded)
                {
                    geneAdded.PostAdd();
                }
                if (genesAdded.Count > 0)
                {
                    foreach (var nGene in genesAdded.Where(g =>
                    g.def.skinIsHairColor || g.def.hairColorOverride != null || g.def.skinColorBase != null || g.def.skinColorOverride != null ||
                    g.def.bodyType != null || g.def.forcedHeadTypes != null || g.def.forcedHair != null || g.def.hairTagFilter != null ||
                    g.def.beardTagFilter != null || g.def.fur != null || g.def.RandomChosen || g.def.soundCall != null
                    ))
                    {
                        NotifyGenesUpdated(pawn, nGene.def);
                    }
                }
                ClearCachedGenes(pawn);
                // Add and remove a dummy gene to force updates from HAR; etc.
                var dummyGene = pawn.genes.AddGene(BSDefs.Robust, true);
                pawn.genes.RemoveGene(dummyGene);
            }
            finally
            {
                BigSmall.performScaleCalculations = true;
            }
            FastAcccess.GetCache(pawn, force: true);
        }

        private static void CheckForOverrides(Pawn pawn)
        {
            if (CheckForOverrides_MethodInfo == null)
            {
                CheckForOverrides_MethodInfo = typeof(Pawn_GeneTracker).GetMethod("CheckForOverrides", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            // Process Overrides.
            CheckForOverrides_MethodInfo.Invoke(pawn.genes, []);
        }

        public static List<Gene> GetActiveGeneByName(Pawn pawn, string geneName)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].Active && genes[i].def.defName == geneName)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static List<Gene> GetGeneByName(Pawn pawn, string geneName)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].def.defName == geneName)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static HashSet<Gene> GetAllActiveGenes(Pawn pawn)
        {
            HashSet<Gene> result = new();
            var genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].Active)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static HashSet<Gene> GetAllActiveOrRandomChosenGenes(Pawn pawn)
        {
            HashSet<Gene> result = new();
            var genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].def.randomChosen && genes[i].Active)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static List<Gene> GetAllGenes(Pawn pawn)
        {
            List<Gene> result = new();
            var genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return result;
            return genes;
        }

        public static HashSet<GeneDef> GetAllActiveGeneDefs(Pawn pawn)
        {
            HashSet<GeneDef> result = new();
            var genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].Active)
                {
                    result.Add(genes[i].def);
                }
            }
            return result;
        }

        public static List<Gene> GetAllInactiveGenes(Pawn pawn)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (!genes[i].Active)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static Hediff GetFirstHediffOfDefName(this HediffSet instance, string defName, bool mustBeVisible = false)
        {
            for (int i = 0; i < instance.hediffs.Count; i++)
            {
                if (instance.hediffs[i].def.defName == defName && (!mustBeVisible || instance.hediffs[i].Visible))
                {
                    return instance.hediffs[i];
                }
            }

            return null;
        }

        public static FieldInfo xenoTypeField = null;
        public static List<XenotypeChance> GetXenotypeChances(this PawnKindDef pawnKind)
        {
            if (pawnKind == null)
            {
                Log.Warning("Trying to GetXenotypeChances, but PawnKindDef is null");
                return new List<XenotypeChance>();
            }
            if (pawnKind.xenotypeSet == null)
            {
                //Log.Warning("Trying to GetXenotypeChances, but the PawnKindDef's XenotypeSet is null");
                return new List<XenotypeChance>();
            }

            if (xenoTypeField == null)
                xenoTypeField = AccessTools.Field(typeof(XenotypeSet), "xenotypeChances");
            if (xenoTypeField == null)
            {
                Log.Warning("Could not find xenotypeChances field in XenotypeSet");
                return new List<XenotypeChance>();
            }
            var result =  xenoTypeField.GetValue(pawnKind.xenotypeSet) as List<XenotypeChance>;
            return result;
        }

        public static XenotypeDef GetRandomXenotype(this List<XenotypeChance> xenoTypeChances)
        {
            // If the sum is less than 1. Add "Baseliner" to the list with a weight of 1 - sum
            if (xenoTypeChances.Sum(x => x.chance) < 1)
            {
                xenoTypeChances.Add(new XenotypeChance(XenotypeDefOf.Baseliner, 1 - xenoTypeChances.Sum(x => x.chance)));
            }

            return xenoTypeChances.RandomElementByWeight(x => x.chance).xenotype;
        }

        public static List<Gene> GetActiveGenesByNames(Pawn pawn, List<string> geneNames)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].Active && geneNames.Contains(genes[i].def.defName))
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static List<Gene> GetAllActiveEndoGenes(Pawn pawn)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn?.genes?.Endogenes;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].Active)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static List<Gene> GetAllActiveXenoGenes(Pawn pawn)
        {
            List<Gene> result = new List<Gene>();
            var genes = pawn?.genes?.Xenogenes;
            if (genes == null) return result;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i].Active)
                {
                    result.Add(genes[i]);
                }
            }
            return result;
        }

        public static Hediff GetHediffOnPawnByName(string name, Pawn pawn)
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(name);
            if (hediffDef == null)
            {
                Log.Error("Could not find hediff with name " + name);
                return null;
            }

            // Check if we already have the hediff
            if (pawn.health.hediffSet.HasHediff(hediffDef))
            {
                // Get the hediff we added
                return pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            }
            return null;

        }

        public static bool HasActiveGene(this Pawn pawn, GeneDef geneDef)
        {
            if (pawn.genes == null)
            {
                return false;
            }
            return pawn.genes.GetGene(geneDef)?.Active ?? false;
        }


        public static void RemoveRandomToMetabolism(int initialMet, List<GeneDef> newGenes, int minMet = -6, List<GeneDef> exclusionList = null)
        {
            if (exclusionList == null)
                exclusionList = new List<GeneDef>();
            int idx = 0;
            // Sum up the metabolism cost of the new genes
            while (newGenes.Sum(x => x.biostatMet) + initialMet < minMet || newGenes.Count <= 1 || idx > 200)
            {
                if (newGenes.Count == 1)
                    break;
                // Pick a random gene from the newGenes with a negative metabolism cost and remove it.
                var geneToRemove = newGenes.Where(x => x.biostatMet <= 1 && !exclusionList.Contains(x)).RandomElement();
                if (geneToRemove != null)
                {
                    newGenes.Remove(geneToRemove);
                }
                else
                {
                    break;
                }
                idx++;  // Ensure we don't get stuck in an infinite loop no matter what.
            }
        }

        public static void RemoveRandomToMetabolism(int initialMet, Pawn_GeneTracker genes, int minMet = -6, List<GeneDef> exclusionList = null)
        {
            if (exclusionList != null)
                exclusionList = new List<GeneDef>();
            int idx = 0;
            // Sum up the metabolism cost of the new genes
            while (genes.GenesListForReading.Where(x => x.Overridden == false).Sum(x => x.def.biostatMet) + initialMet < minMet || genes.GenesListForReading.Count <= 1 || idx > 200)
            {
                if (genes.GenesListForReading.Count == 1)
                    break;
                // Pick a random gene from the newGenes with a negative metabolism cost and remove it.
                var geneToRemove = genes.GenesListForReading.Where(x => x.def.biostatMet < 0 && !exclusionList.Contains(x.def)).RandomElement();
                if (geneToRemove != null)
                {
                    genes.RemoveGene(geneToRemove);
                }
                else
                {
                    break;
                }
                idx++;  // Ensure we don't get stuck in an infinite loop no matter what.
            }
        }

        public static void AddAllXenotypeGenes(Pawn pawn, XenotypeDef def, string name = null, bool xenogene = false)
        {
            pawn.genes.SetXenotypeDirect(def);
            foreach (var xenotypeGene in def.genes)
            {
                // Check if the pawn has the gene already
                if (pawn.genes.GenesListForReading.Any(x => x.def.defName == xenotypeGene.defName) == false)
                {
                    // If not, add the gene to the pawn
                    pawn.genes.AddGene(xenotypeGene, xenogene: xenogene);
                }
            }
            if (name != null)
                pawn.genes.xenotypeName = name;
        }

        private static FieldInfo cachedGenesField = null;
        public static void ClearCachedGenes(Pawn pawn)
        {
            var genes = pawn?.genes;
            if (genes == null) return;
            if (cachedGenesField == null)
                cachedGenesField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedGenes");
            // Set the field to null.
            cachedGenesField.SetValue(genes, null);
        }

        public static List<GeneExtension> GetActiveGeneExtensions(Pawn pawn)
        {
            var activeGenes = GetAllActiveGenes(pawn);
            if (activeGenes.NullOrEmpty()) return new List<GeneExtension>(); ;
            return activeGenes
                   .Where(x => x?.def?.modExtensions != null && x.def.modExtensions.Any(y => y.GetType() == typeof(GeneExtension)))?
                   .Select(x => x.def.GetModExtension<GeneExtension>()).ToList();
        }

        public static void ChangeXenotypeFast(Pawn pawn, XenotypeDef targetXenottype)
        {
            var allGenesBefore = pawn.genes.GenesListForReading.ToList();
            var activeGenesBefore = GetAllActiveGenes(pawn);
            var activeGeneDefsBefore = activeGenesBefore.Select(x => x.def).ToHashSet();
            bool sourceIsEndo = pawn.genes.Xenotype.inheritable;
            bool targetIsEndo = targetXenottype.inheritable;
            var xenoTypeGenes = pawn.genes.Xenotype.AllGenes.ToList();

            var currentXenoGenes = pawn.genes.Xenogenes.Select(x => x).ToList();
            if (sourceIsEndo || targetIsEndo)
            {
                pawn.genes.Endogenes.Clear();
            }
            pawn.genes.Xenogenes.Clear();

            foreach (var gDef in targetXenottype.genes)
            {
                if (targetIsEndo)
                {
                    pawn.genes.Endogenes.Add(GeneMaker.MakeGene(gDef, pawn));
                }
                else
                {
                    pawn.genes.Xenogenes.Add(GeneMaker.MakeGene(gDef, pawn));
                }
            }

            if (sourceIsEndo && targetIsEndo) // Re-add the xenogenes.
            {
                pawn.genes.Xenogenes.AddRange(currentXenoGenes);
            }

            // If a gene targeting the CURRENT xenotype exists, remove it.
            var allGenes = pawn.genes.GenesListForReading.ToList();
            var genesToRemove = allGenes.Where(x => x.def.modExtensions != null && x.def.modExtensions.Any(y => y.GetType() == typeof(GeneExtension) && (y as GeneExtension).metamorphTarget == pawn.genes.Xenotype)).ToList();
            foreach (var gene in genesToRemove)
            {
                Log.Warning($"Removing gene {gene.def.defName} from {pawn.Name} as it targets the current xenotype.");
                pawn.genes.RemoveGene(gene);
            }

            ClearCachedGenes(pawn);
            CheckForOverrides(pawn);

            //var activeGenes = GetAllActiveGenes(pawn);
            List<Gene> allGenesNow = pawn.genes.GenesListForReading.ToList();
            List<Gene> newGenes = allGenesNow.Where(n => !allGenesBefore.Any(b=> b.def == n.def)).ToList();

            List<Gene> removedGenes = allGenesBefore.Where(b => !allGenesNow.Any(n => n.def == b.def)).ToList();


            //Log.Message($"DEBUG: {pawn.Name}\nRemoved genes: {removedGenes.Join(x => x.def.defName, ", ")}.\nAdded genes: {newGenes.Join(x => x.def.defName, ", ")}\n");

            //Log.Message($"DEBUG: {pawn.Name}\nGenes Before: {allGenesBefore.Join(x => x.def.defName, ", ")}.\nGenes Now: {allGenesNow.Join(x => x.def.defName, ", ")}\n");

            //foreach (var geneDef in defsToNotifyChange.Where(x => activeGeneDefsBefore.Contains(x) == false))
            //{
            //    NotifyGenesUpdated(pawn, geneDef);
            //}

            //var allActiveGenesNow = GetAllActiveGenes(pawn);
            //// Go over all traits foced by a gene and check so the gene is still active.
            //for (int tIdx = pawn.story.traits.allTraits.Count - 1; tIdx >= 0; tIdx--)
            //{
            //    Trait trait = pawn.story.traits.allTraits[tIdx];
            //    if (trait.sourceGene != null)
            //    {
            //        if (!allActiveGenesNow.Contains(trait.sourceGene) || trait.sourceGene.Active == false)
            //        {
            //            pawn.story.traits.allTraits.Remove(trait);
            //        }
            //    }
            //}

            RefreshAllGenes(pawn, newGenes, removedGenes);
            pawn.genes.SetXenotypeDirect(targetXenottype);
        }
    }
}
