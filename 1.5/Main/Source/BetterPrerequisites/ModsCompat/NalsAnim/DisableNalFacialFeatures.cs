using BetterPrerequisites;
using BigAndSmall;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class NalFaceExt
    {
        //public static Dictionary<Pawn, object> NalFacialLinks = new Dictionary<Pawn, object>();

        public static void DisableFacialAnimations(Pawn pawn, FacialAnimDisabler options, bool revert)
        {
            // This is disabled for now. Nal updated the mod and the structure is completely different.
            // So this needs to be reworked a ton.
            return;

            var facialAnimCC = pawn.AllComps.FirstOrDefault(x => x.GetType().Name.Contains("FacialAnimationControllerComp"));

            //var drawFaceGC = NalFacialLinks.TryGetValue(pawn, out object value) ? value : null;
            if (facialAnimCC == null)
            {
                return;
            }

            var animationFrameMapper = AccessTools.Field(facialAnimCC.GetType(), "animationFrameMapper").GetValue(facialAnimCC);
            var afm = animationFrameMapper;

            if (afm == null)
            {
                Log.Error("NalsAnim: Could not find animationFrameMapper for " + pawn.Name);
                return;
            }

            // This code needs to be changed to only run if the pawn has code which disables the facial features, for obvious reasons.

            // Get HeadControllerComp value
            var headController = AccessTools.Field(afm.GetType(), "headController").GetValue(afm);
            var skinController = AccessTools.Field(afm.GetType(), "skinController").GetValue(afm);
            var llidController = AccessTools.Field(afm.GetType(), "lidController").GetValue(afm);
            var lidOptionController = AccessTools.Field(afm.GetType(), "lidOptionController").GetValue(afm);
            var eyeballController = AccessTools.Field(afm.GetType(), "eyeballController").GetValue(afm);
            var mouthController = AccessTools.Field(afm.GetType(), "mouthController").GetValue(afm);
            var browController = AccessTools.Field(afm.GetType(), "browController").GetValue(afm);
            //var emotionController = AccessTools.Field(__instance.GetType(), "emotionController").GetValue(__instance);

            if (headController != null) ChangeControllers(pawn, revert, headController, options.headName);
            if (skinController != null) ChangeControllers(pawn, revert, skinController, options.skinName);
            if (llidController != null) ChangeControllers(pawn, revert, llidController, options.lidName);
            if (lidOptionController != null) ChangeControllers(pawn, revert, lidOptionController, options.lidOptionsName);
            if (eyeballController != null) ChangeControllers(pawn, revert, eyeballController, options.eyeballName);
            if (mouthController != null) ChangeControllers(pawn, revert, mouthController, options.mouthName);
            if (browController != null) ChangeControllers(pawn, revert, browController, options.browName);
        }


        private class FaceData
        {
            public Def faceType;
            public Gender gender;

            public FaceData(Def face)
            {
                faceType = face;

                // Get gender via reflection
                gender = (Gender)(faceType.GetType().GetField("gender").GetValue(faceType));
            }
        }
        private static void ChangeControllers(Pawn pawn, bool revert, object feComp, string targetName)
        {
            if (!revert && (targetName.ToLower() == "retain" || targetName.ToLower() == "")) return;
            // Get the pawn private field from the instance.

            var faceFieldInfo = AccessTools.Field(feComp.GetType(), "faceType");
            Def faceTypeValue = (Def)faceFieldInfo.GetValue(feComp);
            if (pawn != null)
            {
                // Don't revert if the faceTypeValue is not a "NOT_" faceType.
                if (revert && !faceTypeValue.defName.StartsWith("NOT_"))
                {
                    return;
                }

                // Get all the types from the `FacialAnimation` assembly.
                var faceType = faceTypeValue.GetType();
                var faceGenClass = AccessTools.TypeByName("FacialAnimation.FaceTypeGenerator`1").MakeGenericType(faceType);

                FieldInfo raceFaceTypeList = AccessTools.Field(faceGenClass, "raceFaceTypeList");
                var raceFaceTypeListV = raceFaceTypeList.GetValue(null);

                var raceFaceTypeListVAsDict = raceFaceTypeListV as System.Collections.IDictionary;
                List<Def> validVaceDefs = new List<Def>();

                // Add all human heads.
                var faceTypeList = raceFaceTypeListVAsDict["Human"] as IEnumerable<Def>;
                foreach (var def in faceTypeList.Where(x => x.GetType() == faceTypeValue.GetType()))
                {
                    validVaceDefs.Add(def);
                }

                // "Not" Face Features.
                var targetFacialFeatures = revert ? validVaceDefs.Where(x => !x.defName.StartsWith("NOT_")) : validVaceDefs.Where(x => x.defName.StartsWith(targetName));

                targetFacialFeatures = targetFacialFeatures
                    .Select(x=>new FaceData(x))
                    .Where(x=>x.gender == pawn.gender || x.gender == Gender.None)
                    .Select(x=>x.faceType);

                // human Faces are FaceTypeDefs. We need to get the DefName 
                if (targetFacialFeatures.Any())
                {
                    faceFieldInfo.SetValue(feComp, targetFacialFeatures.RandomElement());
                }
            }
        }
    }

    internal class FactialAnimHarmonyPatches
    {
        [HarmonyPatch]
        public static class FA_DisableFeatures
        {
            private static readonly string[] fa_methods = new string[]
            {
                "FacialAnimation.FacialAnimationModSettings:ShouldDrawRaceXenoType",
            };

            public static bool Prepare()
            {
                string[] vlfa_methods = fa_methods;
                for (int i = 0; i < vlfa_methods.Length; i++)
                {
                    if (!(AccessTools.Method(vlfa_methods[i]) == null)) return true;
                }
                return false;
            }

            public static IEnumerable<MethodBase> TargetMethods()
            {
                string[] vlfa_methods = fa_methods;
                for (int i = 0; i < vlfa_methods.Length; i++)
                {
                    MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i]);
                    if (!(methodInfo == null))
                        yield return methodInfo;
                }
            }
            //public static FieldInfo pawnField = null;
            public static bool pawnInitialized = true;

            public static bool Prefix(object __instance, Pawn pawn)
            {
                //FieldInfo pawnField = GetPawnField(__instance);
                //Pawn pawn = pawnField.GetValue(__instance) as Pawn;
                if (FastAcccess.GetCache(pawn) is BSCache cache && cache.facialAnimationDisabled)
                {
                    return false;
                }
                return true;
            }

            //private static FieldInfo GetPawnField(object __instance)
            //{
            //    if (pawnField == null)
            //    {
            //        pawnField = AccessTools.Field(__instance.GetType(), "pawn");
            //    }

            //    return pawnField;
            //}
        }
    }
}