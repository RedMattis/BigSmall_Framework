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
            foreach (var xenotypeGene in def.genes)
            {
                // Check if the pawn has the gene already
                if (pawn.genes.GenesListForReading.Any(x => x.def.defName == xenotypeGene.defName) == false)
                {
                    // If not, add the gene to the pawn
                    pawn.genes.AddGene(xenotypeGene, xenogene: xenogene);
                }
                if (name != null)
                    pawn.genes.xenotypeName = name;
            }
        }

        private static FieldInfo cachedGenesField = null;
        public static void ClearCachedGenes(Pawn pawn)
        {
            var genes = pawn?.genes;
            if (genes == null) return;
            if (cachedGenesField == null)
                cachedGenesField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedGenes");
            var cachedGenes = cachedGenesField.GetValue(pawn.genes) as List<Gene>;
            cachedGenes.Clear();
        }

        public static List<GeneExtension> GetActiveGeneExtensions(Pawn pawn)
        {
            var activeGenes = GetAllActiveGenes(pawn);
            if (activeGenes.NullOrEmpty()) return new List<GeneExtension>(); ;
            return activeGenes
                   .Where(x => x?.def?.modExtensions != null && x.def.modExtensions.Any(y => y.GetType() == typeof(GeneExtension)))?
                   .Select(x => x.def.GetModExtension<GeneExtension>()).ToList();
        }

        public static void ChangeXenotype(Pawn pawn, XenotypeDef targetXenottype)
        {
            bool sourceIsEndo = pawn.genes.Xenotype.inheritable;
            bool targetIsEndo = targetXenottype.inheritable;
            var xenoTypeGenes = pawn.genes.Xenotype.AllGenes.ToList();
            var defsToNotifyChange = pawn.genes.GenesListForReading.Select(x => x.def).ToList();

            var currentXenoGenes = pawn.genes.Xenogenes.Select(x => x).ToList();
            if (sourceIsEndo || targetIsEndo)
            {
                pawn.genes.Endogenes.Clear();
            }
            pawn.genes.Xenogenes.Clear();
            ClearCachedGenes(pawn);

            pawn.genes.SetXenotype(targetXenottype);

            if (sourceIsEndo && targetIsEndo) // Re-add the xenogenes.
            {
                pawn.genes.Xenogenes.AddRange(currentXenoGenes);
                defsToNotifyChange.AddRange(currentXenoGenes.Select(x => x.def));
            }
            foreach (var geneDef in defsToNotifyChange)
            {
                NotifyGenesUpdated(pawn, geneDef);
            }

            // If a gene targeting the CURRENT xenotype exists, remove it.
            var allGenes = pawn.genes.GenesListForReading.ToList();
            var genesToRemove = allGenes.Where(x => x.def.modExtensions != null && x.def.modExtensions.Any(y => y.GetType() == typeof(GeneExtension) && (y as GeneExtension).metamorphTarget == pawn.genes.Xenotype)).ToList();
            foreach (var gene in genesToRemove)
            {
                pawn.genes.RemoveGene(gene);
                pawn.story.traits.Notify_GeneRemoved(gene);
            }
        }
    }
}
