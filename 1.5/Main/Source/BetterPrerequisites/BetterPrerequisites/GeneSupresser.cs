using BigAndSmall;
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

        public static Dictionary<Pawn, Dictionary<GeneDef, List<HediffDef>>> supressedGenesPerPawn_Hediff = [];

        public static Dictionary<string, (long time, bool state)> cache = [];
        
        public static bool TryAddSuppressor(Hediff supresserHediff, Pawn pawn)
        {
            bool didAddSupressor = false;
            try
            {
                HediffDef supresserdef = supresserHediff.def;
                if (supresserHediff?.def?.GetModExtension<GeneSuppressor_Hediff>() is GeneSuppressor_Hediff supresser)
                {
                    if (!supressedGenesPerPawn_Hediff.ContainsKey(pawn))
                    {
                        supressedGenesPerPawn_Hediff.Add(pawn, []);
                    }

                    var supressedGenes = supressedGenesPerPawn_Hediff[pawn];
                    foreach (string supressedGeneName in supresser.supressedGenes)
                    {
                        if (DefDatabase<GeneDef>.GetNamed(supressedGeneName, errorOnFail: false) is GeneDef supressedGene)
                        {
                            if (!supressedGenes.ContainsKey(supressedGene))
                            {
                                supressedGenes.Add(supressedGene, [supresserdef]);
                            }
                            // If it does exist check so the supresser is in the list of genes supressing.
                            else if (!supressedGenes[supressedGene].Contains(supresserdef))
                            {
                                supressedGenes[supressedGene].Add(supresserdef);
                            }
                            didAddSupressor = true;
                        }
                    }
                    foreach(string supressedGeneCategory in supresser.supressedCategories)
                    {
                        foreach (GeneDef geneDef in DefDatabase<GeneDef>.AllDefs.Where(x => x.displayCategory?.defName == supressedGeneCategory))
                        {
                            if (!supressedGenes.ContainsKey(geneDef))
                            {
                                supressedGenes.Add(geneDef, [supresserdef]);
                            }
                            // If it does exist check so the supresser is in the list of genes supressing.
                            else if (!supressedGenes[geneDef].Contains(supresserdef))
                            {
                                supressedGenes[geneDef].Add(supresserdef);
                            }
                            didAddSupressor = true;
                        }
                    }
                    foreach (string supressedGeneTag in supresser.supressedExclusionTags)
                    {
                        foreach (GeneDef geneDef in DefDatabase<GeneDef>.AllDefs.Where(x => x.exclusionTags != null && x.exclusionTags.Contains(supressedGeneTag)))
                        {
                            if (!supressedGenes.ContainsKey(geneDef))
                            {
                                supressedGenes.Add(geneDef, [supresserdef]);
                            }
                            // If it does exist check so the supresser is in the list of genes supressing.
                            else if (!supressedGenes[geneDef].Contains(supresserdef))
                            {
                                supressedGenes[geneDef].Add(supresserdef);
                            }
                            didAddSupressor = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error in PrerequisiteValidator: " + e.Message);
            }
            return didAddSupressor;
        }

        public static bool IsSupressedByHediff(GeneDef geneDefName, Pawn pawn)
        {

            if (supressedGenesPerPawn_Hediff.ContainsKey(pawn))
            {
                var supressedGenes = supressedGenesPerPawn_Hediff[pawn];
                if (supressedGenes.ContainsKey(geneDefName))
                {

                    for (int i = supressedGenes[geneDefName].Count - 1; i >= 0; i--)
                    {
                        var supresser = supressedGenes[geneDefName][i];
                        if (supressedGenes[geneDefName].Any(suppressor => pawn.health.hediffSet.HasHediff(supresser)))
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
        public List<string> supressedGenes = [];
        public List<string> supressedExclusionTags = [];
        public List<string> supressedCategories = [];
    }
}
