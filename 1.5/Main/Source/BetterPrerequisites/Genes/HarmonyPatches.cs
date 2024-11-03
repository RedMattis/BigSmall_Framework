using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace BetterPrerequisites
{
    [HarmonyPatch]
    public static class BSVanillaPatches
    {
        [HarmonyPatch(typeof(LifeStageWorker), nameof(LifeStageWorker.Notify_LifeStageStarted))]

        public static void Post_Notify_LifeStageStarted(Pawn pawn)
        {
            if (pawn.genes != null)
            {
                List<Gene> genes = pawn.genes.GenesListForReading;
                foreach (Gene gene in genes.Where(x => x.Active))
                {
                    GeneEffectManager.RefreshGeneEffects(gene, true);
                }
            }
            FastAcccess.GetCache(pawn, force: true);
        }
    }

    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostRemoved))]
    public static class Hediff_PostRemove
    {
        public static void Postfix(Hediff __instance)
        {
            bool supressMngrChangeMade = false;
            if (__instance.pawn != null)
            {
                if (GeneSuppressorManager.supressedGenesPerPawn_Hediff.Keys.Contains(__instance.pawn))
                {
                    var suppressDict = GeneSuppressorManager.supressedGenesPerPawn_Hediff[__instance.pawn];
                    // Remove the Hediff from the Suppressors in the dictionary list.
                    foreach (var key in suppressDict.Keys)
                    {
                        if (suppressDict[key].Contains(__instance.def))
                        {
                            suppressDict[key].Remove(__instance.def);
                            supressMngrChangeMade = true;
                        }
                    }
                    // Remove all dictionary entries with no suppressors.
                    foreach (var key in suppressDict.Keys.ToList())
                    {
                        if (suppressDict[key].Count == 0)
                        {
                            suppressDict.Remove(key);
                            supressMngrChangeMade = true;
                        }
                    }
                }
            }
            if (__instance?.pawn?.Drawer?.renderer != null && __instance.pawn.Spawned)
            {
                if (supressMngrChangeMade)
                {
                    __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                HumanoidPawnScaler.GetCache(__instance.pawn, scheduleForce: 1);
            }
        }
    }

    [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostAdd))]
    public static class Hediff_PostAdd
    {
        public static void Postfix(Hediff __instance, DamageInfo? dinfo)
        {
            var raceProps = __instance?.pawn?.RaceProps;
            if (raceProps == null || __instance?.pawn?.RaceProps?.Animal == true)
            {
                return;
            }
            var genes = __instance?.pawn?.genes;
            if (genes == null) return;
            var geneList = genes.GenesListForReading;
            if (genes != null && geneList.Count > 0)
            {
                bool changeMade = false;
                try
                {
                    PGene.supressPostfix = true;
                    changeMade = GeneSuppressorManager.TryAddSuppressor(__instance, __instance.pawn);
                }
                finally
                {
                    PGene.supressPostfix = false;
                }
                if (__instance?.pawn?.Drawer?.renderer != null && __instance.pawn.Spawned)
                {
                    if (changeMade)
                    {
                        __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
                    }
                    HumanoidPawnScaler.GetCache(__instance.pawn, scheduleForce: 1);
                }

            }
        }
    }



    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Pawn_SpawnSetup
    {
        public static void Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            if (!respawningAfterLoad)
            {
                float? foodNeed = __instance?.needs?.food?.CurLevelPercentage;
                Pawn_PostMapInit.RefreshPawnGenes(__instance, forceRefresh: true);
                if (foodNeed != null)
                {
                    __instance.needs.food.CurLevelPercentage = foodNeed.Value;
                }

                __instance.def.modExtensions?.OfType<RaceExtension>()?.FirstOrDefault()?.ApplyTrackerIfMissing(__instance);
            }
        }
    }

    //[HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    //public static class SetFaction
    //{
    //    public static void Postfix(Pawn __instance, Faction newFaction)
    //    {
    //        if(__instance?.Drawer != null && HumanoidPawnScaler.GetCacheUltraSpeed(__instance) is BSCache cache)
    //        {
    //            cache.ReevaluateGraphics();
    //        }
    //    }
    //}


    // When the game is loaded, go through all hedifs in the pawns health tab and try to add supressors
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostMapInit))]
    public static class Pawn_PostMapInit
    {
        public static void Postfix(Pawn __instance)
        {
            RefreshPawnGenes(__instance, forceRefresh: true);
        }

        public static void RefreshPawnGenes(Pawn __instance, bool forceRefresh = true)
        {
            if (__instance != null)
            {
                foreach (var hediff in __instance.health.hediffSet.hediffs)
                {
                    try
                    {
                        PGene.supressPostfix = true;
                        GeneSuppressorManager.TryAddSuppressor(hediff, __instance);
                    }
                    finally
                    {
                        PGene.supressPostfix = false;
                    }
                }
                UpdatePawnHairAndHeads(__instance);

                foreach (var gene in GeneHelpers.GetAllActiveGenes(__instance))
                {
                    GeneEffectManager.RefreshGeneEffects(gene, activate: true);
                }
                if (forceRefresh)
                {
                    HumanoidPawnScaler.GetCache(__instance, forceRefresh: true);
                }
            }
            else
            {
                Log.Error("BetterPrerequisites: Someone just called PostMapInit called with null pawn. Probably someone did a whoopsie!");
            }
        }

        public static void UpdatePawnHairAndHeads(Pawn pawn)
        {
            try
            {
                // Get all active genes
                var genes = GeneHelpers.GetAllActiveGenes(pawn);
                if (genes.Count == 0) return;

                // Get style whitelist for hair
                List<string> hairStyleWhitelist = genes.Where(x => x.def.hairTagFilter != null && x.def.hairTagFilter.whitelist).Select(x => x.def.hairTagFilter.tags).SelectMany(x => x).ToList();

                if (hairStyleWhitelist.Count > 0)
                {
                    // Check if the current hairstyle has a matching tag
                    if (pawn?.story?.hairDef?.styleTags.Any(x => hairStyleWhitelist.Contains(x)) == false)
                    {
                        // Get all hairdefs that match the whitelist
                        var hairDefs = DefDatabase<HairDef>.AllDefs.Where(x => x.styleTags.Any(st => hairStyleWhitelist.Contains(st))).ToList();

                        // And gene whitelist
                        hairDefs = hairDefs.Where(x => x.requiredGene == null || genes.Any(g => g.def == x.requiredGene)).ToList();

                        // And required mutants
                        hairDefs = hairDefs.Where(x => x.requiredMutant == null || pawn?.mutant?.Def == x.requiredMutant).ToList();

                        if (hairDefs.Count > 0)
                        {
                            // Get a random hairdef from the whitelist
                            var newHair = hairDefs.RandomElement();
                            pawn.story.hairDef = newHair;

                            //Log.Message(pawn.Name.ToStringShort + " has a new hairdef: " + newHair.defName);
                        }
                        else
                        {
                            //Log.Message(pawn.Name.ToStringShort + " has no valid hairs");
                        }
                    }
                    else
                    {
                        //Log.Message(pawn.Name.ToStringShort + $" has a valid hair ({pawn?.story?.hairDef}, with tags {string.Join(", ", pawn?.story?.hairDef?.styleTags?.ToArray())})");
                    }
                }
                else
                {
                }
            }
            catch
            {

            }

            // Get head whitelist
            // TODO...
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    public static class Pawn_GeneTracker_OverrideAllConflicting
    {
        public static void Postfix(GeneDef addedOrRemovedGene, Pawn_GeneTracker __instance)
        {
            try
            {
                PGene.UpdateOverridenGenes(addedOrRemovedGene, __instance);
            }
            catch (Exception e)
            {
                Log.Warning(e.ToString());
            }

            // Update the Cache
            if (__instance?.pawn != null)
            {
                HumanoidPawnScaler.GetCache(__instance.pawn, scheduleForce: 1);
                Pawn_PostMapInit.UpdatePawnHairAndHeads(__instance.pawn);

                if (__instance?.pawn?.Drawer?.renderer != null && __instance.pawn.Spawned)
                    __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
    }



    //// When a gene's OverrideBy state changes, remove/add gene effects
    [HarmonyPatch(typeof(Gene), nameof(Gene.OverrideBy))]
    public static class Gene_OverrideBy_Patch
    {
        public static void Postfix(Gene __instance, Gene overriddenBy)
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
    }


    [HarmonyPatch(typeof(Gene), "PostRemove")]
    public static class Gene_PostRemovePatch
    {
        public static void Postfix(Gene __instance)
        {
            if (__instance is PGene && !PawnGenerator.IsBeingGenerated(__instance.pawn) && __instance.Active)
            {
                GeneEffectManager.RefreshGeneEffects(__instance, activate: false);
            }
        }
    }



    [HarmonyPatch(typeof(Gene), nameof(Gene.PostAdd))]
    public static class Gene_PostAddPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Gene __instance)
        {
            if (__instance?.pawn?.genes != null)
            {
                Pawn pawn = __instance.pawn;
                // Get all other genes
                var genes = GeneHelpers.GetAllActiveGenes(__instance.pawn);

                // Check if active. This will trigger the checker for prerequisites.
                genes.Where(g => g is PGene pGene).Cast<PGene>().ToList().ForEach(pg => pg.ForceRun = true);

                foreach (var gene in genes)
                {
                    GeneEffectManager.RefreshGeneEffects(gene, activate: true);
                }
            }
        }
    }

    public static class GeneEffectManager
    {
        private static Action<Pawn_GeneTracker, GeneDef> notifyGenesChangedDelegate = null;
        public static Action<Pawn_SkillTracker> dirtyAptitudesDelegate = null;
        public static void GainOrRemovePassion(bool disabled, Gene gene)
        {

            if (gene.def.passionMod != null && gene.def.prerequisite == null)
            {
                if (disabled)
                {
                    SkillRecord skill = gene.pawn.skills.GetSkill(gene.def.passionMod.skill);
                    skill.passion = gene.NewPassionForOnRemoval(skill);
                }
                else
                {
                    SkillRecord skill = gene.pawn.skills.GetSkill(gene.def.passionMod.skill);
                    gene.passionPreAdd = skill.passion;
                    skill.passion = gene.def.passionMod.NewPassionFor(skill);
                }
                int? test = null;
                test.GetValueOrDefault();
                // public void DirtyAptitudes()
                dirtyAptitudesDelegate ??= AccessTools.MethodDelegate<Action<Pawn_SkillTracker>>(AccessTools.Method(typeof(Pawn_SkillTracker), "DirtyAptitudes"));
                dirtyAptitudesDelegate(gene.pawn.skills);

                // Uh... This is questionable. Why are we notifying gene change here?
                //notifyGenesChangedDelegate ??= AccessTools.MethodDelegate<Action<Pawn_GeneTracker, GeneDef>>(AccessTools.Method(typeof(Pawn_GeneTracker), "Notify_GenesChanged"));
                //notifyGenesChangedDelegate(gene.pawn.genes, gene.def);
            }
        }

        public static void GainOrRemoveAbilities(bool disabled, Gene gene)
        {
            try
            {
                if (gene?.pawn?.abilities != null && gene?.def?.abilities != null)
                {
                    foreach (var abillity in gene.def.abilities)
                    {
                        if (disabled)
                        {
                            // Check so no enabled gene grants the abillity.
                            bool enabled = false;
                            foreach (var gene2 in GeneHelpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene))
                            {
                                if (gene2 != gene && gene2?.def?.abilities != null && gene2.def.abilities.Contains(abillity))
                                {
                                    enabled = true;
                                    break;
                                }
                            }
                            if (!enabled)
                            {
                                gene.pawn.abilities?.RemoveAbility(abillity);
                            }

                        }
                        else
                        {
                            gene.pawn.abilities?.GainAbility(abillity);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Error in GainOrRemoveAbilities: {ex.Message}");
            }
        }

        public static void ApplyForcedTraits(bool disabled, Gene gene)
        {
            var pawn = gene.pawn;
            if (!gene.def.forcedTraits.NullOrEmpty() && pawn.story != null)
            {
                foreach (var forcedTrait in gene.def.forcedTraits)
                {
                    if (disabled)
                    {
                        for (int j = 0; j < gene.def.forcedTraits.Count; j++)
                        {
                            Trait trait = new Trait(gene.def.forcedTraits[j].def, gene.def.forcedTraits[j].degree);
                            trait.sourceGene = gene;
                            pawn.story.traits.allTraits.RemoveAll((Trait tr) => tr.def == trait.def && tr.sourceGene == gene);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < gene.def.forcedTraits.Count; j++)
                        {
                            Trait trait = new Trait(gene.def.forcedTraits[j].def, gene.def.forcedTraits[j].degree);
                            trait.sourceGene = gene;
                            pawn.story.traits.GainTrait(trait, suppressConflicts: true);
                        }
                    }
                }
            }
        }

        private static bool? harActive = null;
        private static MethodInfo headTypeFilterMethod = null;
        public static bool canRefreshGeneEffects = false;

        public static bool RefreshGeneEffects(Gene __instance, bool activate, PawnExtension geneExt = null)
        {
            //if (__instance.pawn == null || !__instance.pawn.Spawned)
            //{
            //    return false;
            //}

            if (canRefreshGeneEffects == true && __instance is PGene pGene && __instance?.pawn != null)
            {
                canRefreshGeneEffects = false;
                bool conditionalsValid = ConditionalManager.TestConditionals(pGene);
                bool prerequisitesValid = PrerequisiteValidator.Validate(pGene.def, __instance.pawn);
                if (!conditionalsValid || !prerequisitesValid)
                {
                    activate = false;
                }
                canRefreshGeneEffects = true;
            }


            bool changeMade = false;

            var gene = __instance;

            PawnExtension extension = geneExt ?? gene.def.GetModExtension<PawnExtension>();
            if (extension == null) return false;
            try
            {
                AddHediffToPawn(activate, ref changeMade, gene, extension);
                AddHediffToPart(activate, ref changeMade, gene, extension);

            }
            catch (Exception ex)
            {
                Log.Error($"Error in RefreshGeneEffects: {ex.Message} {ex.StackTrace}");
            }
            return changeMade;
        }

        private static void AddHediffToPart(bool activate, ref bool changeMade, Gene gene, PawnExtension extension)
        {
            if (extension.applyPartHediff != null)
            {
                HashSet<BodyPartRecord> notMissingParts = gene.pawn.health.hediffSet.GetNotMissingParts().ToHashSet();
                foreach (HediffToBodyparts item in extension.applyPartHediff)
                {
                    //int num = 0;
                    if (activate && ConditionalManager.TestConditionals(gene, item.conditionals))
                    {
                        foreach (BodyPartDef bodypart in item.bodyparts)
                        {
                            if (!gene.pawn.RaceProps.body.GetPartsWithDef(bodypart).EnumerableNullOrEmpty()) //  && num <= gene.pawn.RaceProps.body.GetPartsWithDef(bodypart).Count
                            {
                                var allParts = gene.pawn.RaceProps.body.GetPartsWithDef(bodypart).ToArray();
                                foreach (var part in allParts)
                                {
                                    // Use hediffset to check if the part exists
                                    if (notMissingParts.Contains(part))
                                    {
                                        if (part != null && item.hediff != null)
                                        {
                                            // If the pawn already has the hediff on that part, abort.
                                            // Get all hediffs on pawn
                                            var hediffs = gene.pawn.health.hediffSet.hediffs;
                                            // Get all hediffs on the part
                                            var hediffsOnPart = hediffs.Where(x => x.Part == part);
                                            // Check if any of the hediffs on the part is the same as the one we want to add
                                            if (hediffsOnPart.Any(x => x.def == item.hediff))
                                            {
                                                continue;
                                            }

                                            changeMade = true;
                                            gene.pawn.health.AddHediff(item.hediff, part);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (gene.pawn.health.hediffSet.GetFirstHediffOfDefName(item.hediff.defName) != null)
                        {
                            bool found = false;
                            var otherGenes = GeneHelpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene);
                            if (otherGenes.Count() == 0) continue;
                            foreach (var otherGene in otherGenes.Select(x => x.def.GetModExtension<PawnExtension>()).Where(x => x != null))
                            {
                                if (otherGene.applyPartHediff != null)
                                {
                                    foreach (var otherItem in otherGene.applyPartHediff)
                                    {
                                        if (otherItem.hediff.defName == item.hediff.defName)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (found)
                            {
                                continue;
                            }

                            RemoveHediffByName(gene.pawn, item.hediff.defName);
                            changeMade = true;
                        }
                    }
                }
            }

            // Step through each hediff and check so its body-parts still exists. If not, remove it.
            for (int idx = gene.pawn.health.hediffSet.hediffs.Count - 1; idx >= 0; idx--)
            {
                Hediff hediff = gene.pawn.health.hediffSet.hediffs[idx];
                if (hediff.Part != null && !gene.pawn.health.hediffSet.GetNotMissingParts().Contains(hediff.Part))
                {
                    // Check so it isn't a Missing Part Hediff. If so we don't want to remove it.
                    if (hediff.def == HediffDefOf.MissingBodyPart)
                    {
                        continue;
                    }

                    gene.pawn.health.RemoveHediff(hediff);
                    changeMade = true;
                }
            }
        }

        private static void AddHediffToPawn(bool activate, ref bool changeMade, Gene gene, PawnExtension extension)
        {
            List<HediffToBody> toRemove = new List<HediffToBody>();
            if (extension.applyBodyHediff != null)
            {
                foreach (HediffToBody item in extension.applyBodyHediff)
                {
                    if (item.hediff != null)
                    {
                        if (activate && ConditionalManager.TestConditionals(gene, item.conditionals))
                        {
                            // If the pawn does not have the hediff, add it.
                            if (gene.pawn.health.hediffSet.GetFirstHediffOfDefName(item.hediff.defName) == null)
                            {
                                gene.pawn.health.AddHediff(item.hediff);
                                changeMade = true;
                            }
                        }
                        else
                        {
                            if (gene.pawn.health.hediffSet.GetFirstHediffOfDefName(item.hediff.defName) != null)
                            {
                                // Check all other genes to see if they have the same hediff. If not, remove it.
                                bool found = false;
                                foreach (var otherGene in GeneHelpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene).Select(x => x.def.GetModExtension<PawnExtension>()).Where(x => x != null))
                                {
                                    if (otherGene.applyBodyHediff != null)
                                    {
                                        foreach (var otherItem in otherGene.applyBodyHediff)
                                        {
                                            if (otherItem.hediff.defName == item.hediff.defName)
                                            {
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (found)
                                {
                                    continue;
                                }
                                RemoveHediffByName(gene.pawn, item.hediff.defName);

                                //Log.Message($"DEBUG: Removed {item.hediff.defName} from {gene.pawn.Name}");
                                changeMade = true;
                            }
                        }
                    }
                    else
                    {
                        Log.Warning($"{gene.def.defName} on {gene.pawn} tried to grant a Hediff, but the Hediff was null. This is probably due to an optional \"MayRequire\" hediff, in which case it is harmless.");

                        // Remove it from the list so we don't get spammed with warnings.
                        toRemove.Add(item);
                    }
                }
            }
            if (toRemove.Count > 0)
            {
                for (int idx = toRemove.Count - 1; idx >= 0; idx--)
                {
                    HediffToBody item = toRemove[idx];
                    extension.applyBodyHediff.Remove(item);
                }
            }
        }

        public static void RemoveHediffByName(Pawn pawn, string hediffName)
        {
            var toRemove = new List<Hediff>();
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def.defName == hediffName)
                {
                    toRemove.Add(hediff);
                }
            }
            foreach (var hediff in toRemove)
            {
                pawn.health.RemoveHediff(hediff);
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

            if (__instance.HasModExtension<GeneSuppressor_Gene>())
            {
                var suppressExtension = __instance.GetModExtension<GeneSuppressor_Gene>();
                if (suppressExtension.supressedGenes != null)
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.AppendLine(__result);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(("BP_GenesSuppressed".Translate() + ":").Colorize(ColoredText.TipSectionTitleColor));
                    foreach (var geneDefName in suppressExtension.supressedGenes)
                    {
                        var gene = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
                        if (gene != null)
                        {
                            stringBuilder.AppendLine(" - " + gene.LabelCap);
                        }
                        else
                        {
                            stringBuilder.AppendLine($" - {geneDefName} ({"BP_GeneNotFoundInGame".Translate()})");
                        }
                    }
                    __result = stringBuilder.ToString();
                }
            }

            if (__instance.HasModExtension<PawnExtension>())
            {
                var geneExt = __instance.GetModExtension<PawnExtension>();

                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine(__result);

                if (geneExt.conditionals != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(("BP_ActiveOnCondition".Translate() + ":").Colorize(ColoredText.TipSectionTitleColor));
                    foreach (var conditional in geneExt.conditionals)
                    {
                        stringBuilder.AppendLine(" - " + conditional.Label);
                    }
                }
                if (geneExt.sizeByAge != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLineTagged(("SizeOffsetByAge".Translate().CapitalizeFirst() + ":").Colorize(ColoredText.TipSectionTitleColor));
                    stringBuilder.AppendLine(geneExt.sizeByAge.Select((CurvePoint pt) => "PeriodYears".Translate(pt.x).ToString() + ": +" + pt.y.ToString()).ToLineList("  - ", capitalizeItems: true));
                }

                var effectorB = geneExt.GetAllEffectorDescriptions();
                stringBuilder.Append(effectorB);



                __result = stringBuilder.ToString();
            }
        }
    }
}