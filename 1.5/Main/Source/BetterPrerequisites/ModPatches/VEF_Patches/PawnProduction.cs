using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall.Balancing.VEPatches
{
    [HarmonyPatch]
    public static class VEF_Production
    {
        private static readonly string[] VLFA_Methods = new string[]
        {
            "AnimalBehaviours.HediffComp_Spawner:TryDoSpawn",
        };

        public static bool Prepare()
        {
            string[] vlfa_methods = VLFA_Methods;
            for (int i = 0; i < vlfa_methods.Length; i++)
            {
                if (!(AccessTools.Method(vlfa_methods[i]) == null)) return true;
            }
            return false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            string[] vlfa_methods = VLFA_Methods;
            for (int i = 0; i < vlfa_methods.Length; i++)
            {
                MethodInfo methodInfo = AccessTools.Method(vlfa_methods[i]);
                if (!(methodInfo == null))
                    yield return methodInfo;
            }
        }

        public static FieldInfo spawnCountField= null;
        //public static PropertyInfo thingToSpawn = null;
        public static int previousSpawnCount = 1;
        public static HediffCompProperties hediffCP_Spawner = null;

        public static void Prefix(ref HediffComp __instance)
        {
            var pawn = __instance.parent.pawn;

            if (pawn.Spawned)
            {

                hediffCP_Spawner = (HediffCompProperties)__instance.GetType().GetProperty("PropsSpawner").GetValue(__instance);

                spawnCountField = hediffCP_Spawner.GetType().GetField("spawnCount");


                if (spawnCountField != null)
                {
                    var sizeCache = HumanoidPawnScaler.GetBSDict(pawn);
                    if (sizeCache != null)
                    {

                        previousSpawnCount = (int)spawnCountField.GetValue(hediffCP_Spawner);
                        //var thing = (ThingDef)thingToSpawn.GetValue(hediffCP_Spawner);
                        float value = sizeCache.scaleMultiplier.DoubleMaxLinear;

                        int targetSpawnCount = Mathf.Max(1, (int)(value * previousSpawnCount));
                        spawnCountField.SetValue(hediffCP_Spawner, targetSpawnCount);
                    }
                }

            }
        }

        public static void Postfix(ref HediffComp __instance)
        {
            if (__instance.parent.pawn.Spawned && hediffCP_Spawner != null)
            {
                spawnCountField.SetValue(hediffCP_Spawner, previousSpawnCount);
            }
        }
     }
}
