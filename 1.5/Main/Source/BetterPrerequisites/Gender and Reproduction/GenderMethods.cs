using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class GenderMethods
    {
        // The purpose of these two lists is to avoid doing repeated lookups.
        public static HashSet<string> failedPaths = [];
        public static HashSet<string> successPaths = [];
        public static void TrySetGenderBody(PawnRenderNode_Body __instance, Pawn pawn, ref Graphic __result)
        {
            string bodyNakedGraphicPath = pawn.story.bodyType.bodyNakedGraphicPath;

            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                if (cache.bodyGraphicPath != null)  // Skip if we have a custom body graphic. Those already support this stuff themselves.
                {
                    return;
                }
                bool femaleBody = cache.apparentGender == Gender.Female || pawn.gender == Gender.Female;

                if (femaleBody && bodyNakedGraphicPath != null && !bodyNakedGraphicPath.Contains("_Female") && (bodyNakedGraphicPath.Contains("_Thin") || bodyNakedGraphicPath.Contains("_Fat") || bodyNakedGraphicPath.Contains("_Hulk")))
                {
                    bodyNakedGraphicPath += "_Female";

                    if (failedPaths.Contains(bodyNakedGraphicPath))
                    {
                        return;
                    }

                    // Check so the path actually exists.
                    if (successPaths.Contains(bodyNakedGraphicPath) || ContentFinder<Texture2D>.Get(bodyNakedGraphicPath + "_south", reportFailure: false) != null)
                    {
                        Shader shader = __instance.ShaderFor(pawn);
                        __result = GraphicDatabase.Get<Graphic_Multi>(bodyNakedGraphicPath, shader, Vector2.one, __instance.ColorFor(pawn));
                        successPaths.Add(bodyNakedGraphicPath);
                    }
                    else
                    {
                        failedPaths.Add(bodyNakedGraphicPath);
                    }
                }
            }
        }

        //public static GeneticBodyType BodyTypeToGeneticBodyType(this BodyTypeDef bodyType)
        //{
        //    return bodyType.defName switch
        //    {
        //        "Fat" => GeneticBodyType.Fat,
        //        "Hulk" => GeneticBodyType.Hulk,
        //        "Thin" => GeneticBodyType.Thin,
        //        _ => GeneticBodyType.Standard,
        //    };
        //}

        public static bool IsBodyStandard(this BodyTypeDef bodyType)
        {
            return bodyType == BodyTypeDefOf.Female || bodyType == BodyTypeDefOf.Male;
        }

        public static void UpdateBodyHeadAndBeardPostGenderChange(Pawn pawn, bool banNarrow = false)
        {
            if (pawn?.story?.headType?.gender == null)
            {
                Log.Warning($"Tried to update body, head and beard for {pawn} but pawn?.story?.headType?.gender was null.\n" +
                    $"{pawn},{pawn?.story},{pawn?.story?.headType}, {pawn?.story?.headType?.gender}\n" +
                    $"Traceback {Environment.StackTrace}");
                return;
            }
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                UpdateBodyHeadAndBeardPostGenderChange(cache, banNarrow);
            }
        }

        public static void UpdateBodyHeadAndBeardPostGenderChange(BSCache cache, bool banNarrow = false)
        {
            Pawn pawn = cache.pawn;
            Gender apparentGender = cache.GetApparentGender();
            bool headNeedsChange = pawn.story.headType.gender != 0 && pawn.story.headType.gender != apparentGender;

            var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
            // Set body type.
            if (activeGenes.Any(x => x.def.bodyType != null))
            {
                var bodyType = activeGenes.First(x => x.def.bodyType != null).def.bodyType;
                if (bodyType != null)
                {
                    pawn.story.bodyType = bodyType.Value.ToBodyType(pawn);
                }
                else // Shouldn't happen, but just in case.
                {
                    pawn.story.bodyType = PawnGenerator.GetBodyTypeFor(pawn);//.BodyTypeToGeneticBodyType().ToBodyType(pawn);
                }
            }
            else
            {
                pawn.story.bodyType = PawnGenerator.GetBodyTypeFor(pawn);//.BodyTypeToGeneticBodyType().ToBodyType(pawn);
            }

            // If we have a head gene we don't want to use a randomchosen head.
            var headGenes = activeGenes.Where(x => !x.def.forcedHeadTypes.NullOrEmpty());
            var possibleHeads = headGenes.SelectMany(x => x.def.forcedHeadTypes).ToList();

            if (possibleHeads.Count > 0)
            {
                Gender targetGender = apparentGender;
                var validHeads = possibleHeads.Where(x => headGenes.All(ag => ag.def.forcedHeadTypes.Contains(x))).Where(x => x.gender == Gender.None || x.gender == targetGender).ToList();
                if (banNarrow)
                {
                    validHeads = validHeads.Where(x => !x.narrow).ToList();
                }
                if (validHeads.Count > 0)
                {
                    Rand.PushState(pawn.thingIDNumber);
                    pawn.story.headType = validHeads.RandomElement();
                    Rand.PopState();
                    headNeedsChange = false;
                }
                else
                {
                    Log.Warning($"Couldn't find an appropriate head fitting {pawn}'s genes.");
                }
            }
            if (headNeedsChange && !pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs.Where((HeadTypeDef x) => x.randomChosen)))
            {
                if (!pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs.Where((HeadTypeDef x) => x.randomChosen)))
                {
                    Log.Warning($"Couldn't find an appropriate head for {pawn}.");
                }

            }
            if (!pawn.style.CanWantBeard && pawn.style.beardDef != BeardDefOf.NoBeard)
            {
                pawn.style.beardDef = BeardDefOf.NoBeard;
            }
        }

        /// <summary>
        /// Some old crappy method to clean up broken pawns. Probably best to leave it alone for now...
        /// </summary>
        /// <param name="pawn"></param>
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
}
