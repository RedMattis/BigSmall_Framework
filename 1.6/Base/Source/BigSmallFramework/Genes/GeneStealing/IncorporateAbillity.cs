using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
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
        public new CompProperties_Incorporate Props => props as CompProperties_Incorporate;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;
            var targetThing = target.Thing;
            if (targetThing == null)
            {
                return;
            }

            IncorporateGenes(pawn, targetThing, genePickCount: Props?.pickCount ?? 2, stealTraits: Props?.stealTraits ?? true);
        }

        public static void IncorporateGenes(Pawn pawn, object target, int genePickCount = 4, bool stealTraits = true,
            bool userPicks = true, int randomPickCount = 4, bool excludeBodySwap = false)
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
            List<GeneDef> unpickedGenes = genesOnCorpse?.Select(x => x.def).ToList() ?? new List<GeneDef>();
            unpickedGenes.AddRange(GenesFromSpecial.GetGenesFromAnomalyCreature(targetPawn).Where(x => x != null));
            if (genesOnCorpse == null && unpickedGenes.Count == 0)
            {
                Log.Warning($"Target {targetPawn} has no valid genes");
                return;
            }

            var genesToPick = new List<GeneDef>();
          
            if (stealTraits && targetPawn?.RaceProps?.Humanlike == true)
            {
                GetGenesFromTraits(targetPawn, genesToPick);
            }
            genePickCount += genesToPick.Count();

            bool isBaseliner = genesOnCorpse != null && genesOnCorpse.Sum(x => x.def.biostatCpx) == 0 && genesOnCorpse.Count < 6;
            if (targetPawn?.genes is Pawn_GeneTracker gt)
            {
                if (gt.xenotype != XenotypeDefOf.Baseliner && !gt.hybrid)
                    isBaseliner = false;
            }
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

            if (excludeBodySwap && pawn.genes.GenesListForReading.Any(x => x.def.exclusionTags?.Contains("ThingDefSwap") == true))
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

            genesToPick.RemoveAll(x => pawn.genes.Xenogenes.Select(g => g.def).Contains(x));

            if (genesToPick.Any())
            {
                if (userPicks)
                {
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

        public static void GetGenesFromTraits(Pawn target, List<GeneDef> genesToPick, bool onlyZeroCostGenes = false)
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
            catch (Exception e)
            {
                Log.Warning($"Gender genes not found. Skipping.\n{e.Message}\n{e.StackTrace}");
            }

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
                target.Thing?.Destroy();
            }
            RemoveGenesOverLimit(parent.pawn, -9);
        }

        // Patched by Keyz, don't rename or change parameters.
        public static bool RemoveGenesOverLimit(Pawn pawn, int limit)
        {
            var xGenes = pawn.genes.Xenogenes;
            bool removed = false;

            int idx = 0;
            while (pawn.genes.GenesListForReading.Where(x => !x.Overridden).Sum(x => x.def.biostatMet) < limit || idx > 100)
            {
                if (xGenes.Count == 0)
                    break;
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
                idx++;
            }
            if (removed)
            {
                Messages.Message($"BS_GenesRemovedByOverLimit".Translate(pawn.Name.ToStringShort, idx, limit), pawn, MessageTypeDefOf.NegativeHealthEvent);
            }

            return removed;
        }
    }
    public class Dialog_PickGenes : Window
    {
        private Pawn caster;
        private List<GeneDef> availableGenes;
        private GeneDef selectedGene;
        public string letterTextKey;

        private Vector2 scrollPositionGenes;
        private Vector2 scrollPositionCaster;
        private Vector2 scrollPositionDetails;
        private RimWorld.QuickSearchWidget quickSearchWidget = new RimWorld.QuickSearchWidget();
        private List<Gene> casterXenogenes;
        private List<Gene> casterEndogenes;
        private List<GeneDef> filteredGenes = new List<GeneDef>();
        private static Vector2 lastWindowSize = Vector2.zero;

        public override Vector2 InitialSize
        {
            get
            {
                if (lastWindowSize == Vector2.zero)
                {
                    float w = Mathf.Clamp(UI.screenWidth * 0.60f, 850f, 1200f);
                    float h = Mathf.Clamp(UI.screenHeight * 0.70f, 650f, 900f);
                    lastWindowSize = new Vector2(w, h);
                }
                return lastWindowSize;
            }
        }

        public Dialog_PickGenes(Pawn caster, List<GeneDef> availableGenes)
        {
            this.caster = caster;
            this.availableGenes = availableGenes;
            this.casterXenogenes = caster.genes.Xenogenes;
            this.casterEndogenes = caster.genes.Endogenes;
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.draggable = true;
            this.resizeable = true;
            this.drawShadow = true;
            UpdateFilteredGenes();
            if (filteredGenes.Count > 0)
            {
                selectedGene = filteredGenes[0];
            }
        }

        private void UpdateFilteredGenes()
        {
            filteredGenes.Clear();
            foreach (var gene in availableGenes)
            {
                if (quickSearchWidget.filter.Matches(gene.label))
                {
                    filteredGenes.Add(gene);
                }
            }
            scrollPositionGenes = Vector2.zero;
        }

        public override void DoWindowContents(Rect inRect)
        {
            const float headerHeight = 28f;
            Rect headerRect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "BS_PickAListedGene".Translate(caster).Resolve());
            Text.Font = GameFont.Small;

            // Header controls
            Rect searchRect = new Rect(0f, 45f, 300f, 24f);
            quickSearchWidget.OnGUI(searchRect, UpdateFilteredGenes);

            // Main Content
            const float rightWidth = 380f;
            Rect mainRect = new Rect(0f, 85f, inRect.width, inRect.height - 85f - 110f);
            Rect rightRect = new Rect(mainRect.xMax - rightWidth, mainRect.y, rightWidth, mainRect.height);
            Rect leftRect = new Rect(mainRect.x, mainRect.y, mainRect.width - rightWidth - 12f, mainRect.height);

            // Left side: Gene List
            const float scrollbarWidth = 16f;
            const float geneWidth = 87f;
            const float geneHeight = 68f;
            const float gap = 4f;

            const float geneHeightWithGap = geneHeight + gap;
            const float geneWidthWithGap = geneWidth + gap;
            const float baseGeneListHeight = headerHeight + geneHeightWithGap + gap * 2;

            // 1. Available Genes Panel
            int genesPerRow = Mathf.FloorToInt((leftRect.width - scrollbarWidth - 4f) / geneWidthWithGap);
            float heightFromGeneCount = geneHeightWithGap * (filteredGenes.Count / genesPerRow);
            float maxAvailableRectHeight = baseGeneListHeight + heightFromGeneCount;
            float availableRectHeight = Mathf.Min(maxAvailableRectHeight, leftRect.height * 0.60f);
            Rect availableRect = new Rect(leftRect.x, leftRect.y, leftRect.width, availableRectHeight);

            Widgets.DrawMenuSection(availableRect);
            Rect availableOutRect = availableRect.ContractedBy(4f);
            int availableCols = Mathf.FloorToInt((availableOutRect.width - scrollbarWidth - 4f) / (geneWidth + gap));
            if (availableCols <= 0) availableCols = 1;

            Rect availableViewRect = new Rect(0f, 0f, availableOutRect.width - scrollbarWidth, CalculateAvailableScrollHeight(availableCols));
            Widgets.BeginScrollView(availableOutRect, ref scrollPositionGenes, availableViewRect);

            float curY = 0f;
            Rect sectionHeaderRect = new Rect(0f, curY, availableViewRect.width, 24f);
            Widgets.Label(sectionHeaderRect, "Available to Incorporate:".Colorize(ColoredText.TipSectionTitleColor));
            curY += headerHeight;

            for (int i = 0; i < filteredGenes.Count; i++)
            {
                var gene = filteredGenes[i];
                int row = i / availableCols;
                int col = i % availableCols;
                Rect geneRect = new Rect(col * (geneWidth + gap), curY + row * (geneHeight + gap), geneWidth, geneHeight);

                if (selectedGene == gene)
                {
                    Widgets.DrawHighlightSelected(geneRect);
                }
                if (Widgets.ButtonInvisible(geneRect))
                {
                    selectedGene = gene;
                    scrollPositionDetails = Vector2.zero;
                    // SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
                GeneUIUtility.DrawGeneDef(gene, geneRect, GeneType.Xenogene, null, true, true, false);
            }
            Widgets.EndScrollView();

            // 2. Caster's Genes Panel
            Rect casterLabelRect = new Rect(leftRect.x, availableRect.yMax + 8f, leftRect.width, 24f);
            Widgets.Label(casterLabelRect, (caster.LabelShortCap + "'s genes:").Colorize(ColoredText.TipSectionTitleColor));

            Rect casterRect = new Rect(leftRect.x, casterLabelRect.yMax, leftRect.width, leftRect.height - availableRect.height - 32f);
            Widgets.DrawMenuSection(casterRect);
            Rect casterOutRect = casterRect.ContractedBy(4f);
            int casterCols = Mathf.FloorToInt((casterOutRect.width - scrollbarWidth - 4f) / (geneWidth + gap));
            if (casterCols <= 0) casterCols = 1;

            Rect casterViewRect = new Rect(0f, 0f, casterOutRect.width - scrollbarWidth, CalculateCasterScrollHeight(casterCols));
            Widgets.BeginScrollView(casterOutRect, ref scrollPositionCaster, casterViewRect);

            curY = 0f;
            // Draw Endogenes
            for (int i = 0; i < casterEndogenes.Count; i++)
            {
                int row = i / casterCols;
                int col = i % casterCols;
                Rect geneRect = new Rect(col * (geneWidth + gap), curY + row * (geneHeight + gap), geneWidth, geneHeight);
                GeneUIUtility.DrawGeneDef(casterEndogenes[i].def, geneRect, GeneType.Endogene, null, true, false, casterEndogenes[i].Overridden);
            }

            if (casterEndogenes.Count > 0 && casterXenogenes.Count > 0)
            {
                curY += Mathf.CeilToInt(casterEndogenes.Count / (float)casterCols) * (geneHeight + gap) + 4f;
                Widgets.DrawLineHorizontal(4f, curY, casterViewRect.width - 8f);
                curY += 8f;
            }

            // Draw Xenogenes
            for (int i = 0; i < casterXenogenes.Count; i++)
            {
                int row = i / casterCols;
                int col = i % casterCols;
                Rect geneRect = new Rect(col * (geneWidth + gap), curY + row * (geneHeight + gap), geneWidth, geneHeight);
                GeneUIUtility.DrawGeneDef(casterXenogenes[i].def, geneRect, GeneType.Xenogene, null, true, false, casterXenogenes[i].Overridden);
            }
            Widgets.EndScrollView();

            // Right side: Details
            if (selectedGene != null)
            {
                Widgets.DrawMenuSection(rightRect);
                Rect infoRect = rightRect.ContractedBy(12f);
                float detailsY = infoRect.y;

                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(infoRect.x, detailsY, infoRect.width, 30f), selectedGene.LabelCap);
                detailsY += 35f;
                Text.Font = GameFont.Small;

                float statsHeight = BiostatsTable.HeightForBiostats(selectedGene.biostatArc);
                Rect statsRect = new Rect(infoRect.x, detailsY, infoRect.width, statsHeight);
                BiostatsTable.Draw(statsRect, selectedGene.biostatCpx, selectedGene.biostatMet, selectedGene.biostatArc, false, false);
                detailsY += statsHeight + 10f;

                Widgets.DrawLineHorizontal(infoRect.x, detailsY, infoRect.width);
                detailsY += 10f;

                Rect descOutRect = new Rect(infoRect.x, detailsY, infoRect.width, infoRect.height - (detailsY - infoRect.y));
                string fullDesc = selectedGene.DescriptionFull;
                float textHeight = Text.CalcHeight(fullDesc, descOutRect.width - 16f);
                Rect descViewRect = new Rect(0f, 0f, descOutRect.width - 16f, textHeight);

                Widgets.BeginScrollView(descOutRect, ref scrollPositionDetails, descViewRect);
                Widgets.Label(new Rect(0f, 0f, descViewRect.width, textHeight), fullDesc);
                Widgets.EndScrollView();
            }

            // Bottom: Pawn Status and Buttons
            Rect bottomArea = new Rect(0f, inRect.height - 100f, inRect.width, 100f);
            Widgets.DrawLineHorizontal(0f, bottomArea.y, inRect.width);

            int currentMet = caster.genes.GenesListForReading.Where(x => !x.Overridden).Sum(x => x.def.biostatMet);
            int resultingMet = currentMet;
            if (selectedGene != null)
            {
                resultingMet += selectedGene.biostatMet;
            }

            Rect statusRect = new Rect(10f, bottomArea.y + 6f, inRect.width - 20f, 24f);
            string metText = $"Current Metabolic Efficiency: {currentMet}";
            int metabolismLimit = -9;
            if (selectedGene != null)
            {
                string color = resultingMet < metabolismLimit ? "red" : "cyan";
                metText += $" -> <color={color}>{resultingMet}</color> (Min: {metabolismLimit})";
            }
            else
            {
                metText += $" (Min: {metabolismLimit})";
            }
            Widgets.Label(statusRect, metText);

            if (resultingMet < metabolismLimit)
            {
                Rect warningRect = new Rect(10f, statusRect.yMax - 2f, inRect.width - 20f, 20f);
                GUI.color = ColorLibrary.RedReadable;
                Widgets.Label(warningRect, "Warning: Metabolism will be too low. Random genes will be removed!");
                GUI.color = Color.white;
            }

            // Buttons
            float buttonY = inRect.height - 40f;
            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 160f, buttonY, 150f, 38f), "Cancel"))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 10f, buttonY, 150f, 38f), "Incorporate Gene"))
            {
                Accept();
            }
        }

        private float CalculateAvailableScrollHeight(int cols)
        {
            if (filteredGenes.Count == 0) return 30f;
            float geneHeight = 68f;
            float gap = 4f;
            return 28f + Mathf.CeilToInt(filteredGenes.Count / (float)cols) * (geneHeight + gap);
        }

        private float CalculateCasterScrollHeight(int cols)
        {
            float geneHeight = 68f;
            float gap = 4f;
            float h = 0f;
            if (casterEndogenes.Count > 0)
            {
                h += Mathf.CeilToInt(casterEndogenes.Count / (float)cols) * (geneHeight + gap);
            }
            if (casterEndogenes.Count > 0 && casterXenogenes.Count > 0)
            {
                h += 12f;
            }
            if (casterXenogenes.Count > 0)
            {
                h += Mathf.CeilToInt(casterXenogenes.Count / (float)cols) * (geneHeight + gap);
            }
            return Mathf.Max(h, 40f);
        }

        public override void PostClose()
        {
            lastWindowSize = windowRect.size;
            base.PostClose();
        }

        private void Accept()
        {
            if (selectedGene == null) return;

            GainGene(caster, selectedGene);
            HumanoidPawnScaler.GetInvalidateLater(caster);
            this.Close();
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

        public static void GainGene(Pawn pawn, GeneDef gene)
        {
            if (BodyShapeGeneNames.Contains(gene.defName))
            {
                foreach (var bodyShapeGene in pawn.genes.GenesListForReading.Where(x => BodyShapeGeneNames.Contains(x.def.defName)))
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
    }
}
