using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public static class AlienApperanceUtils
    {
        public enum AlienState
        {
            VeryAlien,
            LittleAlien,
            Neutral,
        }

        public static bool GeneIsSimilar(GeneDef geneA, GeneDef geneB)
        {
            if (geneA == geneB)
            {
                return true;
            }
            var alienGrpsDefs = GlobalSettings.GetAlienGeneGroups();
            foreach (var group in alienGrpsDefs)
            {
                if (group.Contains(geneA) && group.Contains(geneB))
                {
                    return true;
                }
            }
            return false;
        }

        public static ThoughtState GetAlienApperanceThoughtState(List<GeneDef> targetGenes, AlienState targetApperance, List<GeneDef> observerGenes, AlienState observerApperance, int offset=0)
        {
            bool bothAlien = false;
            bool differentLevel = false;
            int sharedRomanceChanceGenes = 0;
            int unsharedRomanceChanceGenes = 0;
            if (observerApperance != AlienState.Neutral && targetApperance != observerApperance)
            {
                bothAlien = true;
                differentLevel = true;
            }
            else if (targetApperance == observerApperance)
            {
                bothAlien = true;
            }

            observerGenes.Where(x => x != BSDefs.BS_AlienApperanceStandards &&
                                     x != BSDefs.BS_AlienApperanceStandards_Lesser &&
                                     x.missingGeneRomanceChanceFactor < 1).ToList().ForEach(x =>
            {
                if (!targetGenes.Any(y => GeneIsSimilar(x, y)))
                {
                    unsharedRomanceChanceGenes++;
                }
                else
                {
                    sharedRomanceChanceGenes++;
                }
            });

            targetGenes.Where(x => x != BSDefs.BS_AlienApperanceStandards &&
                                   x != BSDefs.BS_AlienApperanceStandards_Lesser &&
                                   x.missingGeneRomanceChanceFactor < 1).ToList().ForEach(x =>
            {
                if (!observerGenes.Any(y => GeneIsSimilar(x, y)))
                {
                    unsharedRomanceChanceGenes++;
                }
            });

            float percentShared = 0;
            if (sharedRomanceChanceGenes != 0)
            {
                percentShared = (float)sharedRomanceChanceGenes / (sharedRomanceChanceGenes + unsharedRomanceChanceGenes);
                percentShared -= differentLevel ? 0.25f : 0;
            }
            else if (bothAlien)
            {
                // Handle the case where both are alien but have no "missing romance" genes at all.
                if (differentLevel && unsharedRomanceChanceGenes == 0 && sharedRomanceChanceGenes == 0)
                {
                    percentShared = 0.26f;
                }
                else if (unsharedRomanceChanceGenes == 0)
                {
                    percentShared = 1;
                }
            }

            ThoughtState result;
            if (bothAlien)
            {
                result = percentShared switch
                {
                    float n when n > 0.9 => ThoughtState.ActiveAtStage(0 + offset), // Small Bonus.
                    float n when n > 0.44 => ThoughtState.ActiveAtStage(1 + offset), // Tiny bonus
                    float n when n > 0.24 => ThoughtState.ActiveAtStage(3 + offset),
                    float n when n > 0.0 => ThoughtState.ActiveAtStage(4 + offset),
                    _ => ThoughtState.ActiveAtStage(5 + offset), // Max Penalty.
                };
            }
            else
            {
                result = percentShared switch
                {
                    float n when n > 0.44 => ThoughtState.ActiveAtStage(2 + offset),
                    float n when n > 0.19 => ThoughtState.ActiveAtStage(4 + offset),
                    _ => ThoughtState.ActiveAtStage(5 + offset),
                };
            }

            return result;
        }
    }

    public class ThoughtWorker_AlienApperance : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn observingPawn, Pawn targetPawn)
        {
            if (!targetPawn.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(observingPawn, targetPawn))
            {
                return false;
            }
            if (PawnUtility.IsBiologicallyOrArtificiallyBlind(observingPawn))
            {
                return false;
            }
            if (targetPawn.genes is Pawn_GeneTracker tgtGenes && observingPawn.genes is Pawn_GeneTracker obGenes)
            {
                var tgtActiveGenes = GeneHelpers.GetAllActiveGeneDefs(targetPawn);
                var obActiveGenes = GeneHelpers.GetAllActiveGeneDefs(observingPawn);

                var targetApperance = AlienApperanceUtils.AlienState.Neutral;
                if (tgtActiveGenes.Contains(BSDefs.BS_AlienApperanceStandards))
                    targetApperance = AlienApperanceUtils.AlienState.VeryAlien;
                else if (tgtActiveGenes.Contains(BSDefs.BS_AlienApperanceStandards_Lesser))
                    targetApperance = AlienApperanceUtils.AlienState.LittleAlien;

                var observerApperance = AlienApperanceUtils.AlienState.Neutral;
                if (obActiveGenes.Contains(BSDefs.BS_AlienApperanceStandards))
                    observerApperance = AlienApperanceUtils.AlienState.VeryAlien;
                else if (obActiveGenes.Contains(BSDefs.BS_AlienApperanceStandards_Lesser))
                    observerApperance = AlienApperanceUtils.AlienState.LittleAlien;


                if (!(targetApperance == AlienApperanceUtils.AlienState.Neutral && targetApperance == observerApperance)) // If both are neutral, no need to check.
                {
                    bool someoneIsVeryAlien = targetApperance == AlienApperanceUtils.AlienState.VeryAlien || observerApperance == AlienApperanceUtils.AlienState.VeryAlien;

                    int offsetState = 0;
                    if (someoneIsVeryAlien)
                    {
                        offsetState = 6;
                        int tgtBeauty = (int)targetPawn.GetStatValue(StatDefOf.PawnBeauty);
                        if (tgtBeauty > 0)
                        {
                            offsetState = 6 * tgtBeauty + 6;
                        }
                    }
                    var result = AlienApperanceUtils.GetAlienApperanceThoughtState(tgtActiveGenes.ToList(), targetApperance, obActiveGenes.ToList(), observerApperance, offsetState); ;
                    return result;
                }
            }
            return false;
        }
    }

    //public class ThoughtWorker_AlienApperance : ThoughtWorker
    //{
    //    protected override ThoughtState CurrentSocialStateInternal(Pawn observingPawn, Pawn targetPawn)
    //    {
    //        if (!targetPawn.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(observingPawn, targetPawn))
    //        {
    //            return false;
    //        }
    //        if (PawnUtility.IsBiologicallyOrArtificiallyBlind(observingPawn))
    //        {
    //            return false;
    //        }
    //        if (targetPawn.genes is Pawn_GeneTracker tgtGenes && observingPawn.genes is Pawn_GeneTracker obGenes)
    //        {
    //            var tgtActiveGenes = GeneHelpers.GetAllActiveGeneDefs(targetPawn);
    //            var obActiveGenes = GeneHelpers.GetAllActiveGeneDefs(observingPawn);
    //            if (tgtActiveGenes.Contains(BSDefs.BS_AlienApperanceStandards) || obActiveGenes.Contains(BSDefs.BS_AlienApperanceStandards))
    //            {
    //                return AlienApperanceUtils.GetAlienApperanceThoughtState(tgtActiveGenes, obActiveGenes, false, 0);
    //            }
    //        }
    //        return false;
    //    }
    //}
}
