using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_XenogermCreator : CompProperties
    {
        public bool archite = false;
        public bool endogenes = false;
        public bool xenogenes = false;
        public bool inactivegenes = true;

        public CompProperties_XenogermCreator()
        {
            compClass = typeof(CompTargetEffect_CreateXenogerm);
        }
    }

    public class CompTargetEffect_CreateXenogerm : CompTargetEffect
    {
        public CompProperties_XenogermCreator Props => (CompProperties_XenogermCreator)props;
        public override void DoEffectOn(Pawn _, Thing target)
        {
            if (target is Pawn pawn)
            {
                CreateXenogerm(pawn, Props.archite, Props.endogenes, Props.xenogenes, Props.inactivegenes);
            }
        }

        public static void CreateXenogerm(Pawn pawn, bool archite, bool endoGenes, bool xenoGenes, bool inactive)
        {
            // spawn a xenogerm item containing a list of genes of thingClass Xenogerm
            Xenogerm xenogerm = (Xenogerm)ThingMaker.MakeThing(ThingDefOf.Xenogerm);
            xenogerm.Initialize([], pawn.genes.xenotypeName, pawn.genes.iconDef);

            List<Gene>  targetGenes = [.. GeneHelpers.GetAllGenes(pawn)];

            // Filter genes from "weird" sources, e.g. Insector.
            targetGenes = targetGenes.Where(x => pawn.genes.Xenogenes.Contains(x) || pawn.genes.Endogenes.Contains(x)).ToList();
            if (!endoGenes)
            {
                var pEndoGenes = pawn.genes.Endogenes.ToList();
                targetGenes = targetGenes.Where(x => !pEndoGenes.Contains(x)).ToList();
            }
            if (!xenoGenes)
            {
                var pXenoGenes = pawn.genes.Xenogenes.ToList();
                targetGenes = targetGenes.Where(x => !pXenoGenes.Contains(x)).ToList();
            }
            if (!inactive)
            {
                targetGenes = targetGenes.Where(x => x.Active).ToList();
            }
            if (!archite)
            {
                targetGenes = targetGenes.Where(x => x.def.biostatArc == 0).ToList();
            }

            try
            {
                xenogerm.GeneSet.SetNameDirect(pawn.genes.xenotypeName);
            }
            catch { } // This isn't important, so whatever if it fails.

            GenPlace.TryPlaceThing(xenogerm, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }
    }

}