using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace BigAndSmall
{
    public static partial class HarmonyPatches
    {
        private static bool HasSOS => ModsConfig.IsActive("kentington.saveourship2");
        public static bool HasVFE => ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");

        private static bool NotNull(params object[] input)
        {
            if (input.All(o => o != null))
            {
                return true;
            }

            Log.Message("Signature match not found");
            foreach (var obj in input)
            {
                if (obj is MemberInfo memberObj)
                {
                    Log.Message($"\tValid entry:{memberObj}");
                }
            }

            return false;
        }
    }

    public static class GameUtils
    {
        public static void UnhealingRessurection(Pawn pawn)
        {
            ////// Save Hediffs of all wounds on the pawn.
            List<(HediffDef, BodyPartRecord)> missingParts = new List<(HediffDef, BodyPartRecord)>();

            // Foreach hediff per body part
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_MissingPart)
                {
                    var missingPart = hediff as Hediff_MissingPart;
                    var part = missingPart.Part;
                    missingParts.Add((hediff.def, part));
                }
            }

            // Ressurect the pawn

            ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams
            {
                restoreMissingParts = false,
            });

            //////// Re-apply the wounds. Counting backwards
            /// Eeeey! This is supported by vanilla now!
            /// The below code is probably not needed anymore.
            //for (int i = missingParts.Count - 1; i >= 0; i--)
            //{
            //    // Get the current hediff
            //    var hediff = missingParts[i];

            //    if (!pawn.health.hediffSet.PartIsMissing(hediff.Item2))
            //    {
            //        // Add the hediff back to the pawn
            //        pawn.health.AddHediff(hediff.Item1, part: hediff.Item2);
            //    }
            //}
        }

        public static void AddAllXenotypeGenes(Pawn pawn, XenotypeDef def, string name=null, bool xenogene=false)
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

        public static FoodKind GetDiet(Pawn pawn)
        {
            var dietGenes = Helpers.GetActiveGenesByNames(pawn, new List<string> { "BS_Diet_Carnivore", "BS_Diet_Herbivore" });

            foreach (var gene in dietGenes)
            {
                if (gene.def.defName.Contains("Carnivore"))
                    return FoodKind.Meat;
                if (gene.def.defName.Contains("Herbivore"))
                    return FoodKind.NonMeat;
            }
            return FoodKind.Any;
        }
    }

    public static partial class Helpers
    {
        public static bool ApproximatelyEquals(this float f1, float f2, float tolerance = 0.01f)
        {
            return Math.Abs(f1 - f2) < tolerance;
        }

        public static List<Gene> GetActiveGenesByName(Pawn pawn, string geneName)
        {
            return GetActiveGenesByNames(pawn, new List<string> { geneName });
        }

        
    }
}
