using BetterPrerequisites;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static partial class RaceHelper
    {
        public static RaceTracker GetRaceTracker(this Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs.FirstOrDefault(h => h is RaceTracker) as RaceTracker;
        }
        private static HediffComp_Race GetRaceComp(this Pawn pawn)
        {
            return pawn.GetRaceTracker()?.TryGetComp<HediffComp_Race>();
        }
        public static List<PawnExtension> GetRacePawnExtensions(this Pawn pawn)
        {
            List<PawnExtension> pawnRaceExts = ModExtHelper.GetAllExtensions<PawnExtension>(pawn, parentWhitelist: [typeof(RaceTracker)]);

            if (pawnRaceExts.Count > 0)
            {
                return pawnRaceExts;
            }
            else
            {
                return [PawnExtension.defaultPawnExtension];
            }
        }

        public static CompProperties_Race GetRaceCompProps(this Pawn pawn)
        {
            if (GetRaceComp(pawn)?.Props is CompProperties_Race raceCompProps)
            {
                return raceCompProps;
            }
            else return CompProperties_Race.defaultMissingProps;
        }
    }

    public class HediffComp_Race : HediffComp_ColorAndFur
    {
        public CompProperties_Race Props => (CompProperties_Race)props;
        public override void CompPostMake()
        {
            //Props.ValidateLists(parent.pawn);
            GenderMethods.UpdateBodyHeadAndBeardPostGenderChange(Pawn, banNarrow: true);
            base.CompPostMake();
        }
    }

    [HarmonyPatch]
    public static class PawnGeneColorsPatches
    {
        public static bool didWarn = false;
        [HarmonyPatch(typeof(PawnSkinColors), nameof(PawnSkinColors.RandomSkinColorGene))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void RandomSkinColorGene_Postfix(ref GeneDef __result, Pawn pawn)
        {
            //if (pawn.GetRacePawnExtension().randomSkinGenes == null) return;
            List<GeneDef> skinGenes = [];
            foreach (var skinGeneSet in ModExtHelper.GetAllPawnExtensions(pawn)
                .Where(x => x.randomSkinGenes != null).Select(x=>x.randomSkinGenes))
            {
                skinGenes.AddRange(skinGeneSet);
            }
            if (skinGenes.Count > 0)
            {
                using (new RandBlock(pawn.thingIDNumber))
                {
                    __result = skinGenes.RandomElement();
                }
            }
        }

        [HarmonyPatch(typeof(PawnHairColors), nameof(PawnHairColors.RandomHairColorGeneFor))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void RandomHairColorGeneFor_Postfix(ref GeneDef __result, Pawn pawn)
        {
            List<GeneDef> hairGenes = [];
            foreach (var skinGeneSet in ModExtHelper.GetAllPawnExtensions(pawn)
                .Where(x => x.randomHairGenes != null).Select(x => x.randomHairGenes))
            {
                hairGenes.AddRange(skinGeneSet);
            }
            if (hairGenes.Count > 0)
            {
                using (new RandBlock(pawn.thingIDNumber))
                {
                    __result = hairGenes.RandomElement();
                }
            }
        }
    }

    public class CompProperties_Race : CompProperties_ColorAndFur
    {
        public CompProperties_Race()
        {
            compClass = typeof(HediffComp_Race);
        }

        public static CompProperties_Race defaultMissingProps = new()
        {
            canSwapAwayFrom = false,  // We assume that races missing the comp will not be able to swap away from.
        };
        /// <summary>
        /// If TRUE this will let genes and hediffs change the pawn's race without the force command.
        /// If you want genes that change the body shape to work then this is advised.
        /// </summary>
        public bool canSwapAwayFrom = true;


        public void EnsureCorrectBodyType(Pawn pawn)
        {
            if (BodyTypeDef(pawn) != null && pawn.story.bodyType != BodyTypeDef(pawn))
            {
                pawn.story.bodyType = BodyTypeDef(pawn);
            }
        }
        public void EnsureCorrectHeadType(Pawn pawn)
        {
            if (HeadTypeDef(pawn) != null && pawn.story.headType != HeadTypeDef(pawn))
            {
                pawn.story.headType = HeadTypeDef(pawn);
            }
        }
    }

    //public class BaseRaceOverrides
    //{
    //    #region White, Black, and Allow-lists.
    //    public PawnDiet pawnDiet = null;
    //    #endregion

    //    #region Rendering
    //    public AdaptiveGraphicsCollection bodyPaths = [];
    //    public AdaptiveGraphicsCollection headPaths = [];
    //    public CustomMaterial bodyMaterial = null;
    //    public CustomMaterial headMaterial = null;
    //    #endregion
    //}
}
