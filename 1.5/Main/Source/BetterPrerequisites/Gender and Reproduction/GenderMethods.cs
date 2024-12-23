using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class GenderBodyType
    {
        public BodyTypeDef bodyType = null;
        public bool isDefault = false;
        public HashSet<Gender> apparentGender = [];
        //private List<string> genderTags = [];
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            List<string> genderTags = [.. xmlRoot.InnerText.Split(',')];
            for (int idx = genderTags.Count - 1; idx >= 0; idx--)
            {
                string genderStr = genderTags[idx];
                if (Enum.TryParse(genderStr, out Gender gender))
                {
                    apparentGender.Add(gender);
                    genderTags.RemoveAt(idx);
                }
                else if (genderStr == "Default" || genderStr == "Any")
                {
                    isDefault = true;
                    genderTags.RemoveAt(idx);
                }
            }
            string bodyDefName = xmlRoot.Name;
            string mayRequireMod = xmlRoot.Attributes?["MayRequire"]?.Value;
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(bodyType), bodyDefName, mayRequireMod: mayRequireMod);
        }
    }
    public class BodyTypesPerGender : List<GenderBodyType>
    {
        public List<GenderBodyType> BodytypesForGender(Gender gender)
        {
            List<GenderBodyType> genderMatch = this.Where(x => x.apparentGender.Contains(gender)).ToList();
            if (genderMatch.Count == 0)
            {
                genderMatch = this.Where(x => x.isDefault).ToList();
            }
            return genderMatch;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode childNode in xmlRoot.ChildNodes)
            {
                var body = new GenderBodyType();
                body.LoadDataFromXmlCustom(childNode);
                Add(body);
            }
        }
    }

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
                bool femaleBody = cache.GetApparentGender() == Gender.Female || pawn.gender == Gender.Female;

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

        private static HashSet<BodyTypeDef> vanillaBodyTypesPlus = null;
        public static HashSet<BodyTypeDef> VanillaBodyTypesPlus
        {
            get
            {
                if (vanillaBodyTypesPlus != null) return vanillaBodyTypesPlus;
                HashSet<BodyTypeDef> baseGame = [BodyTypeDefOf.Male, BodyTypeDefOf.Female, BodyTypeDefOf.Thin, BodyTypeDefOf.Fat, BodyTypeDefOf.Hulk];
                if (ModsConfig.BiotechActive)
                {
                    baseGame.Add(BodyTypeDefOf.Baby);
                    baseGame.Add(BodyTypeDefOf.Child);
                }
                return vanillaBodyTypesPlus = baseGame;
            }
        }

        private static HashSet<BodyTypeDef> standardBodyTypes = null;
        public static HashSet<BodyTypeDef> StandardBodyTypes => standardBodyTypes ??= [BodyTypeDefOf.Male, BodyTypeDefOf.Female];

        public static bool IsBodyStandard(this BodyTypeDef bodyType)
        {
            return StandardBodyTypes.Contains(bodyType);
        }
        public static bool TryBodyGenderBodyUpdate(BodyTypeDef bodyType, Gender apparentGender, BSCache cache, out BodyTypeDef newBody)
        {

            var harLegalBodies = HARCompat.TryGetHarBodiesForThingdef(cache?.pawn?.def);
            newBody = null;
            if (IsBodyStandard(bodyType))
            {
                newBody = apparentGender switch
                {
                    Gender.Male => BodyTypeDefOf.Male,
                    Gender.Female => BodyTypeDefOf.Female,
                    _ => null
                };
                if (harLegalBodies != null && !harLegalBodies.Contains(newBody))
                {
                    return false;
                }
                return newBody != null && newBody != bodyType;
            }
            return false;
        }

        public static void UpdateBodyHeadAndBeardPostGenderChange(Pawn pawn, bool banNarrow = false, bool force = false)
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
                UpdateBodyHeadAndBeardPostGenderChange(cache, banNarrow, force:force);
            }
        }

        public static bool IsAdult(this Pawn pawn)
        {
            return pawn?.DevelopmentalStage == null || pawn.DevelopmentalStage > DevelopmentalStage.Child;
        }


        //private static HashSet<BodyTypeDef> vanillaBodytypes = [];
        public static void UpdateBodyHeadAndBeardPostGenderChange(BSCache cache, bool banNarrow = false, bool force = false)
        {
            Pawn pawn = cache.pawn;
            
            Gender apparentGender = cache.GetApparentGender();
            if (apparentGender == Gender.None)
            {
                return;
            }

            bool headNeedsChange = pawn.story.headType.gender != 0 && pawn.story.headType.gender != apparentGender;

            var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
            var activeGeneDefs = activeGenes.Select(x => x.def).ToList();
            // Set body type.
            bool updateBody = pawn?.story.bodyType == null ||
                pawn.story.bodyType.IsBodyStandard() ||
                cache.bodyTypeOverride != null ||
                apparentGender != pawn?.gender ||
                banNarrow ||
                force;

            // 
            var harLegalBodies = HARCompat.TryGetHarBodiesForThingdef(cache?.pawn?.def);

            if (updateBody && harLegalBodies == null)
            {
                bool adult = pawn.IsAdult();
                var currentBody = pawn.story?.bodyType;
                if (cache.bodyTypeOverride != null && adult)
                {
                    var bodyTypeOverride = cache.bodyTypeOverride.BodytypesForGender(apparentGender);
                    // A bit lazy, but if the body-type was added from this mod then it we can safely handle it.
                    vanillaBodyTypesPlus.AddRange(bodyTypeOverride.Select(x => x.bodyType));
                    if (bodyTypeOverride.Any())
                    {
                        using (new RandBlock(pawn.GetPawnRNGSeed()))
                        {
                            pawn.story.bodyType = bodyTypeOverride.RandomElement().bodyType;
                            goto Head;
                        }
                    }
                }
                if (currentBody != null && !VanillaBodyTypesPlus.Contains(currentBody))
                {
                    // Skip. We don't want to change the bodytype if it's is from an unkown source and not forced.
                }
                else if (activeGenes.Any(x => x.def.bodyType != null))
                {
                    var bodyType = activeGenes.First(x => x.def.bodyType != null).def.bodyType;
                    if (bodyType != null && adult)
                    {
                        pawn.story.bodyType = bodyType.Value.ToBodyType(pawn);
                    }
                    else if (bodyType == null) // Shouldn't happen, but just in case.
                    {
                        TrySetMissingBodytype(pawn, apparentGender);
                    }
                }
                else
                {
                    if (currentBody.IsBodyStandard())
                    {
                        if (apparentGender == Gender.Female)
                        {
                            pawn.story.bodyType = BodyTypeDefOf.Female;
                        }
                        else if (apparentGender == Gender.Male)
                        {
                            pawn.story.bodyType = BodyTypeDefOf.Male;
                        }
                    }
                    else if (currentBody == null)
                    {
                        if (pawn.story?.Adulthood != null)
                        {
                            pawn.story.bodyType = pawn.story.Adulthood.BodyTypeFor(apparentGender);
                        }
                        else if (pawn.story.bodyType == null)
                        {
                            TrySetMissingBodytype(pawn, apparentGender);
                        }
                    }
                }
            }
            if (pawn.story.bodyType == null)
            {
                pawn.story.bodyType = PawnGenerator.GetBodyTypeFor(pawn);
            }

        Head:
            if (headNeedsChange)
            {
                // If we have a head gene we don't want to use a randomchosen head.
                var headGenes = activeGenes.Where(x => !x.def.forcedHeadTypes.NullOrEmpty());
                var possibleHeads = headGenes.SelectMany(x => x.def.forcedHeadTypes).ToList();
                if (possibleHeads.Count > 0)
                {
                    Gender targetGender = apparentGender;
                    var validHeads = possibleHeads.Where(x => headGenes.All(ag => ag.def.forcedHeadTypes.Contains(x)))
                        .Where(x => x.gender == targetGender && (x.requiredGenes.NullOrEmpty() || x.requiredGenes.All(x=> activeGeneDefs.Contains(x)))).ToList();
                    if (validHeads.Count == 0)  // 
                    {
                        validHeads = possibleHeads.Where(x => headGenes.All(ag => ag.def.forcedHeadTypes.Contains(x))).Where(x => x.gender == targetGender).ToList();
                        if (validHeads.Count == 0)  // Try None heads only if there are no exact matches.
                        {
                            validHeads = possibleHeads.Where(x => headGenes.All(ag => ag.def.forcedHeadTypes.Contains(x))).Where(x => x.gender == Gender.None || x.gender == targetGender).ToList();
                        }
                    }
                    if (validHeads.Count == 0)
                    {
                        validHeads = possibleHeads.Where(x => headGenes.All(ag => ag.def.forcedHeadTypes.Contains(x))).ToList();
                        if (validHeads.Any())
                        {
                            Log.Warning($"Couldn't find an appropriate head fitting {pawn}'s genes. One was picked which ignored gender-limits.");
                        }
                    }
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

            static void TrySetMissingBodytype(Pawn pawn, Gender gender)
            {
                if (pawn.story?.Adulthood != null)
                {
                    pawn.story.bodyType = pawn.story.Adulthood.BodyTypeFor(gender);
                }
                pawn.story.bodyType ??= pawn.story.Adulthood.BodyTypeFor(pawn.gender);
                if (pawn.story.bodyType == null && ModsConfig.BiotechActive && pawn.DevelopmentalStage.Juvenile())
                {
                    if (pawn.DevelopmentalStage == DevelopmentalStage.Baby)
                    {
                        pawn.story.bodyType = BodyTypeDefOf.Baby;
                    }
                    else { pawn.story.bodyType = BodyTypeDefOf.Child; }
                }
                if (pawn.story.bodyType == null)
                {
                    PawnGenerator.GetBodyTypeFor(pawn);
                }
                //.BodyTypeToGeneticBodyType().ToBodyType(pawn);
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
