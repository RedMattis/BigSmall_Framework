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
    public partial class Helpers
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
            //if (pawn.genes == null) return result;

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

        public static List<Gene> GetGeneByName(Pawn pawn, string geneName)
        {
            List<Gene> result = new List<Gene>();
            //if (pawn.genes == null) return result;

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

        public static List<Gene> GetAllActiveGenes(Pawn pawn)
        {
            List<Gene> result = new List<Gene>();
            //if (pawn.genes == null) return result;

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

        public static List<Gene> GetAllActiveEndoGenes(Pawn pawn)
        {
            List<Gene> result = new List<Gene>();
            //if (pawn.genes == null) return result;

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
            //if (pawn.genes == null) return result;

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

        public static List<Gene> GetAllInactiveGenes(Pawn pawn)
        {
            List<Gene> result = new List<Gene>();
            //if (pawn.genes == null) return result;

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
    }
}
