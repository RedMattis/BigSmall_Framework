using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

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

        public static void IncorporateGenes(Pawn pawn, object target, int genePickCount=4, bool stealTraits=true,
            bool userPicks=true, int randomPickCount=4, bool excludeBodySwap=false)
        {
            Pawn targetPawn = target as Pawn;
            if (targetPawn == null)
            {
                var corpse = target as Corpse;
                targetPawn = corpse?.InnerPawn;
            }
            if (targetPawn == null)
            {
                Log.Warning($"Target {target} is not a pawn");
                return;
            }

            var genesOnCorpse = targetPawn?.genes?.GenesListForReading;
            List<GeneDef> unpickedGenes = genesOnCorpse?.Select(x => x.def).ToList() ?? [];
            unpickedGenes.AddRange(MutantToGeneset.GetGenesFromAnomalyCreature(targetPawn).Where(x=>x != null));
            if (genesOnCorpse == null && unpickedGenes.Count == 0)
            {
                // Replace these with messages. Preferably notify the user via a message popup.
                Log.Warning($"Target {targetPawn} has no valid genes");
                return;
            }

            var genesToPick = new List<GeneDef>();
            
            

            if (stealTraits && targetPawn?.RaceProps?.Humanlike == true)
            {
                GetGenesFromTraits(targetPawn, genesToPick);
            }
            genePickCount += genesToPick.Count();

            bool isBaseliner = genesOnCorpse != null && genesOnCorpse.Sum(x => x.def.biostatCpx) == 0;
            // Try adding all the baseliner genes.
            if (isBaseliner)
            {
                var humanGeneList = new List<string>
                {
                    "Hands_Human",
                    "Headbone_Human",
                    "Ears_Human",
                    "Nose_Human",
                    "Jaw_Baseline",
                    "Voice_Human"
                };
                var baseLinerGenes = DefDatabase<GeneDef>.AllDefsListForReading
                    .Where(x => x.defName.StartsWith("GET_") || humanGeneList.Contains(x.defName)).ToList();
                unpickedGenes.AddRange(baseLinerGenes);
            }

            // If the pawn already has a body-type gene, don't swap it. It is a bit too extreme of a change since it can remove bionics and such.
            if (excludeBodySwap && pawn.genes.GenesListForReading.Any(x=>x.def.exclusionTags?.Contains("ThingDefSwap") == true))
            {
                unpickedGenes = unpickedGenes.Where(x => x.exclusionTags?.Contains("ThingDefSwap") == false).ToList();
            }
            while (unpickedGenes.Count > 0 && genesToPick.Count < genePickCount)
            {
                var gene = unpickedGenes.RandomElement();
                unpickedGenes.Remove(gene);
                if (pawn.genes.GenesListForReading.Any(x => x.def.defName == gene.defName) == false
                    && genesToPick.Contains(gene) == false)
                {
                    if (pawn.genes.GenesListForReading.Any(x => x.def.defName == gene.defName))
                        continue;
                    genesToPick.Add(gene);
                }
            }

            var allGeneDefs = DefDatabase<GeneDef>.AllDefsListForReading;
            try
            {
                ReplaceGeneInList(genesToPick, allGeneDefs, "BS_GeneStabilizing_Extreme", "BS_Instability_Catastrophic");
                ReplaceGeneInList(genesToPick, allGeneDefs, "BS_GeneStabilizing_Great", "Instability_Major");
                ReplaceGeneInList(genesToPick, allGeneDefs, "BS_GeneStabilizing_Moderate", "Instability_Mild");
            }
            catch { }
            
            // Remove genes the pawn already has in their xenogene list.
            genesToPick.RemoveAll(x => pawn.genes.Xenogenes.Select(g=>g.def).Contains(x));

            if (genesToPick.Any())
            {
                if (userPicks)
                {
                    // Reverse so traits (etc.) are at the bottom.
                    genesToPick.Reverse();
                    Find.WindowStack.Add(new Dialog_PickGenes(pawn, genesToPick));
                }
                else
                {
                    var genesToAdd = new List<GeneDef>();
                    while (randomPickCount > 0 && genesToPick.Count > 0)
                    {
                        var gene = genesToPick.RandomElement();
                        genesToPick.Remove(gene);
                        genesToAdd.Add(gene);
                        randomPickCount--;
                    }

                    foreach (var gene in genesToAdd)
                    {
                        pawn.genes.AddGene(gene, xenogene: true);
                    }
                }
            }
        }

        private static void ReplaceGeneInList(List<GeneDef> genesToPick, List<GeneDef> allGeneDefs, string stabilityExtreme, string instabilityExtreme)
        {
            foreach (var gene in genesToPick)
            {
                if (gene.defName.StartsWith(stabilityExtreme))
                {
                    var newGene = allGeneDefs.Where(x => x.defName == instabilityExtreme).FirstOrDefault();
                    if (newGene != null)
                    {
                        genesToPick.Remove(gene);
                        genesToPick.Add(newGene);
                    }
                }
            }
        }

        public static void GetGenesFromTraits(Pawn target, List<GeneDef> genesToPick, bool onlyZeroCostGenes=false)
        {
            if (target == null) return;
            var allGeneDefs = DefDatabase<GeneDef>.AllDefsListForReading;
            var traitsOnCorpse = target?.story?.traits?.allTraits;
            if (traitsOnCorpse != null && !onlyZeroCostGenes)
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
            try
            {
                if (target?.gender == Gender.Male)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_MaleOnly").First());
                }
                else if (target?.gender == Gender.Female)
                {
                    genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_FemaleOnly").First());
                }
            }
            catch { Log.Warning($"Gender genes not found. Skipping."); }

            if (target?.story.bodyType == BodyTypeDefOf.Male)
            {
                genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Standard").First());
            }
            else if (target?.story.bodyType == BodyTypeDefOf.Female)
            {
                genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Standard").First());
            }
            else if (target?.story.bodyType == BodyTypeDefOf.Hulk)
            {
                genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Hulk").First());
            }
            else if (target?.story.bodyType == BodyTypeDefOf.Fat)
            {
                genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Fat").First());
            }
            else if (target?.story.bodyType == BodyTypeDefOf.Thin)
            {
                genesToPick.Add(allGeneDefs.Where(x => x.defName == "Body_Thin").First());
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
            RemoveGenesOverLimit(parent.pawn, -9);
        }

        // Don't refactor this. It is currently patched by Keyz for Samuel Streamer's run.
        public static bool RemoveGenesOverLimit(Pawn pawn, int limit)
        {
            var xGenes = pawn.genes.Xenogenes;
            bool removed = false;

            int idx = 0;
            // Sum up the metabolism cost of the new genes
            while (pawn.genes.GenesListForReading.Where(x => !x.Overridden).Sum(x => x.def.biostatMet) < limit || idx > 100)
            {
                if (xGenes.Count == 0)
                    break;
                // Pick a random gene from the newGenes with a negative metabolism cost and remove it.
                var geneToRemove = xGenes.Where(x => x.def.biostatMet <= 1).RandomElement();
                if (geneToRemove != null)
                {
                    xGenes.Remove(geneToRemove);
                    removed = true;
                }
                else
                {
                    break;
                }
                idx++;  // Ensure we don't get stuck in an infinite loop no matter what.
            }
            if (removed)
            {
                Messages.Message($"BS_GenesRemovedByOverLimit".Translate(pawn.Name.ToStringShort, idx, limit), pawn, MessageTypeDefOf.NegativeHealthEvent);
            }
            
            return removed;
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

        public static List<string> BodyShapeGeneNames =>
        [
            "Body_Standard",
            "Body_Hulk",
            "Body_Fat",
            "Body_Thin",
        ];

        public static List<string> ForcedGenderGenes =>
        [
            "Body_MaleOnly",
            "Body_FemaleOnly",
        ];

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
