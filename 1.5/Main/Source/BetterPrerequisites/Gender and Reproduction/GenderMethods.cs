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
                bool forceFemaleBody = cache.forceFemaleBody;

                bool femaleBody = pawn.gender != Gender.Male || forceFemaleBody;

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

        public static GeneticBodyType BodyTypeToGeneticBodyType(this BodyTypeDef bodyType)
        {
            if (bodyType == BodyTypeDefOf.Fat)
            {
                return GeneticBodyType.Fat;
            }
            else if (bodyType == BodyTypeDefOf.Hulk)
            {
                return GeneticBodyType.Hulk;
            }
            else if (bodyType == BodyTypeDefOf.Thin)
            {
                return GeneticBodyType.Thin;
            }
            else
            {
                return GeneticBodyType.Standard;
            }
        }

        public static void UpdateBodyHeadAndBeardPostGenderChange(Pawn pawn, bool banNarrow = false)
        {
            if (pawn?.story?.headType?.gender == null)
            {
                Log.Warning($"Tried to update body, head and beard for {pawn} but pawn?.story?.headType?.gender was null.");
                return;
            }
            bool headNeedsChange = pawn.story.headType.gender != 0 && pawn.story.headType.gender != pawn.gender;

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
            bool forceFemaleBody = HumanoidPawnScaler.GetCache(pawn) is BSCache cache && cache.forceFemaleBody;

            if (possibleHeads.Count > 0)
            {
                Gender targetGender = pawn.gender;
                if (forceFemaleBody)
                {
                    targetGender = Gender.Female;
                }
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


        public static float GetCompatibilityWith(this Pawn pawn, Pawn otherPawn, float defaultValue=0)
        {
            float ConstantPerPawnsPairCompatibilityOffset(int otherPawnID)
            {
                Rand.PushState();
                Rand.Seed = (pawn.thingIDNumber ^ otherPawnID) * 37;
                float result = Rand.GaussianAsymmetric(0.3f, 1f, 1.4f);
                Rand.PopState();
                return result;
            }
            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && HumanoidPawnScaler.GetCacheUltraSpeed(otherPawn) is BSCache cacheTwo)
            {
                if (pawn.def != otherPawn.def || pawn == otherPawn)
                {
                    return 0f;
                }
                
                float x = Mathf.Abs(cache.apparentAge - cacheTwo.apparentAge);
                float num = Mathf.Clamp(GenMath.LerpDouble(0f, 20f, 0.45f, -0.45f, x), -0.45f, 0.45f);
                float num2 = ConstantPerPawnsPairCompatibilityOffset(otherPawn.thingIDNumber);
                return num + num2;
            }
            return defaultValue;
        }

    }
}
