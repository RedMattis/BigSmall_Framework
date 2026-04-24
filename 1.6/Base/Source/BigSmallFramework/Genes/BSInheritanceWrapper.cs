using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BigAndSmall
{
    public static class BSInheritanceWrapper
    {
        public static bool? ModActive { get; private set; }
        static bool initialized = false;
        static Traverse GetChildGenesMethod = null;
        static Traverse tryXenoByParents = null;
        public static void TrySetup()
        {
            if (initialized)
                return;
            initialized = true;

            ModActive = ModsConfig.IsActive("RedMattis.BetterGeneInheritance");
            if (ModActive == false)
                return;

            const string bgiExternal = "BGInheritance.External, BGInheritance";
            try
            {
                Type external = Type.GetType(bgiExternal) ?? throw new NullReferenceException($"\"{bgiExternal}\" could not be found");
                GetChildGenesMethod = Traverse
                    .Create(external)
                    .Method("GetChildGenes", [typeof(Pawn), typeof(Pawn)])
                    ?? throw new MissingMethodException($"Could not find GetChildGenes");

                tryXenoByParents = Traverse
                    .Create(external)
                    .Method("TrySetXenotypeBasedOnParents", [typeof(Pawn), typeof(List<Pawn>)])
                    ?? throw new MissingMethodException($"Could not find TrySetXenotypeBasedOnParents");
            }
            catch(Exception e)
            {
                Log.Error($"{nameof(BSInheritanceWrapper)} failed {e.Message}\n{e.StackTrace}");
            }
        }
        public static List<GeneDef> GetChildGenes(Pawn parentA, Pawn parentB) =>
            GetChildGenesMethod.GetValue<List<GeneDef>>(parentA, parentB);
  

        public static void TrySetXenotypeBasedOnParents(Pawn baby, List<Pawn> parents) =>
            tryXenoByParents.GetValue(baby, parents);
    }
}
