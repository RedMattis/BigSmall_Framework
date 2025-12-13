using BigAndSmall;
using BigAndSmall.FilteredLists;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class MorphSettings
    {
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

        public static XenotypeDef TryGetMorphTarget(Pawn pawn, IEnumerable<MorphSettings> triggers)
        {
            bool? metamorphValid = null;
            bool? retromorphValid = null;
            foreach (var trigger in triggers)
            {
                var result = trigger.CanMorph(pawn);
                {
                    if (trigger.isRetromorph)
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
            if (metamorphValid == true)
            {
                var metamorphTargets = allPawnExts.Where(x=>x.metamorphTarget != null).Select(x => x.metamorphTarget).ToList();
                if (metamorphTargets.Count != 0)
                {
                    var pickList = TryFilterByGender(pawn?.gender, metamorphTargets);
                    var result = pickList.RandomElementByWeight(x=> x.GetMorphWeight());
                    return result;
                }
            }
            if (retromorphValid == true)
            {
                var retroMorphTargets = allPawnExts.Where(x => x.retromorphTarget != null).Select(x => x.retromorphTarget).ToList();
                if (retroMorphTargets.Count != 0)
                {
                    var pickList = TryFilterByGender(pawn?.gender, retroMorphTargets);
                    var result = pickList.RandomElementByWeight(x => x.GetMorphWeight());
                    return result;
                }
            }
            return null;
        }

        private static List<XenotypeDef> TryFilterByGender(Gender? gender, List<XenotypeDef> defs)
        {

            var femaleXenos = defs.Where(x => x.genes.Any(x => x == BSDefs.Body_FemaleOnly || x.defName == "AG_Female") ||
                (x.modExtensions?.Any(mx => mx is XenotypeExtension ex && ex.morphIgnoreGender)) == true).ToList();
            var maleXenos = defs.Where(x => x.genes.Any(x => x == BSDefs.Body_MaleOnly || x.defName == "AG_Male") ||
                (x.modExtensions?.Any(mx => mx is XenotypeExtension ex && ex.morphIgnoreGender)) == true).ToList();

            var femaleLegal = defs.Except(maleXenos);
            var maleLegal = defs.Except(femaleXenos);

            if (gender == Gender.Female && femaleLegal.Count() > 0)
            {
                return femaleLegal.ToList();
            }
            else if (gender == Gender.Male && maleLegal.Count() > 0)
            {
                return maleLegal.ToList();
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
                    GeneHelpers.ChangeXenotypeFast(pawn, metamorphTarget);
                    pawnsQueuedForMorphing.Remove(pawn);
                }
                BigAndSmallCache.queuedJobs.Enqueue(morphAction);
            }
        }

        
    }
}
