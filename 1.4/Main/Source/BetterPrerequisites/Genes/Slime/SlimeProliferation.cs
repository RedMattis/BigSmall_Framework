using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;
using static RimWorld.ColonistBar;
using static System.Net.Mime.MediaTypeNames;

namespace BigAndSmall
{
    public class CompProperties_SlimeProliferation : CompProperties_AbilityEffect
    {
        public CompProperties_SlimeProliferation()
        {
            compClass = typeof(CompAbilityEffect_SlimeProliferation);
        }
    }

    public class CompAbilityEffect_SlimeProliferation : CompAbilityEffect
    {
        public new CompProperties_SlimeProliferation Props => (CompProperties_SlimeProliferation)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                DoProliferate(parent.pawn, pawn);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = true)
        {
            Pawn otherPawn = target.Pawn;
            if (otherPawn == null)
            {
                otherPawn = parent.pawn;
            }

            // Check if the other pawn has the SlimeProliferation Gene
            if (Helpers.GetActiveGenesByName(otherPawn, "BS_SlimeProliferation").Count() == 0)
            {
                if (throwMessages)
                {
                    Messages.Message("BS_TargetLacksSlimeProliferation".Translate(otherPawn.Label),
                        otherPawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return true;
        }

        public static void DoProliferate(Pawn parentA, Pawn parentB)
        {
            var geneSetA = Helpers.GetAllActiveGenes(parentA).Select(x=>x.def).ToList();
            var geneSetB = Helpers.GetAllActiveGenes(parentB).Select(x => x.def).ToList();

            var sharedGenes = geneSetA.Intersect(geneSetB).ToList();

            int parentBGeneCount = geneSetB.Count();

            // 20% to 100% of ParentA's Genes:
            int numberOfGenesToTransfer = Rand.RangeInclusive((int)(parentBGeneCount * 0.1), parentBGeneCount-1);

            // Generate a baby pawn
            PawnGenerationRequest request = new PawnGenerationRequest(parentA.kindDef, parentA.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Newborn);
            Pawn babyPawn = PawnGenerator.GeneratePawn(request);

            // Remove all genes from the baby pawn
            babyPawn.genes.Reset();

            // Add all genes from parentA to the baby pawn
            foreach (var gene in geneSetA)
            {
                babyPawn.genes.AddGene(gene, false);
            }
            int endoMet = Helpers.GetAllActiveEndoGenes(babyPawn).Sum(x => x.def.biostatMet);

            // If the baby has any gene not from the mother delete it. It is probably a hair or skin gene that the 
            // game pulled out of a magic hat.
            foreach(var gene in babyPawn.genes.GenesListForReading.Where(x => !geneSetA.Contains(x.def)).ToList())
            {
                babyPawn.genes.RemoveGene(gene);
            }

            if (parentA != parentB)
            {
                // Add 25-75% of genes from father to the baby pawn as xenogenes
                int count = 0;
                var bGenes = new List<GeneDef>();
                while (count < numberOfGenesToTransfer && geneSetB.Count > 0)
                {
                    var gene = geneSetB.RandomElement();
                    geneSetB.Remove(gene);
                    if (!babyPawn.genes.GenesListForReading.Select(x=>x.def).Contains(gene))
                    {
                        bGenes.Add(gene);
                    }
                    count++;
                }

                //Discombobulator.RemoveRandomToMetabolism(0, bGenes, minMet: -5);

                foreach (var gene in bGenes)
                {
                    babyPawn.genes.AddGene(gene, true);
                }

                Helpers.GetAllActiveGenes(babyPawn).Sum(x => x.def.biostatMet);

                var finalXegenes = babyPawn.genes.Xenogenes.Select(x=>x.def).ToList();

                

                babyPawn.genes.Xenogenes.Clear();

                foreach (var gene in finalXegenes)
                {
                    babyPawn.genes.AddGene(gene, true);
                }

                // Remove all overriden genes from the baby pawn
                foreach (var gene in babyPawn.genes.GenesListForReading.Where(x => x.Overridden).ToList())
                {
                    babyPawn.genes.RemoveGene(gene);
                }

                Helpers.RemoveRandomToMetabolism(0, babyPawn.genes, minMet: -5, exclusionList: sharedGenes);
                Helpers.RemoveRandomToMetabolism(0, babyPawn.genes, minMet: -5);

                // Integrate all the Xenogenes, turning them into Endogenes.
                Discombobulator.IntegrateGenes(babyPawn);

                babyPawn.genes.xenotypeName = "Hybrid".Translate();
            }
            else
            {
                parentB = null;
            }

            if (PawnUtility.TrySpawnHatchedOrBornPawn(babyPawn, parentA))
            {
                if (babyPawn.playerSettings != null && parentA.playerSettings != null)
                {
                    babyPawn.playerSettings.AreaRestriction = parentA.playerSettings.AreaRestriction;
                }
                if (babyPawn.RaceProps.IsFlesh)
                {
                    babyPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parentA);
                    if (parentB != null)
                    {
                        babyPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parentB);
                    }
                }
                if (parentA.Spawned)
                {
                    parentA.GetLord()?.AddPawn(babyPawn);
                }
            }
            else
            {
                Find.WorldPawns.PassToWorld(babyPawn, PawnDiscardDecideMode.Discard);
            }
            TaleRecorder.RecordTale(TaleDefOf.GaveBirth, parentA, babyPawn);

            if (parentA.Spawned)
            {
                FilthMaker.TryMakeFilth(parentA.Position, parentA.Map, ThingDefOf.Filth_AmnioticFluid, parentA.LabelIndefinite(), 5);
                if (parentA.caller != null)
                {
                    parentA.caller.DoCall();
                }
                if (babyPawn.caller != null)
                {
                    babyPawn.caller.DoCall();
                }
            }
            ChoiceLetter_BabyBirth choiceLetter_BabyBirth = (ChoiceLetter_BabyBirth)LetterMaker.MakeLetter("BS_ProliferationBirth".Translate(), "BS_ProliferationDescription".Translate(), LetterDefOf.BabyBirth, babyPawn);
            choiceLetter_BabyBirth.Start();
            Find.LetterStack.ReceiveLetter(choiceLetter_BabyBirth);

            // Age baby up to 3 years
            babyPawn.ageTracker.AgeBiologicalTicks = 3 * GenDate.TicksPerYear;

            //Find.QuestManager.Notify_PawnBorn(babyPawn, parentA, parentA, parentB);

        }
    }
}
