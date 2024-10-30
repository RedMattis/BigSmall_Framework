using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static UnityEngine.Scripting.GarbageCollector;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.LearnRateFactor))] //"LearnRateFactor"
    public class SkillRecord_Patch
    {
        public static void Postfix(ref float __result, SkillRecord __instance)
        {
            var sizeCache = HumanoidPawnScaler.GetCache(__instance.Pawn);
            if (sizeCache != null && sizeCache.minimumLearning > 0.351)
            {
                if (__instance.passion == Passion.None)
                {
                    // If we have a minimum skill learning speed of 0.35 and a override for 1 this will make the 
                    // final skill learning rate 1.0.
                    float value = sizeCache.minimumLearning / 0.35f;
                    __result *= value;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_AgeTracker), "GrowthPointsPerDayAtLearningLevel")] //"LearnRateFactor"
    public static class GrowthPointPerDayAtLearningLevel_Patch
    {
        public static void Postfix(ref float __result, Pawn ___pawn)
        {
            var sizeCache = HumanoidPawnScaler.GetCache(___pawn);
            if (HumanoidPawnScaler.GetCache(___pawn) is BSCache cache)
            {
                __result *= cache.growthPointGain;
            }
        }
    }

    [HarmonyPatch]
    public static class SkillAndAptitude
    {
        public static int GetExtAptitude(SkillRecord record, Pawn pawn)
        {
            int amount = 0;
            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache && cache.aptitudes != null)
            //if (HumanoidPawnScaler.GetCache(pawn, forceRefresh:true) is BSCache cache && cache.aptitudes != null)
            {
                cache.aptitudes.Where(x => x.skill == record.def).Do(x => amount = x.level);
            }
            return amount;
        }
        public static MethodBase TargetMethod()
        {
            return typeof(SkillRecord).GetProperty(nameof(SkillRecord.Aptitude), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetGetMethod();
        }

        // This stuff really doesn't need a Transpiler. I just made it as ane excercise.
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AptitudeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            bool found = false;

            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];

                if (!found && codes[i].opcode == OpCodes.Stfld && codes[i].operand is FieldInfo fieldInfo && fieldInfo.Name == "aptitudeCached" && fieldInfo.DeclaringType == typeof(SkillRecord))
                {
                    found = true;

                    // Add the class to the top of the stack for use with the field waaaay down.
                    yield return new(OpCodes.Ldarg_0);

                    // Load 
                    yield return new(OpCodes.Ldarg_0);

                    // Load Pawn
                    yield return new(OpCodes.Ldarg_0);
                    yield return new(OpCodes.Ldfld, typeof(SkillRecord).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance));

                    // Call GetAptitude with the loaded args.
                    yield return new(OpCodes.Call, typeof(SkillAndAptitude).GetMethod("GetExtAptitude", BindingFlags.Static | BindingFlags.Public, null, [typeof(SkillRecord), typeof(Pawn)], null));


                    // Get the cached value.
                    yield return new(OpCodes.Ldarg_0);
                    yield return new(OpCodes.Ldflda, typeof(SkillRecord).GetField("aptitudeCached", BindingFlags.NonPublic | BindingFlags.Instance));

                    // Call 0-parameter version
                    yield return new(OpCodes.Call, typeof(int?).GetMethod("GetValueOrDefault", Type.EmptyTypes));

                    // Add the two integers together
                    yield return new(OpCodes.Add);

                    //// Write the result into the Apt field.
                    
                    yield return new(OpCodes.Newobj, typeof(int?).GetConstructor([typeof(int)]));
                    yield return new(OpCodes.Stfld, typeof(SkillRecord).GetField("aptitudeCached", BindingFlags.NonPublic | BindingFlags.Instance));

                }
            }
        }
    }


}
