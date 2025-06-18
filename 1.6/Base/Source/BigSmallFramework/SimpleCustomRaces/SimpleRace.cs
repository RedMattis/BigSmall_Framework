using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public static partial class RaceHelper
    {
        public static List<RaceTracker> GetRaceTrackers(this Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs.Where(h => h is RaceTracker).Cast<RaceTracker>().ToList();
        }
        public static List<RaceExtension> GetRaceExtensions(this ThingDef def)
        {
            return def.ExtensionsOnDef<RaceExtension, ThingDef>();
        }

        private static List<HediffComp_Race> GetRaceComps(this Pawn pawn)
        {
            return pawn.GetRaceTrackers()
                .Select(x => x.TryGetComp<HediffComp_Race>() ?? null)
                .Where(x=>x!=null).ToList();
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

        public static List<CompProperties_Race> GetRaceCompProps(this Pawn pawn)
        {
            var raceCompPropList = GetRaceComps(pawn).Where(x => x.Props is not null).Select(x => x.Props).ToList();
            if (raceCompPropList.Any()) { return raceCompPropList; }
            return [CompProperties_Race.defaultMissingProps];
        }

        public static bool IsMechanical(this Pawn pawn)
        {
            return pawn.GetAllPawnExtensions().Any(x => x.isMechanical) || pawn.RaceProps.IsMechanoid;
        }

        public static bool IsMechanicalDef(this ThingDef def)
        {
            if (FusedBody.FusedBodyByThing.TryGetValue(def, out var fusedBody) && fusedBody.isMechanical)
            {
                return true;
            }
            return def.GetRaceExtensions().SelectMany(x => x.PawnExtensionOnRace).Any(x => x.isMechanical);
        }
    }

    public class HediffComp_Race : HediffComp_ColorAndFur
    {
        public CompProperties_Race Props => (CompProperties_Race)props;
        public override void CompPostMake()
        {
            //Props.ValidateLists(parent.pawn);
            HumanoidPawnScaler.GetCache(parent.pawn, forceRefresh: true);
            GenderMethods.UpdateBodyHeadAndBeardPostGenderChange(Pawn, banNarrow: true, force:true);
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
            try
            {
                if (pawn == null || __result == null) { return; }
                List<GeneDef> skinGenes = [];
                foreach (var skinGeneSet in pawn.GetAllPawnExtensions()
                    .Where(x => x.randomSkinGenes != null).Select(x => x.randomSkinGenes))
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
            catch (System.Exception e)
            {
                if (!didWarn)
                {
                    Log.Error($"[BigAndSmall] Error in RandomSkinColorGene_Postfix for {pawn}: {e}");
                    didWarn = true;
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

    public static class CompProperties_Race_Extensions
    {
        public static void EnsureValidBodyType(this List<CompProperties_Race> comps, BSCache cache)
        {
            var pawn = cache.pawn;
            var gender = cache.GetApparentGender();
            List<BodyTypeDef> validBodyTypeDefs = comps.SelectMany(x => x.BodyTypeDefs(gender)).ToList();
            if (validBodyTypeDefs.Any() && validBodyTypeDefs.Contains(pawn.story?.bodyType) == false)
            {
                using (new RandBlock(pawn.GetPawnRNGSeed()))
                {
                    pawn.story.bodyType = validBodyTypeDefs.RandomElement();
                    //Log.Message($"Changed body type to {pawn.story.bodyType.defName}. Valid options were {string.Join(", ", validBodyTypeDefs.Select(x => x?.defName))}.");
                }
            }
        }

        public static void EnsureValidHeadType(this List<CompProperties_Race> comps, BSCache cache)
        {
            var pawn = cache.pawn;
            var gender = cache.GetApparentGender();
            List<HeadTypeDef> validHeadTypeDefs = comps.SelectMany(x => x.HeadTypeDefs(gender)).ToList();
            if (validHeadTypeDefs.Any() && validHeadTypeDefs.Contains(pawn.story?.headType) == false)
            {
                using (new RandBlock(pawn.GetPawnRNGSeed()))
                {
                    pawn.story.headType = validHeadTypeDefs.RandomElement();
                    //Log.Message($"Changed head type to {pawn.story.headType.defName}. Valid options were {string.Join(", ", validHeadTypeDefs.Select(x => x?.defName))}.");
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
