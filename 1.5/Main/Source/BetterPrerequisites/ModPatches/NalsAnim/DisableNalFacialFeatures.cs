using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                List<Def> validVaceDefs = [];

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

            public static bool Prefix(ref bool __result, object __instance, Pawn pawn)
            {
                //FieldInfo pawnField = GetPawnField(__instance);
                //Pawn pawn = pawnField.GetValue(__instance) as Pawn;
                if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn, canRegenerate:false) is BSCache cache && cache.facialAnimationDisabled)
                {
                    __result = false;
                    return false; // Skip original method
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


    //[StaticConstructorOnStartup]
    //public static class FA_Tweaks
    //{
    //    static FA_Tweaks()
    //    {
    //        Assembly facialAnimationAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "FacialAnimation");

    //        if (facialAnimationAssembly == null)
    //        {
    //            return;
    //        }

    //        var genericTypeDefinition = facialAnimationAssembly.GetTypes().Where(t => t.Name.StartsWith("ControllerBaseComp") && t.IsGenericTypeDefinition).ToList();

    //        if (genericTypeDefinition == null)
    //        {
    //            Log.Warning("Could not find generic type definition for ControllerBaseComp");
    //            return;
    //        }


    //        var derivedTypeList = GetDerivedTypes(genericTypeDefinition, facialAnimationAssembly);

    //        if (derivedTypeList.Count == 0)
    //        {
    //            Log.Warning("Could not find any derived types of ControllerBaseComp");
    //            return;
    //        }

    //        List<MethodInfo> methodList = new List<MethodInfo>();
    //        foreach (var derivedType in derivedTypeList)
    //        {
    //            Log.Message($"[DEBUG] Inspecting type: {derivedType.FullName}");
    //            var methods = derivedType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    //            foreach (var method in methods)
    //            {
    //                Log.Message($"[DEBUG] Found method: {method.Name}, Parameters: {string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}");
    //                if (method.Name == "LoadTextures")
    //                {
    //                    methodList.Add(method);
    //                }
    //            }
    //        }

    //        if (methodList.Count == 0)
    //        {
    //            Log.Warning("Could not find LoadTextures method in any derived types of ControllerBaseComp");
    //            return;
    //        }
    //        else
    //        {
    //            // Add patches
    //            foreach (var method in methodList)
    //            {
    //                BSCore.harmony.Patch(method, prefix: new HarmonyMethod(typeof(FA_DisableFeatures), nameof(FaceGenderPrefix)), postfix: new HarmonyMethod(typeof(FA_DisableFeatures), nameof(FaceGenderPostfix)));
    //            }
    //        }
    //    }


    //    public static Dictionary<Type, FieldInfo> pawnFieldDict = new Dictionary<Type, FieldInfo>();
    //    public static bool pawnInitialized = true;
    //    public static bool swapBackToMale = false;

    //    public static void FaceGenderPrefix(object __instance)
    //    {
    //        var pawn = GetPawnField(__instance).GetValue(__instance) as Pawn;
    //        bool androgynous = pawn.genes?.GenesListForReading?.Any(x => x.def == BSDefs.Body_Androgynous) == true;
    //        if (androgynous && pawn.gender == Gender.Male)
    //        {
    //            swapBackToMale = true;
    //            pawn.gender = Gender.Female;
    //        }
    //    }

    //    public static void FaceGenderPostfix(object __instance)
    //    {
    //        var pawn = GetPawnField(__instance).GetValue(__instance) as Pawn;
    //        if (swapBackToMale)
    //        {
    //            pawn.gender = Gender.Male;
    //        }
    //    }

    //    private static FieldInfo GetPawnField(object __instance)
    //    {
    //        var type = __instance.GetType();
    //        if (pawnFieldDict.TryGetValue(type, out FieldInfo pawnField))
    //        {
    //            return pawnField;
    //        }
    //        else
    //        {
    //            pawnFieldDict[type] = AccessTools.Field(__instance.GetType(), "pawn");
    //            if (pawnFieldDict[type] == null)
    //            {
    //                Log.Error($"Could not find pawn field in {type.FullName}");
    //            }
    //            return pawnFieldDict[type];
    //        }
    //    }

    //    /// <summary>
    //    /// Takes a list of generic types and then runs a while loop adding all derived types until the list contains no generic types.
    //    /// </summary>
    //    /// <param name="genericTypes"></param>
    //    /// <returns></returns>
    //    private static List<Type> GetDerivedTypes(List<Type> inputTypeList, Assembly targetAssembly)
    //    {
    //        Queue<Type> inputTypes = new Queue<Type>(inputTypeList);
    //        var nonGenericTypes = new List<Type>();
    //        var processedTypes = new HashSet<Type>(); // To track processed types and avoid duplicates

    //        while (true)
    //        {
    //            if (inputTypes.Count == 0)
    //            {
    //                return nonGenericTypes;
    //            }
    //            var currentType = inputTypes.Dequeue();

    //            if (currentType.IsGenericTypeDefinition)
    //            {
    //                var derivedTypes = new List<Type>();

    //                // Scan all types in the assembly
    //                foreach (var assemblyType in targetAssembly.GetTypes())
    //                {
    //                    // Make sure we're not comparing against ourself.
    //                    if (assemblyType == currentType)
    //                    {
    //                        continue;
    //                    }

    //                    if (assemblyType.IsGenericType && currentType == assemblyType.GetGenericTypeDefinition())
    //                    {
    //                        derivedTypes.Add(assemblyType);
    //                    }
    //                    else if (IsDerivedFromGenericTypeDefinition(currentType, assemblyType))
    //                    {
    //                        derivedTypes.Add(assemblyType);
    //                    }
    //                }

    //                // Add non-generic types to the result list
    //                foreach(var derivedType in derivedTypes)
    //        {
    //                    if (!processedTypes.Contains(derivedType))
    //                    {
    //                        processedTypes.Add(derivedType);
    //                        if (derivedType.IsGenericTypeDefinition)
    //                        {
    //                            inputTypes.Enqueue(derivedType);
    //                        }
    //                        else
    //                        {
    //                            nonGenericTypes.Add(derivedType);
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                nonGenericTypes.Add(currentType);
    //            }
    //        }
    //    }

    //    private static bool IsDerivedFromGenericTypeDefinition(Type genericTypeDefinition, Type type)
    //    {
    //        if (type == null || type == typeof(object))
    //        {
    //            return false;
    //        }

    //        var baseType = type.BaseType;
    //        while (baseType != null)
    //        {
    //            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericTypeDefinition)
    //            {
    //                return true;
    //            }

    //            baseType = baseType.BaseType;
    //        }

    //        return false;
    //    }

    //}

}