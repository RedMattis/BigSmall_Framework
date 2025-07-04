﻿using BigAndSmall;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public static class Metamorphosis
    {
        public class MorphManager
        {
            private readonly List<XenotypeDef> upstreamXenos = [];
            private readonly List<XenotypeDef> downstreamXenos = [];

            public readonly bool canEverMorphUp = false;
            public readonly bool canEverMorphDown = false;

            public int? morphUpRequiresAge = null;
            public int? morphDownRequiresAge = null;
            public bool morphUpRequiresPreg = false;
            public bool morphUpRequiresDay = false;
            public bool morphUpRequiresNight = false;

            public static bool ValidToMorph(List<PawnExtension> geneExts)
            {
                return geneExts.Any(x => x.CanMorphAtAll);
            }

            public XenotypeDef GetMorphTarget(Pawn pawn)
            {
                XenotypeDef result = null;
                var pawnAge = pawn.ageTracker.AgeBiologicalYears;
                
                if (canEverMorphUp && upstreamXenos.Count > 0)
                {
                    bool ageCheck = morphUpRequiresAge == null || pawnAge >= morphUpRequiresAge;
                    bool pregnantCheck = !morphUpRequiresPreg || pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman);
                    bool dayCheck = !morphUpRequiresDay || pawn.Map.skyManager.CurSkyGlow > 0.3f;
                    bool nightCheck = !morphUpRequiresNight || pawn.Map.skyManager.CurSkyGlow < 0.3f;

                    //Log.Message($"{pawn}: MorphChain: Checking if pawn can morph up. Old Enough: {ageCheck}, Pregnancy Check: {pregnantCheck}");
                    if (ageCheck && pregnantCheck && dayCheck && nightCheck)
                    {

                        var pickList = TryFilterByGender(pawn?.gender, upstreamXenos);
                        //Log.Message($"[DEBUG]: {pawn}: MorphChain: Morphing up to {pickList.Count} possible morphs. Total Weight of {pickList.Sum(x => x.GetMorphWeight())}");
                        result = pickList.RandomElementByWeight(x=> x.GetMorphWeight());
                        //Log.Message($"{pawn}: MorphChain: Morphing up to {result.LabelCap}");
                        return result;
                    }
                }
                if (canEverMorphDown && downstreamXenos.Count > 0)
                {
                    bool ageCheck = morphDownRequiresAge == null || pawnAge < morphDownRequiresAge;
                    if (ageCheck)
                    {
                        var pickList = TryFilterByGender(pawn?.gender, downstreamXenos);
                        result = pickList.RandomElementByWeight(x => x.GetMorphWeight());
                        return result;
                    }
                }
                return result;
            }

            private List<XenotypeDef> TryFilterByGender(Gender? gender, List<XenotypeDef> defs)
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

            public MorphManager(List<PawnExtension> geneExts)
            {
                upstreamXenos = geneExts.Where(x => x.metamorphTarget != null)?.Select(x => x.metamorphTarget).ToList();
                downstreamXenos = geneExts.Where(x => x.retromorphTarget != null)?.Select(x => x.retromorphTarget).ToList();
                canEverMorphDown = geneExts.Where(x => x.CanMorphDown).Any();
                canEverMorphUp = geneExts.Where(x => x.CanMorphUp).Any();
                morphUpRequiresAge = geneExts.Where(x => x.metamorphAtAge != null).Max(x => x.metamorphAtAge);
                morphDownRequiresAge = geneExts.Where(x => x.retromorphUnderAge != null).Min(x => x.retromorphUnderAge);
                morphUpRequiresPreg = geneExts.Where(x => x.metamorphIfPregnant).Any();
                morphUpRequiresDay = geneExts.Where(x => x.metamorphIfDay).Any();
                morphUpRequiresNight = geneExts.Where(x => x.metamorphIfNight).Any();
            }
        }
        private static MorphManager GetMorphChain(Pawn pawn, List<PawnExtension> geneExts)
        {
            var pawnGT = pawn.genes;

            if (MorphManager.ValidToMorph(geneExts))
            {
                return new MorphManager(geneExts);
            }
            return null;
        }

        public static HashSet<Pawn> pawnsQueuedForMorphing = [];
        public static void HandleMetamorph(Pawn pawn, List<PawnExtension> geneExts)
        {
            if (geneExts.Count == 0 || pawnsQueuedForMorphing.Contains(pawn))
            {
                return;
            }

            // Filter to morph-related genes.
            geneExts = geneExts.Where(x => x.MorphRelated).ToList();

            if (geneExts.Any(x => x.metamorphTarget != null || x.retromorphTarget != null))
            {
                var mp = GetMorphChain(pawn, geneExts);
                if (mp == null)
                {
                    return;
                }
                var metamorphTarget = mp.GetMorphTarget(pawn);
                if (metamorphTarget != null)
                {
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
}
