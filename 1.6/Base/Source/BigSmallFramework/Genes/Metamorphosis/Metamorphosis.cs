using BigAndSmall;
using BigAndSmall.FilteredLists;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class MorphTarget
    {
        // Morph result settings
        public ThingDef raceThingDef = null;
        public XenotypeDef xenotype;
        public List<HediffDef> hediffDefs;
        public List<GeneDef> endoGenes;
        public List<GeneDef> xenoGenes;

        // Targeting priority type/settings
        public bool isRetromorph = false;
        public Gender? preferredGenders = null;
        public float morphWeight = 1f;

        public void ExecuteMorph(Pawn pawn)
        {
            if (xenotype != null)
            {
                GeneHelpers.ChangeXenotypeFast(pawn, xenotype);
            }
            if (raceThingDef != null)
            {
                RaceMorpher.SwapThingDef(pawn, raceThingDef, true, RaceMorpher.withoutSourcePriority, force: true);
            }
            if (endoGenes != null)
            {
                foreach (var gene in endoGenes)
                {
                    pawn.genes.AddGene(gene, false);
                }
            }
            if (xenoGenes != null)
            {
                foreach (var gene in xenoGenes)
                {
                    pawn.genes.AddGene(gene, true);
                }
            }
            if (hediffDefs != null)
            {
                foreach (var hediffDef in hediffDefs)
                {
                    pawn.health.AddHediff(hediffDef);
                }
            }
        }

        public float GetMorphWeight()
        {
            float weight = morphWeight;
            if (xenotype != null)
            {
                weight += xenotype.GetMorphWeight();
            }
            return weight;
        }

        public Gender GetPrefferedGender()
        {
            if (xenotype != null)
            {
                var both = xenotype.modExtensions?.Any(mx => mx is XenotypeExtension ex && ex.morphIgnoreGender) == true;
                if (both)
                    return Gender.Male | Gender.Female;

                var xenoGenes = xenotype.genes;
                var femalePrio = xenotype.genes.Any(x => x == BSDefs.Body_FemaleOnly || x.defName == "AG_Female");
                if (femalePrio)
                    return Gender.Female;
                var malePrio = xenotype.genes.Any(x => x == BSDefs.Body_MaleOnly || x.defName == "AG_Male");
                if (malePrio)
                    return Gender.Male;
            }
            return Gender.None;
        }
        
    }
    public class MorphSettings
    {
        #region Standard Morph Settings
        public bool isRetromorph = false;
        protected bool requiresFrequentChecks = false;
        /// <summary>
        /// Requires the conditional stat effector to evaluate to true for the morph to be allowed.
        /// </summary>
        public List<ConditionalStatAffecter> conditionals = null;
        public List<HediffDef> requiredHediffs = null;
        public List<HediffDef> disallowedHediffs = null;

        public List<GeneDef> requiredGenes = null;
        public List<GeneDef> disallowedGenes = null;

        public FilterListSet<ThingDef> raceFilter = null;

        public int? morphOverAge = null;
        public int? morphUnderAge = null;
        public bool morphIfPregnant = false;
        public bool morphIfNight = false;
        public bool morphIfDay = false;
        #endregion

        public bool RequiresFrequentChecks => requiresFrequentChecks || morphIfDay || morphIfNight;

        public bool CanMorph(Pawn pawn)
        {
            if (morphOverAge != null)
                if (pawn.ageTracker.AgeBiologicalYears < morphOverAge)
                    return false;

            if (morphUnderAge != null)
                if (pawn.ageTracker.AgeBiologicalYears >= morphUnderAge)
                    return false;

            if (morphIfPregnant)
                if (!pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman))
                    return false;

            if (morphIfDay)
                if (pawn.Map.skyManager.CurSkyGlow < 0.3f)
                    return false;

            if (morphIfNight)
                if (pawn.Map.skyManager.CurSkyGlow > 0.3f)
                    return false;

            if (conditionals != null)
                if (!ConditionalManager.TestConditionals(pawn, conditionals))
                    return false;
            
            if (requiredHediffs != null)
                foreach (var hediff in requiredHediffs)
                    if (!pawn.health.hediffSet.HasHediff(hediff))
                        return false;
                
            if (disallowedHediffs != null)
                foreach (var hediff in disallowedHediffs)
                    if (pawn.health.hediffSet.HasHediff(hediff))
                        return false;
                
            var activeGenes = GeneHelpers.GetAllActiveGeneDefs(pawn); ;
                if (requiredGenes != null)
                foreach (var gene in requiredGenes)
                    if (!activeGenes.Contains(gene))
                        return false;  
                
            if (disallowedGenes != null)
                foreach (var gene in disallowedGenes)
                    if (activeGenes.Contains(gene))
                        return false;
            
            if (raceFilter != null)
                if (raceFilter.GetFilterResult(pawn.def).Denied())
                    return false;
                
            return true;
        }
    }

    public static class Metamorphosis
    {
        public static bool ValidToMorph(List<PawnExtension> pawnExtensions)
        {
            return pawnExtensions.Any(x => x.morphSettings != null);
        }

        public static MorphTarget TryGetMorphTarget(Pawn pawn, IEnumerable<MorphSettings> triggers)
        {
            bool? metamorphValid = null;
            bool? retromorphValid = null;
            List<MorphSettings> customMorphs = [];
            foreach (var trigger in triggers)
            {
                var result = trigger.CanMorph(pawn);
                {
                    if (trigger.isRetromorph == true)
                    {
                        retromorphValid ??= true;
                        retromorphValid &= result;
                    }
                    else
                    {
                        metamorphValid ??= true;
                        metamorphValid &= result;
                    }
                }
            }
            if (metamorphValid == null && retromorphValid == null)
            {
                return null;
            }
            var allPawnExts = pawn.GetAllPawnExtensions();
            var allMetaMorphs = allPawnExts.Where(x => x.morphTargets != null).SelectMany(x => x.morphTargets).ToList();
            if (metamorphValid == true)
            {
                var metamorphTargets = allMetaMorphs.Where(x => !x.isRetromorph).ToList();
                if (metamorphTargets.Count != 0)
                {
                    var pickList = TryFilterByGender(pawn?.gender, metamorphTargets).ToList();
                    var result = pickList.RandomElementByWeight(x=> x.GetMorphWeight());
                    return result;
                }
            }
            if (retromorphValid == true)
            {
                var retroMorphTargets = allMetaMorphs.Where(x => x.isRetromorph).ToList();
                if (retroMorphTargets.Count != 0)
                {
                    var pickList = TryFilterByGender(pawn?.gender, retroMorphTargets);
                    var result = pickList.RandomElementByWeight(x => x.GetMorphWeight());
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Tries to filter the options based on gender. E.g. females will prioritise xenotypes with forced-female.
        /// </summary>
        private static List<MorphTarget> TryFilterByGender(Gender? gender, List<MorphTarget> defs)
        {
            var byPrefferedGenderDict = defs.GroupBy(x => x.GetPrefferedGender()).ToDictionary(x => x.Key, x => x.ToList());
            var femalePrio = byPrefferedGenderDict.TryGetValue(Gender.Female, out var fList) ? fList : [];
            var malePrio = byPrefferedGenderDict.TryGetValue(Gender.Male, out var mList) ? mList : [];
            var ignoresPrio = byPrefferedGenderDict.TryGetValue(Gender.Male | Gender.Female, out var iList) ? iList : [];

            var femaleOptions = femalePrio.Union(ignoresPrio);
            var maleOptions = malePrio.Union(ignoresPrio);

            // If there are prioritised options return only those.
            if (gender == Gender.Female && femaleOptions.Count() > 0)
                return[..femaleOptions];
            else if (gender == Gender.Male && maleOptions.Count() > 0)
                return [.. maleOptions];

            // For cases where there are not gender-prioritsed options for one gender, but there are for the other.
            // Simply exclude those prioritised for the opposite
            else if (gender == Gender.Female && malePrio.Count() > 0)
            {
                var femaleFallback = defs.Except(malePrio);
                if (femaleFallback.Any())
                    return [.. femaleFallback];
            }
            else if (gender == Gender.Male && femalePrio.Count() > 0)
            {
                var maleFallback = defs.Except(femalePrio);
                if (maleFallback.Any())
                    return [.. maleFallback];
            }
            return defs;
        }
        

        public static HashSet<Pawn> pawnsQueuedForMorphing = [];
        public static void HandleMetamorph(Pawn pawn, List<PawnExtension> pawnExts)
        {
            if (pawnExts.Count == 0 || pawnsQueuedForMorphing.Contains(pawn))
            {
                return;
            }
            
            var withMorphTriggers = pawnExts.Where(x => x.morphSettings != null);

            if (withMorphTriggers.Any())
            {
                var triggers = withMorphTriggers.Select(x => x.morphSettings);
                var metamorphTarget = TryGetMorphTarget(pawn, triggers);
                if (metamorphTarget == null)
                {
                    return;
                }
                pawnsQueuedForMorphing.Add(pawn);
                void morphAction()
                {
                    pawnsQueuedForMorphing.Remove(pawn);
                    metamorphTarget.ExecuteMorph(pawn);
                }
                BigAndSmallCache.queuedJobs.Enqueue(morphAction);
            }
        }

        
    }
}
