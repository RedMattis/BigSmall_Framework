using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;
using System.IO;

namespace BetterPrerequisites
{
    [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostMake))]
    public static class Hediff_PostMake
    {
        public static void Postfix(Hediff __instance)
        {
            if (__instance.pawn != null)
            {
                try
                {
                    PGene.supressPostfix = true;
                    GeneSuppressorManager.TryAddSuppressor(__instance, __instance.pawn);
                }
                finally
                {
                    PGene.supressPostfix = false;
                }
            }
        }
    }


    // When the game is loaded, go through all hedifs in the pawns health tab and try to add supressors
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostMapInit))]
    public static class Pawn_PostMapInit
    {
        public static void Postfix(Pawn __instance)
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
            }
            if (__instance != null)
            {
                HumanoidPawnScaler.GetBSDict(__instance, forceRefresh: true);
            }
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
                HumanoidPawnScaler.GetBSDict(__instance.pawn, forceRefresh: true);
            }
        }
    }


    [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostRemoved))]
    public static class Hediff_PostRemove
    {
        public static void Postfix(Hediff __instance)
        {
            if (__instance.pawn != null)
            {
                var HediffName = __instance.def.defName;
                if (GeneSuppressorManager.supressedGenesPerPawn_Hediff.Keys.Contains(__instance.pawn))
                {
                    var suppressDict = GeneSuppressorManager.supressedGenesPerPawn_Hediff[__instance.pawn];
                    // Remove the Hediff from the Suppressors in the dictionary list.
                    foreach (var key in suppressDict.Keys)
                    {
                        if (suppressDict[key].Contains(HediffName))
                        {
                            suppressDict[key].Remove(HediffName);
                        }
                    }
                    // Remove all dictionary entries with no suppressors.
                    foreach (var key in suppressDict.Keys.ToList())
                    {
                        if (suppressDict[key].Count == 0)
                        {
                            suppressDict.Remove(key);
                        }
                    }
                }
            }
            if (__instance?.pawn != null)
            {
                HumanoidPawnScaler.GetBSDict(__instance.pawn, forceRefresh: true);
            }
        }
    }

    //// When a gene's OverrideBy state changes, remove/add gene effects
    [HarmonyPatch(typeof(Gene), nameof(Gene.OverrideBy))]
    public static class Gene_OverrideBy_Patch
    {
        public static void Postfix(Gene __instance, Gene overriddenBy)
        {
            bool overriden = overriddenBy != null;
            Gene gene = __instance;
            if (gene != null && gene.pawn != null && gene.pawn.Spawned)
            {
                GeneEffectManager.GainOrRemovePassion(overriden, gene);

                GeneEffectManager.GainOrRemoveAbilities(overriden, gene);

                GeneEffectManager.ApplyForcedTraits(overriden, gene);

                try
                {
                    // Check if pawnmorpher is active, if so skip all this since it seems to cause issues.
                    // Edit: Didn't help. No idea what code Pawnmorpher is having issues with.
                    //if (ModsConfig.IsActive("tachyonite.pawnmorpherpublic") == false)
                    //{
                    //}

                    bool change = GeneEffectManager.ToggleFurSkinVisibility(overriden, gene);
                    if (change)
                    {
                        PawnRendering.pawnsQueueForRendering.Add(gene.pawn);
                    }


                }
                catch (Exception e)
                {
                    Log.Warning($"{e.Message}\n{e.StackTrace}");
                }

                if (gene?.pawn != null)
                {
                    HumanoidPawnScaler.GetBSDict(gene.pawn, regenerateIfTimer:true);
                }
            }
        }
    }

    //[HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    //public static class NotifyGenesChanges_Patch
    //{
    //    public static void Postfix(GeneDef addedOrRemovedGene, Pawn_GeneTracker __instance)
    //    {
    //        if (__instance?.pawn.genes != null && __instance.pawn?.story != null)
    //        {
    //            var pawn = __instance.pawn;

    //            var cache = RenderingCacheGC.instance.GetCache(pawn);
    //            if (cache != null && cache.hasFur == false)
    //            {
    //                Color? skinColorOverride = pawn.story.SkinColorBase;
    //                List<Gene> genesListForReading = __instance.GenesListForReading;
    //                for (int i = 0; i < genesListForReading.Count; i++)
    //                {
    //                    Gene gene = genesListForReading[i];
    //                    if (!gene.Overridden && gene.Active)
    //                    {
    //                        if (gene.def.skinColorOverride.HasValue)
    //                        {
    //                            skinColorOverride = gene.def.skinColorOverride;
    //                        }
    //                    }
    //                }
    //                pawn.story.skinColorOverride = skinColorOverride;
    //            }
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(Pawn_GeneTracker), "EnsureCorrectSkinColorOverride")]
    //public static class NotifyGenesChanges_Patch
    //{
    //    public static void Postfix(Pawn_GeneTracker __instance)
    //    {
    //        if (__instance?.pawn.genes != null && __instance.pawn?.story != null)
    //        {
    //            var pawn = __instance.pawn;

    //            var cache = RenderingCacheGC.instance.GetCache(pawn);
    //            if (cache != null && cache.hasFur == false)
    //            {
    //                Color? skinColorOverride = pawn.story.SkinColorBase;
    //                List<Gene> genesListForReading = __instance.GenesListForReading;
    //                for (int i = 0; i < genesListForReading.Count; i++)
    //                {
    //                    Gene gene = genesListForReading[i];
    //                    if (!gene.Overridden && gene.Active)
    //                    {
    //                        if (gene.def.skinColorOverride.HasValue)
    //                        {
    //                            skinColorOverride = gene.def.skinColorOverride;
    //                        }
    //                    }
    //                }
    //                pawn.story.skinColorOverride = skinColorOverride;
    //            }
    //        }
    //    }
    //}



    [HarmonyPatch(typeof(Gene), "PostRemove")]
    public static class VanillaGenesExpanded_Gene_PostRemove_Patch
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
            if (__instance.pawn != null)
            {
                // Get all other genes
                var genes = Helpers.GetAllActiveGenes(__instance.pawn);

                // Check if active. This will trigger the checker for prerequisites.
                genes.Where(g => g is PGene pGene).Cast<PGene>().ToList().ForEach(pg=>pg.ForceRun = true);
            }
        }
    }

    public static class GeneEffectManager
    {
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

                MethodInfo methodInfo = AccessTools.Method("RimWorld.Pawn_GeneTracker:Notify_GenesChanged");
                if (!(methodInfo == null))
                {
                    methodInfo.Invoke(gene.pawn.genes, new object[1] { gene.def });
                }
                //var method = gene.pawn.genes.GetType().GetMethod("Notify_GenesChanged", new Type[1]{typeof(GeneDef) });
                //if (method != null) method.Invoke(gene.pawn.genes, new object[1] { gene });
                else { Log.Message($"Notify_GenesChanged not found"); }
                //gene.pawn.genes.Notify_GenesChanged(gene.def);
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
                            foreach (var gene2 in Helpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene))
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

        public static bool ToggleFurSkinVisibility(bool disabled, Gene gene)
        {
            if (PawnRendering.instance == null) return false;
            var pawn = gene?.pawn;
            if (pawn == null) return false;
            bool change = false;
            if (gene?.pawn?.Spawned != true) return false;

            bool debug = false;
            // Disable Fur
            if (gene.def?.graphicData?.fur != null && pawn.story?.headType != null && gene.pawn.Drawer?.renderer?.graphics != null && PawnRendering.instance != null)
            {
                var allHeads = DefDatabase<HeadTypeDef>.AllDefs;
                var cache = PawnRendering.instance.GetCache(pawn);
                bool headFound = false;

                cache.AddHeadDefName(pawn.story.headType.defName); // Add the current head in case it is not there yet.

                if (disabled)
                {
                    cache.hasFur = false;
                    // If pawn's furskin is this one, remove their fur.
                    if (pawn.story.furDef == gene.def.graphicData.fur)
                    {
                        HeadTypeDef newHead = null;
                        var validHeadDefs = allHeads.Where(x => cache.HeadDefNames.Contains(x.defName));
                        if (cache.HeadDefNames != null && validHeadDefs != null && SetHead(validHeadDefs, ref newHead))
                        {
                            pawn.story.headType = newHead;
                            headFound = true;
                            if (debug)
                                Log.Message($"Swapping to cached {newHead.defName} ({newHead.graphicPath} for {pawn.Name}");
                        }
                        if (!headFound)
                        {
                            // Get a random head that does not require any genes.
                            if (SetHead(GetRandomHeads(), ref newHead))
                            {
                                pawn.story.headType = newHead;
                                cache.HeadDefNames.Add(pawn.story.headType.defName);
                                headFound = true;
                                if (debug)
                                    Log.Message($"Swapping to new head def {newHead.defName} ({newHead.graphicPath} for {pawn.Name}");
                            }
                        }
                        if (!headFound)
                        {
                            Log.Warning($"No head found for {pawn.Name}");
                        }
                        // Disable skin from hair color

                        pawn.story.furDef = null;
                        change = true;
                    }
                }
                // Enable Fur
                else
                {
                    cache.hasFur = true;
                    // If the gene has fur...
                    if (gene.def.graphicData?.fur != null && gene.def.forcedHeadTypes != null)
                    {
                        if (pawn.story.furDef == null || pawn.story.furDef != gene.def.graphicData.fur)
                        {
                            var validCachedHeads = allHeads.Where(x => cache.HeadDefNames.Contains(x.defName) && gene.def.forcedHeadTypes.Contains(x)).ToList(); ;
                            if (cache.HeadDefNames != null && validCachedHeads.Count > 0 && SetHead(validCachedHeads, ref pawn.story.headType))
                            {
                                headFound = true;
                                if (debug)
                                    Log.Message($"Swapping to cached fur {pawn.story.headType.defName} ({pawn.story.headType.graphicPath} for {pawn.Name}");
                            }

                            if (!headFound)
                            {
                                if (!headFound)
                                {
                                    HeadTypeDef newHead = null;
                                    // Get a random head that does not require any genes.
                                    if (SetHead(gene.def.forcedHeadTypes, ref newHead))
                                    {
                                        pawn.story.headType = newHead;
                                        cache.AddHeadDefName(pawn.story.headType.defName);
                                        headFound = true;
                                        if (debug)
                                            Log.Message($"Swapping to new fur {newHead.defName} ({newHead.graphicPath} for {pawn.Name}");
                                    }
                                }
                                if (!headFound)
                                {
                                    Log.Warning($"No headdefs found for {pawn.Name}");
                                }
                            }

                            pawn.story.furDef = gene.def.graphicData.fur;

                            change = true;

                        }
                    }
                }
            }

            //try
            //{
            //    if (gene?.pawn?.story != null)
            //    {
            //        //bool removeFurHair = pawn.story.furDef == null && pawn.story.skinColorOverride == pawn.story.HairColor;

            //        // Call "EnsureCorrectSkinColorOverride" in pawn.genes using reflection.
            //        var ensureCorrectSkinColorOverride = typeof(Pawn_GeneTracker).GetMethod("EnsureCorrectSkinColorOverride", BindingFlags.NonPublic | BindingFlags.Instance);
            //        ensureCorrectSkinColorOverride.Invoke(gene.pawn.genes, null);
            //        //EnsureCorrectSkinColorOverride()

            //        //if(removeFurHair)
            //        //{
            //        //    pawn.story.skinColorOverride = pawn.story.SkinColorBase;
            //        //}
            //    }
            //}
            //catch (Exception e)
            //{
            //    Log.Error($"Error in ToggleFurSkinVisibility: {e.Message}\n {e.StackTrace}");
            //}
            return change;
            List<HeadTypeDef> GetRandomHeads()
            {
                return DefDatabase<HeadTypeDef>.AllDefs.Where((HeadTypeDef x) => x.randomChosen).ToList(); ;
            }
            bool SetHead(IEnumerable<HeadTypeDef> options, ref HeadTypeDef headType)
            {
                Rand.PushState(pawn.thingIDNumber);
                HeadTypeDef newHead = null;
                options.Where((HeadTypeDef h) => CanUseHeadType(h)).TryRandomElementByWeight((HeadTypeDef x) => x.selectionWeight, out newHead);
                if (newHead != null)
                {
                    headType = newHead;
                }
                bool result = headType != null;
                Rand.PopState();
                return headType != null;
            }
            bool CanUseHeadType(HeadTypeDef head)
            {
                if (!head.requiredGenes.NullOrEmpty())
                {
                    if (pawn.genes == null)
                    {
                        return false;
                    }

                    foreach (GeneDef requiredGene in head.requiredGenes)
                    {
                        var validGenes = Helpers.GetActiveGenesByName(pawn, requiredGene.defName);
                        if (validGenes.Count() == 0)
                        {
                            return false;
                        }
                    }
                }
                // Check is path is empty, null, or points at a directory rather than a file. If so, return false.
                var graphicPath = head.graphicPath;
                if (string.IsNullOrEmpty(graphicPath) || graphicPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    Log.Warning($"Found a Headdef with an empty graphicPath: {head.defName}: {head.graphicPath}");
                    return false;
                }

                if (harActive == null)
                {
                    harActive = ModsConfig.IsActive("erdelf.HumanoidAlienRaces");
                }
                if (harActive == true && head.requiredGenes.NullOrEmpty())
                {
                    return HarRaceCompatible(head);
                }

                if (head.gender != 0)
                {
                    return head.gender == pawn.gender;
                }

                return true;
            }
            bool HarRaceCompatible(HeadTypeDef headTypeDef)
            {
                if (headTypeFilterMethod == null)
                {
                    Type alienRaceHarmonyPatches = AccessTools.TypeByName("AlienRace.HarmonyPatches");
                    headTypeFilterMethod = AccessTools.Method(alienRaceHarmonyPatches, "HeadTypeFilter");
                }
                IEnumerable<HeadTypeDef> usableHeadTypes = (IEnumerable<HeadTypeDef>)headTypeFilterMethod.Invoke(obj: null, new object[2] { new List<HeadTypeDef> { headTypeDef }, pawn });
                return usableHeadTypes.Count() > 0;
            }
        }
        private static bool? harActive = null;
        private static MethodInfo headTypeFilterMethod = null;

        public static bool RefreshGeneEffects(Gene __instance, bool activate, GeneExtension geneExt = null)
        {
            //if (__instance.pawn == null || !__instance.pawn.Spawned)
            //{
            //    return false;
            //}

            bool changeMade = false;

            var gene = __instance;

            GeneExtension extension = geneExt ?? gene.def.GetModExtension<GeneExtension>();
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

        private static void AddHediffToPart(bool activate, ref bool changeMade, Gene gene, GeneExtension extension)
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
                                foreach(var part in allParts)
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
                            foreach (var otherGene in Helpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene).Select(x => x.def.GetModExtension<GeneExtension>()).Where(x => x != null))
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

        private static void AddHediffToPawn(bool activate, ref bool changeMade, Gene gene, GeneExtension extension)
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
                                foreach (var otherGene in Helpers.GetAllActiveGenes(gene.pawn).Where(x => x != gene).Select(x => x.def.GetModExtension<GeneExtension>()).Where(x => x != null))
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
                                changeMade = true;
                            }
                        }
                    }
                    else
                    {
                        Log.Warning($"{gene.def.defName} on {gene.pawn} tried to grant a Hediff, but the Hediff was null.");

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
                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.AppendLine(__result);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"An average-sized carrier produces {geneExtension.product.LabelCap} every {geneExtension.frequencyInDays} days.");

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
                    StringBuilder stringBuilder = new StringBuilder();
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

            if (__instance.HasModExtension<GeneExtension>())
            {
                var geneExt = __instance.GetModExtension<GeneExtension>();

                StringBuilder stringBuilder = new StringBuilder();
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

                var effectorB = geneExt.GetAllEffectorDescriptions();
                stringBuilder.Append(effectorB);



                __result = stringBuilder.ToString();
            }
        }
    }

}
