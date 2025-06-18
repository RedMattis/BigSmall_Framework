
using Verse;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace BigAndSmall
{
    [StaticConstructorOnStartup]
    internal class BigAndSmall_Early : Mod
    {
        public static BigAndSmall_Early instance = null;
       // public static BSXenoSettings settings;

        public BigAndSmall_Early(ModContentPack content) : base(content)
        {
            instance = this;
            BigSmallMod.settings ??= GetSettings<BSSettings>();
            //settings = GetSettings<BSXenoSettings>();

            ApplyHarmonyPatches();
        }

        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.BigAndSmall_Early");
            harmony.PatchAll();
        }
    }

    public class BSXenoSettings: ModSettings
    {
        
    }

    [HarmonyPatch]
    public static class ReloadPatches
    {
        
        [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
        [HarmonyPrefix]
        public static void GenerateImpliedDefs_Prefix(bool hotReload)
        {
            BSCore.RunBeforeGenerateImpliedDefs(hotReload: hotReload);
        }
        
        
        [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> InsertBeforeResolveAllWantedCrossReferences(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var methodToCall = AccessTools.Method(typeof(BSCore), nameof(BSCore.RunDuringGenerateImpliedDefs)); 

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo methodInfo &&
                    methodInfo.Name == "ResolveAllWantedCrossReferences")
                {
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0)); 
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, methodToCall));
                    break;
                }
            }

            return codes.AsEnumerable(); // Return the modified instruction list.
        }
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
        //public static void LoadAllActiveModsPostfix(bool hotReload)
        //{
        //    BSCore.RunDefPatchesWithHotReload(hotReload: hotReload);

        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
        //public static void LoadAllActiveModsPrefix(bool hotReload)
        //{
        //    if (hotReload)
        //    {
        //        RaceFuser.PreHotreload();
        //    }
        //}
    }


    //[HarmonyPatch]
    //public static class LoadReloadPatches
    //{
    //    [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    //    [HarmonyPrefix]
    //    public static void GenerateImpliedDefs_PreResolve(bool hotReload)
    //    {
    //        BSCore.RunDefPatchesWithHotReload(hotReload: hotReload);
    //    }
    //}

}
