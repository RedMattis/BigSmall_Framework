using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Verse;

namespace BigAndSmall
{

    [HarmonyPatch]
    public static class NotifyGenesChanges
    {
        [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
        [HarmonyPostfix]
        public static void Notify_GenesChanged_Postfix(GeneDef addedOrRemovedGene, Pawn_GeneTracker __instance)
        {
            //try
            //{
            //    PGene.UpdateOverridenGenes(addedOrRemovedGene, __instance);
            //}
            //catch (Exception e)
            //{
            //    Log.Warning(e.ToString());
            //}

            // Update the Cache
            if (__instance?.pawn != null)
            {
                HumanoidPawnScaler.GetInvalidateLater(__instance.pawn, scheduleForce: 1);
                GenderMethods.UpdatePawnHairAndHeads(__instance.pawn);

                if (__instance?.pawn?.Drawer?.renderer != null && __instance.pawn.Spawned)
                    __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        [HarmonyPatch(typeof(Gene), nameof(Gene.OverrideBy))]
        [HarmonyPostfix]
        public static void Gene_OverrideBy_Patch(Gene __instance, Gene overriddenBy)
        {
            if (!BigSmall.performScaleCalculations) return;
            bool overriden = overriddenBy != null;
            Gene gene = __instance;
            if (gene != null && gene.pawn != null && gene.pawn.Spawned)
            {
                GeneEffectManager.GainOrRemovePassion(overriden, gene);

                GeneEffectManager.GainOrRemoveAbilities(overriden, gene);

                GeneEffectManager.ApplyForcedTraits(overriden, gene);


                if (gene?.pawn != null)
                {
                    HumanoidPawnScaler.GetCache(gene.pawn, scheduleForce: 1);
                }
            }
        }

        [HarmonyPatch(typeof(Gene), "PostRemove")]
        [HarmonyPostfix]
        public static void Gene_PostRemovePatch(Gene __instance)
        {
            if (!PawnGenerator.IsBeingGenerated(__instance.pawn) && __instance.Active)
            {
                HumanoidPawnScaler.GetInvalidateLater(__instance.pawn);
                if (__instance.Active)
                {
                    GeneRequestThingSwap(__instance, false);
                }

                var geneExt = __instance.def.ExtensionsOnDef<PawnExtension, GeneDef>();
                if (geneExt.NullOrEmpty())
                {
                    var pawn = __instance.pawn;
                    bool lastActiveOfDef = !pawn.genes.GenesListForReading.Any(x => x.def == __instance.def && x != __instance);
                    if (lastActiveOfDef)
                    {
                        foreach (var geneDef in geneExt.Where(x => x.hiddenGenes != null).SelectMany(x => x.hiddenGenes))
                        {
                            // Remove all genes matching def
                            var matchingGenes = pawn.genes.GenesListForReading.Where(x => x.def == geneDef).ToList(); ;
                            foreach (var gene in matchingGenes)
                            {
                                pawn.genes.RemoveGene(gene);
                            }
                        }
                    }
                    HumanoidPawnScaler.GetInvalidateLater(__instance.pawn);
                }
            }
        }

        [HarmonyPatch(typeof(Gene), nameof(Gene.PostAdd))]
        [HarmonyPostfix]
        public static void Gene_PostAddPatch(Gene __instance)
        {
            var pawn = __instance?.pawn;
            if (pawn?.genes != null)
            {
                HumanoidPawnScaler.GetInvalidateLater(__instance.pawn);

                bool needsReevaluation = false;
                var geneExt = __instance.def.ExtensionsOnDef<PawnExtension, GeneDef>();
                bool xenoGene = pawn.genes.Xenogenes.Any(x => x == __instance);
                foreach (var geneDef in geneExt.SelectMany(x => x.hiddenGenes))
                {
                    pawn.genes.AddGene(geneDef, xenoGene);
                }

                if (geneExt.Any(x => x.FrequentUpdate))
                {
                    needsReevaluation = true;
                }
                if (__instance.Active)
                {
                    GeneRequestThingSwap(__instance, true);
                }
                if (needsReevaluation)
                {
                    BigAndSmallCache.frequentUpdateGenes[__instance] = true;
                }

                HumanoidPawnScaler.GetInvalidateLater(__instance.pawn);
            }
        }

        public static void GeneRequestThingSwap(Gene gene, bool state)
        {
            var exts = gene.def.GetAllPawnExtensionsOnGene();
            if (exts.Any(x => x.thingDefSwap != null))
            {
                
                var firstValid = exts.Where(x => x.thingDefSwap != null).Select(x => x.thingDefSwap).First();
                RaceMorpher.SwapThingDef(gene.pawn, firstValid, state, targetPriority: 0, source: gene);
            }
        }

        [HarmonyPatch(typeof(Gene), nameof(Gene.ExposeData))]
        [HarmonyPostfix]
        public static void Gene_ExposeDataPatch(Gene __instance)
        {
            if (__instance?.pawn != null && __instance.def.GetAllPawnExtensionsOnGene().Any(x => x.FrequentUpdate))
            {
                BigAndSmallCache.frequentUpdateGenes[__instance] = __instance?.Active;
            }
        }
    }

    // Harmony class which postfixes the GetDescriptionFull method to insert a description which informs the user about what the sets of prerequisites, their type, and their contents in a human-readable way.
    [HarmonyPatch(typeof(GeneDef), "GetDescriptionFull")]
    public static class GeneDef_GetDescriptionFull
    {
        public static void Postfix(ref string __result, GeneDef __instance)
        {
            if (__instance.HasModExtension<ProductionGeneSettings>())
            {
                var geneExtension = __instance.GetModExtension<ProductionGeneSettings>();
                StringBuilder stringBuilder = new();

                stringBuilder.AppendLine(__result);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("BS_ProductionTooltip".Translate(geneExtension.baseAmount, geneExtension.product.LabelCap.AsTipTitle(), geneExtension.frequencyInDays));

                __result = stringBuilder.ToString();
            }

            if (__instance.HasModExtension<GenePrerequisites>())
            {
                var geneExtension = __instance.GetModExtension<GenePrerequisites>();
                if (geneExtension.prerequisiteSets != null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(__result);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(("BP_GenePrerequisites".Translate() + ":").Colorize(ColoredText.TipSectionTitleColor));
                    foreach (var prerequisiteSet in geneExtension.prerequisiteSets)
                    {
                        if (prerequisiteSet.prerequisites != null)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine(($"BP_{prerequisiteSet.type}".Translate() + ":").Colorize(GeneUtility.GCXColor));
                            foreach (var prerequisite in prerequisiteSet.prerequisites)
                            {
                                var gene = DefDatabase<GeneDef>.GetNamedSilentFail(prerequisite);
                                if (gene != null)
                                {
                                    stringBuilder.AppendLine(" - " + gene.LabelCap);
                                }
                                else
                                {
                                    stringBuilder.AppendLine($" - {prerequisite} ({"BP_GeneNotFoundInGame".Translate()})");
                                }
                            }
                        }
                    }
                    __result = stringBuilder.ToString();
                }
            }

            //if (__instance.HasModExtension<GeneSuppressor_Gene>())
            //{
            //    var suppressExtension = __instance.GetModExtension<GeneSuppressor_Gene>();
            //    if (suppressExtension.supressedGenes != null)
            //    {
            //        StringBuilder stringBuilder = new();
            //        stringBuilder.AppendLine(__result);
            //        stringBuilder.AppendLine();
            //        stringBuilder.AppendLine(("BP_GenesSuppressed".Translate() + ":").Colorize(ColoredText.TipSectionTitleColor));
            //        foreach (var geneDefName in suppressExtension.supressedGenes)
            //        {
            //            var gene = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
            //            if (gene != null)
            //            {
            //                stringBuilder.AppendLine(" - " + gene.LabelCap);
            //            }
            //            else
            //            {
            //                stringBuilder.AppendLine($" - {geneDefName} ({"BP_GeneNotFoundInGame".Translate()})");
            //            }
            //        }
            //        __result = stringBuilder.ToString();
            //    }
            //}

            if (__instance.HasModExtension<PawnExtension>())
            {
                var pawnExt = __instance.GetModExtension<PawnExtension>();

                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine(__result);

                if (PawnExtensionExtension.TryGetDescription([pawnExt], out string description))
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(description);
                }


                //if (geneExt.conditionals != null)
                //{
                //    stringBuilder.AppendLine();
                //    stringBuilder.AppendLine(("BP_ActiveOnCondition".Translate() + ":").Colorize(ColoredText.TipSectionTitleColor));
                //    foreach (var conditional in geneExt.conditionals)
                //    {
                //        stringBuilder.AppendLine(" - " + conditional.Label);
                //    }
                //}
                //if (geneExt.sizeByAge != null)
                //{
                //    stringBuilder.AppendLine();
                //    stringBuilder.AppendLineTagged(("SizeOffsetByAge".Translate().CapitalizeFirst() + ":").Colorize(ColoredText.TipSectionTitleColor));
                //    stringBuilder.AppendLine(geneExt.sizeByAge.Select((CurvePoint pt) => "PeriodYears".Translate(pt.x).ToString() + ": +" + pt.y.ToString()).ToLineList("  - ", capitalizeItems: true));
                //}

                //var effectorB = geneExt.GetAllEffectorDescriptions();
                //stringBuilder.Append(effectorB);



                __result = stringBuilder.ToString();
            }
        }
    }



    
}