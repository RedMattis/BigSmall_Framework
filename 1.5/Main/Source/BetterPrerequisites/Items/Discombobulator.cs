using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;


namespace BigAndSmall
{
    public static class Discombobulator
    {
        public static void Discombobulate(Pawn pawn)
        {
            var previousGenes = pawn.genes.Xenogenes.ToList();
            var invalidTags = new List<string> { "VU_", "BS_Corrupted", "BS_Damaged_Genes", "BS_Xenolocked", "Titan" };
            var exclusionTags = new List<string> { "BS_Pilotable" };
            List<GeneDef> allValidGenes = GetValidGenes(pawn, invalidTags, exclusionTags);

            int geneCount = previousGenes.Count;

            geneCount = geneCount < 3 ? 3 : geneCount;

            // Get Metabolic Efficiency
            int met = Helpers.GetAllActiveEndoGenes(pawn).Sum(x => x.def.biostatMet);

            if (met > 0)
            {
                geneCount += Rand.Range(0, 4);
            }
            else
            {
                geneCount += Rand.Range(-2, 4);
            }

            // Pick a number of new genes equal to the count.
            var newGenes = new HashSet<GeneDef>();
            for (int i = 0; i < geneCount; i++)
            {
                // Pick a random gene from the list of valid genes.
                var newGene = allValidGenes.RandomElementByWeight(x => x.selectionWeight);
                newGenes.Add(newGene);
            }

            // If none of the genes contain the word "frame" add a gene with that word.
            if (newGenes.Any(x => x.defName.Contains("Frame")) == false)
            {
                // 40% chance
                if (Rand.Chance(0.4f))
                {
                    // Get a gene from the valid genes containing the word frame
                    var frameGene = allValidGenes.Where(x => x.defName.Contains("Frame"))?.RandomElement();
                    if (frameGene != null)
                        newGenes.Add(frameGene);
                }
            }

            Helpers.RemoveRandomToMetabolism(met, newGenes.ToList());

            // Remove the previous xenogenes from the pawn
            foreach (var gene in previousGenes)
            {
                pawn.genes.RemoveGene(gene);
            }
            // Add the new genes to the pawn
            foreach (var gene in newGenes)
            {
                pawn.genes.AddGene(gene, xenogene: true);
            }

            // Add xenogermination coma
            var coma = HediffMaker.MakeHediff(HediffDefOf.XenogerminationComa, pawn);
            pawn.health.AddHediff(coma);
        }

        private static List<GeneDef> GetValidGenes(Pawn pawn, List<string> invalidTags, List<string> exclusionTags)
        {
            var allValidGenes = DefDatabase<GeneDef>.AllDefsListForReading.Where(x => !invalidTags.Any(y => x.defName.StartsWith(y))).ToList();
            allValidGenes = allValidGenes.Where(x => !exclusionTags.Any(y => x.exclusionTags.Contains(y))).ToList();
            allValidGenes = allValidGenes.Where(x => x.canGenerateInGeneSet).ToList();

            // Remove all eye genes. It is boring to get a dozen eye genes, and they can render badly depending on modlist.
            allValidGenes = allValidGenes.Where(x => !x.defName.ToLower().Contains("eye")).ToList();

            // Remove genes with prerequisites the pawn doesn't have.
            allValidGenes = allValidGenes.Where(x => x.prerequisite == null || pawn.genes.GenesListForReading.Select(y=>y.def).Contains(x.prerequisite)).ToList();
            return allValidGenes;
        }

        public static void IntegrateGenes(Pawn pawn, bool removeOverriden=true)
        {
            var xenogenes = pawn.genes.Xenogenes.ToList();

            if (removeOverriden)
            {
                var inactiveGenes = Helpers.GetAllInactiveGenes(pawn);
                foreach (var gene in inactiveGenes)
                {
                    pawn.genes.RemoveGene(gene);
                }
            }

            // remove all xenogenes from the pawn
            foreach (var gene in xenogenes)
            {
                pawn.genes.RemoveGene(gene);
            }

            // add them as endogenes
            foreach (var gene in xenogenes)
            {
                pawn.genes.AddGene(gene.def, xenogene: false);
            }
        }

        public static void XenoCopy(Pawn pawn)
        {
            var endoGenes = pawn.genes.Endogenes.ToList();

            // Keep 2/3 of the genes, chosen randomly.
            var genesToTransfer = endoGenes.Where(x => Rand.Chance(0.66f) && x.Active).ToList();

            // Add the genesToTransfer to the xenogenes if the aren't already there.
            foreach (var gene in genesToTransfer)
            {
                if (!pawn.genes.Xenogenes.Any(x=>x.def.defName == gene.def.defName))
                {
                    pawn.genes.AddGene(gene.def, xenogene: true);
                }
            }
        }

        public static void CreateXenogerm(Pawn pawn, bool archite=false)
        {
            // spawn a xenogerm item containing a list of genes of thingClass Xenogerm
            Xenogerm xenogerm = (Xenogerm)ThingMaker.MakeThing(ThingDefOf.Xenogerm);
            xenogerm.Initialize(new List<Genepack>(), pawn.genes.xenotypeName, pawn.genes.iconDef);

            // Get pawn's xenogenes
            var xenogenes = pawn.genes.Xenogenes.ToList();

            // Add the xenogenes to the xenogerm
            foreach (var gene in xenogenes)
            {

                if (!archite && gene.def.biostatArc > 0)
                {
                    continue;
                }

                xenogerm.GeneSet.AddGene(gene.def);
            }

            //xenogerm.xenotypeName = pawn.genes.xenotypeName;
            //xenogerm.iconDef = pawn.genes.iconDef;
            GenPlace.TryPlaceThing(xenogerm, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }
    }

}