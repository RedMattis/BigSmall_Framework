using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BetterPrerequisites
{
    public static class GeneSuppressorManager
    {
        // Dictionary of supressed genes and the genes they are supressed by
        //public static Dictionary<Pawn, Dictionary<string, List<string>>> supressedGenesPerPawn_Gene = new Dictionary<Pawn, Dictionary<string, List<string>>>();

        public static Dictionary<Pawn, Dictionary<string, List<string>>> supressedGenesPerPawn_Hediff = new Dictionary<Pawn, Dictionary<string, List<string>>>();

        public static Dictionary<string, (long time, bool state)> cache = new Dictionary<string, (long time, bool state)>();
        
        public static void TryAddSuppressor(Hediff supresserHediff, Pawn pawn)
        {
            try
            {
                HediffDef supresserdef = supresserHediff.def;
                if (supresserdef.HasModExtension<GeneSuppressor_Hediff>())
                {
                    var geneExtension = supresserdef.GetModExtension<GeneSuppressor_Hediff>();
                    if (!supressedGenesPerPawn_Hediff.ContainsKey(pawn))
                    {
                        supressedGenesPerPawn_Hediff.Add(pawn, new Dictionary<string, List<string>>());
                    }
                    var supressedGenes = supressedGenesPerPawn_Hediff[pawn];
                    foreach (string supressedGene in geneExtension.supressedGenes)
                    {
                        if (!supressedGenes.ContainsKey(supressedGene))
                        {
                            supressedGenes.Add(supressedGene, new List<string>() { supresserdef.defName });
                        }
                        // If it does exist check so the supresser is in the list of genes supressing.
                        else if (!supressedGenes[supressedGene].Contains(supresserdef.defName))
                        {
                            supressedGenes[supressedGene].Add(supresserdef.defName);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Log.Error("Error in PrerequisiteValidator: " + e.Message);
            }
        }

        public static bool IsSupressedByHediff(string geneDefName, Pawn pawn)
        {

            if (supressedGenesPerPawn_Hediff.ContainsKey(pawn))
            {
                var supressedGenes = supressedGenesPerPawn_Hediff[pawn];
                if (supressedGenes.ContainsKey(geneDefName))
                {

                    for (int i = supressedGenes[geneDefName].Count - 1; i >= 0; i--)
                    {
                        var supresser = supressedGenes[geneDefName][i];
                        if (supressedGenes[geneDefName].Any(suppressor => pawn.health.hediffSet.HasHediff(HediffDef.Named(supresser))))
                        {
                            return true;
                        }
                        else
                        {
                            //Log.Message($"No Hediffs of type {supresser} on pawn ({HediffDef.Named(supresser)})");
                            //foreach(var hediff in pawn.health.hediffSet.hediffs)
                            //{
                            //    Log.Message(hediff.def.defName);
                            //}
                            ////supressedGenes.Remove(geneDefName);
                        }
                    }
                }
   
            }
            return false;
        }

    }

    public class GeneSuppressor_Gene : DefModExtension
    {
        public List<string> supressedGenes;
    }

    public class GeneSuppressor_Hediff : DefModExtension
    {
        public List<string> supressedGenes;
    }
}
