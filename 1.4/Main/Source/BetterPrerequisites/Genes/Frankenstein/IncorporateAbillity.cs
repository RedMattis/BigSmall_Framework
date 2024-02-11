using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace BigAndSmall
{
    public class CompProperties_Incorporate : CompProperties_AbilityEffect
    {
        public int pickCount = 2;
        public bool stealTraits = true;
        public CompProperties_Incorporate()
        {
            compClass = typeof(CompProperties_IncorporateEffect);
        }
    }

    public class CompProperties_IncorporateEffect : CompAbilityEffect
    {
        public new CompProperties_Incorporate Props => (CompProperties_Incorporate)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;
            var corpse = (Corpse)target.Thing;
            if (corpse == null)
            {
                Log.Warning($"Target {target.Thing} is not a corse");
                return;
            }

            IncorporateGenes(pawn, corpse, genePickCount: Props.pickCount, stealTraits:Props.stealTraits);

        }

        public static void IncorporateGenes(Pawn pawn, Corpse corpse, int genePickCount=4, bool stealTraits=true)
        {
            var genesOnCorpse = corpse.InnerPawn?.genes.GenesListForReading;
            var traitsOnCorpse = corpse.InnerPawn?.story?.traits?.allTraits;
            var allGeneDefs = DefDatabase<GeneDef>.AllDefsListForReading;

            var genesToPick = new List<GeneDef>();
            if (genesOnCorpse == null)
                return;

            if (stealTraits)
            {
                if (traitsOnCorpse != null)
                {
                    var beautyTraits = traitsOnCorpse.Where(x => x.def.defName.StartsWith("Beauty"));
                    if (beautyTraits.Count() > 0)
                    {
                        var bTrait = beautyTraits.First();
                        if (bTrait.Degree == 2)
                            genesToPick.Add(allGeneDefs.Where(x => x.defName == "Beauty_Beautiful").First());
                        if (bTrait.Degree == 1)
                            genesToPick.Add(allGeneDefs.Where(x => x.defName == "Beauty_Pretty").First());
                        if (bTrait.Degree == -1)
                            genesToPick.Add(allGeneDefs.Where(x => x.defName == "Beauty_Ugly").First());
                        if (bTrait.Degree == -2)
                            genesToPick.Add(allGeneDefs.Where(x => x.defName == "Beauty_VeryUgly").First());
                    }

                    var toughTrait = traitsOnCorpse.Any(x => x.def.defName.StartsWith("Tough"));
                    if (toughTrait)
                    {
                        genesToPick.Add(allGeneDefs.Where(x => x.defName == "Robust").First());
                    }

                    var speedTraits = traitsOnCorpse.Where(x => x.def.defName.StartsWith("SpeedOffset"));
                    if (speedTraits.Count() > 0)
                    {
                        var speedTrait = speedTraits.First();
                        if (speedTrait.Degree == 2)
                            genesToPick.Add(allGeneDefs.Where(x => x.defName == "MoveSpeed_VeryQuick").First());
                        if (speedTrait.Degree == 1)
                            genesToPick.Add(allGeneDefs.Where(x => x.defName == "MoveSpeed_Quick").First());
                        if (speedTrait.Degree == -1)
                            genesToPick.Add(allGeneDefs.Where(x => x.defName == "MoveSpeed_Slow").First());
                    }
                }

                if (corpse.InnerPawn?.gender == Gender.Male)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_MaleOnly").First());
                }
                else if (corpse.InnerPawn?.gender == Gender.Female)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_FemaleOnly").First());
                }

                if (corpse.InnerPawn?.story.bodyType == BodyTypeDefOf.Male)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Standard").First());
                }
                else if (corpse.InnerPawn?.story.bodyType == BodyTypeDefOf.Female)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Standard").First());
                }
                else if (corpse.InnerPawn?.story.bodyType == BodyTypeDefOf.Hulk)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Hulk").First());
                }
                else if (corpse.InnerPawn?.story.bodyType == BodyTypeDefOf.Fat)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Fat").First());
                }
                else if (corpse.InnerPawn?.story.bodyType == BodyTypeDefOf.Thin)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Thin").First());
                }
            }
            genePickCount += genesToPick.Count();
            List<Gene> unpickedGenes = genesOnCorpse.ToList();
            while (unpickedGenes.Count > 0 && genesToPick.Count < genePickCount)
            {
                var gene = unpickedGenes.RandomElement();
                unpickedGenes.Remove(gene);
                if (pawn.genes.GenesListForReading.Any(x => x.def.defName == gene.def.defName) == false
                    && genesToPick.Contains(gene.def) == false)
                {
                    genesToPick.Add(gene.def);
                }
            }


            if (genesToPick.Any())
            {
                // Reverse so traits (etc.) are at the bottom.
                genesToPick.Reverse();
                Find.WindowStack.Add(new Dialog_PickGenes(pawn, genesToPick));
            }
        }

        public override void PostApplied(List<LocalTargetInfo> targets, Map map)
        {
            base.PostApplied(targets, map);
            foreach (var target in targets)
            {
                var corpse = (Corpse)target.Thing;
                corpse?.Destroy();
            }
            RemoveGenesOverLimit(parent.pawn);
        }

        public static void RemoveGenesOverLimit(Pawn pawn)
        {
            var xGenes = pawn.genes.Xenogenes;

            int idx = 0;
            // Sum up the metabolism cost of the new genes
            while (pawn.genes.GenesListForReading.Where(x => !x.Overridden).Sum(x => x.def.biostatMet) < -9 || idx > 100)
            {
                if (xGenes.Count == 1)
                    break;
                // Pick a random gene from the newGenes with a negative metabolism cost and remove it.
                var geneToRemove = xGenes.Where(x => x.def.biostatMet <= 1).RandomElement();
                if (geneToRemove != null)
                {
                    xGenes.Remove(geneToRemove);
                }
                else
                {
                    break;
                }
                idx++;  // Ensure we don't get stuck in an infinite loop no matter what.
            }
        }
    }
    // UI grabbed from another mod.
    public class Dialog_PickGenes : Window
    {
        private GeneDef pickedGene;
        public List<GeneDef> geneChoices;
        private float scrollHeight;
        private Vector2 scrollPosition;
        public Pawn pawn;
        public string letterTextKey;

        public override Vector2 InitialSize => new Vector2(420f, 340f);

        public Dialog_PickGenes(Pawn pawn, List<GeneDef> geneChoices)
        {
            this.pawn = pawn;
            this.geneChoices = geneChoices;
            pickedGene = geneChoices.First();
            forcePause = true;
            absorbInputAroundWindow = true;
            if (!SelectionsMade())
            {
                closeOnAccept = false;
                closeOnCancel = false;
            }
        }

        public static List<string> BodyShapeGeneNames => new List<string>
        {
            "Body_Standard",
            "Body_Hulk",
            "Body_Fat",
            "Body_Thin",
        };

        public static List<string> ForcedGenderGenes => new List<string>
        {
            "Body_MaleOnly",
            "Body_FemaleOnly",
        };

        public override void DoWindowContents(Rect inRect)
        {
            float curY = 0f;
            Widgets.Label(0f, ref curY, inRect.width, "BS_PickAListedGene".Translate(pawn).Resolve() + ":");
            Rect outRect = new Rect(inRect.x, curY + 15f, inRect.width, inRect.height - 50f);
            outRect.yMax -= 4f + CloseButSize.y;
            Text.Font = GameFont.Small;
            curY = outRect.y;
            Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16f, scrollHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            DrawGeneChoices(viewRect.width, ref curY);
            if (Event.current.type == EventType.Layout)
            {
                scrollHeight = Mathf.Max(curY - 24f - 15f, outRect.height);
            }
            Widgets.EndScrollView();
            Rect rect = new Rect(0f, outRect.yMax+15f, inRect.width, CloseButSize.y);
            AcceptanceReport acceptanceReport = CanClose();
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, CloseButSize.x, CloseButSize.y), "Cancel".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(new Rect(rect.xMax - CloseButSize.x, rect.y, CloseButSize.x, CloseButSize.y), "OK".Translate()))
            {
                if (acceptanceReport.Accepted)
                {
                    GainGene(pawn, pickedGene);
                    Close();
                }
            }
        }

        public static void GainGene(Pawn pawn, GeneDef gene)
        {
            // Remove forced gender and body genes if the new ones is of that type.
            if (BodyShapeGeneNames.Contains(gene.defName))
            {
                foreach(var bodyShapeGene in pawn.genes.GenesListForReading.Where(x => BodyShapeGeneNames.Contains(x.def.defName)))
                {
                    pawn.genes.RemoveGene(bodyShapeGene);
                }
            }
            if (ForcedGenderGenes.Contains(gene.defName))
            {
                foreach (var forcedGenderGene in pawn.genes.GenesListForReading.Where(x => ForcedGenderGenes.Contains(x.def.defName)))
                {
                    pawn.genes.RemoveGene(forcedGenderGene);
                }
            }

            if (gene.skinColorOverride != null)
            {
                pawn.story.SkinColorBase = gene.skinColorOverride.Value;
                pawn.story.skinColorOverride = gene.skinColorOverride;
            }
            if (gene.hairColorOverride != null)
            {
                pawn.story.HairColor = gene.hairColorOverride.Value;
            }

            pawn.genes.AddGene(gene, xenogene: true);
        }

        private bool SelectionsMade()
        {
            if (!geneChoices.NullOrEmpty() && geneChoices == null)
            {
                return false;
            }
            return true;
        }

        private AcceptanceReport CanClose()
        {
            if (!SelectionsMade())
            {
                return "SelectAGene".Translate();
            }
            return AcceptanceReport.WasAccepted;
        }

        private void DrawGeneChoices(float width, ref float curY)
        {
            if (geneChoices.NullOrEmpty())
            {
                return;
            }
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, curY, 360, 99999f);
            listing_Standard.Begin(rect);

            foreach (GeneDef geneChoice in geneChoices)
            {
                if (listing_Standard.RadioButton($"{geneChoice.LabelCap}, (" + "BS_Metabolism".Translate() +$": {geneChoice.biostatMet})",
                    pickedGene == geneChoice, 30f, tooltip: geneChoice.description))
                {
                    pickedGene = geneChoice;
                }
            }
            listing_Standard.End();
            curY += listing_Standard.CurHeight + 10f + 4f;
        }
    }
}
