using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class PawnKindExtension : DefModExtension
    {
        public SimpleCurve ageCurve = null;
        public SimpleCurve psylinkLevels = null;

        public List<GeneDef> appendGenes = [];
        public bool appendAsXenogenes = false;
        public bool removeOverlappingGenes = true;

        public void Execute(Pawn pawn)
        {
            AppendGenes(pawn);
            ApplyAgeCurve(pawn);
            ApplyPsylink(pawn);
        }

        public void AppendGenes(Pawn pawn)
        {
            // Check exclusion tags and remove all conflicting genes.
            List<Gene> pawnGenes = [..pawn.genes.GenesListForReading];
            if (removeOverlappingGenes)
            {
                var appendGeneExlusions = appendGenes.SelectMany(x => x.exclusionTags).ToList();
                if (appendGeneExlusions.Any())
                {
                    foreach (var gene in pawnGenes.Where(x => !x.def.exclusionTags.NullOrEmpty() && x.def.exclusionTags.Intersect(appendGeneExlusions).Any()))
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                }
            }

            // Append
            if (appendAsXenogenes)
            {
                foreach (var gene in appendGenes)
                {
                    pawn.genes.AddGene(gene, true);
                }
            }
            else
            {
                foreach (var gene in appendGenes)
                {
                    pawn.genes.AddGene(gene, false);
                }
            }
        }

        public void ApplyAgeCurve(Pawn pawn)
        {
            if (ageCurve != null)
            {
                pawn.ageTracker.AgeBiologicalTicks = (long)ageCurve.Evaluate(Rand.Value) * 3600000;
            }

        }
        public void ApplyPsylink(Pawn pawn)
        {
            if (psylinkLevels is SimpleCurve psyLinkCurve && ModsConfig.RoyaltyActive)
            {
                int countToSet = (int)psyLinkCurve.Evaluate(Rand.Value);

                if (countToSet > 0)
                {
                    // Check if they have it already.
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier) is Hediff_Level hediff_Level)
                    {
                        int level = hediff_Level.level;
                        hediff_Level.SetLevelTo(countToSet + level);
                    }
                    else
                    {
                        hediff_Level = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn, pawn.health.hediffSet.GetBrain()) as Hediff_Level;
                        pawn.health.AddHediff(hediff_Level);
                        hediff_Level.SetLevelTo(countToSet);
                    }
                }
            }
        }
    }
}
