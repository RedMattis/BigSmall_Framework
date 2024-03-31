using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(CompHasGatherableBodyResource), nameof(CompHasGatherableBodyResource.Gathered))]
    public static class CompHasGatherableBodyResourcePatches
    {
        static MethodInfo newResourceAmountMI = AccessTools.Method(typeof(CompHasGatherableBodyResourcePatches), nameof(GetModifiedProductionAmount));

        public static int GetModifiedProductionAmount(int resourceAmount, ThingWithComps thing)
        {
            if (thing != null && thing is Pawn pawn)
                return ProductionGene.ModifyProductionBasedOnSize(resourceAmount, pawn);

            else
            {
                Log.Warning($"GetModifiedProductionAmount could not modify production amount because {thing.Label} is not a Pawn. Returning original resource amount.");
                return resourceAmount;
            }
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Gathered_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var resourceAmountGetter = AccessTools.PropertyGetter(typeof(CompHasGatherableBodyResource), "ResourceAmount");

            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(resourceAmountGetter))
                {
                    // Load the instance unto the stack...
                    yield return new CodeInstruction(OpCodes.Ldarg_0);

                    // Load the Parent (ThingWithComps) unto the stack...
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent)));

                    // Call the new method, putting it at the top of the stack. 
                    yield return new CodeInstruction(OpCodes.Call, newResourceAmountMI);
                }
            }
        }
    }

    [HarmonyPatch]
    public static class BE_Production
    {
        private static readonly string[] BE_Properties = new string[]
        {
            "Gene_ExcessMilkProduction:CreateProduce",
            "Gene_RapidCoatGrowth:TickCreateProduce"
        };

        public static bool Prepare()
        {
            try
            {
                string[] beProps = BE_Properties;
                for (int i = 0; i < beProps.Length; i++)
                {
                    if (!(AccessTools.Method(beProps[i]) == null)) return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            string[] beProps = BE_Properties;
            for (int i = 0; i < beProps.Length; i++)
            {
                MethodInfo propGetter = AccessTools.Method(beProps[i]);

                if (!(propGetter == null))
                    yield return propGetter;
            }
        }

        public static int unmodifiedAmount = 15;
        public static FieldInfo amountField = null;
        public static void Prefix(ref Gene __instance)
        {
            if (__instance.pawn is Pawn pawn)
            {
                if (amountField == null)
                {
                    amountField = __instance.GetType().GetField("amount", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                unmodifiedAmount = (int)amountField.GetValue(__instance);
                var newAmount = ProductionGene.ModifyProductionBasedOnSize(unmodifiedAmount, pawn);
                amountField.SetValue(__instance, newAmount);
            }
        }

        public static void Postfix(ref Gene __instance)
        {
            amountField?.SetValue(__instance, unmodifiedAmount);
        }
    }
}
