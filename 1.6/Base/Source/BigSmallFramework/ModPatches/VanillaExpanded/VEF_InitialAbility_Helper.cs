using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    // Fetches VEF.AnimalBehaviours.CompProperties_InitialAbility's "initialAbility"
    public static class VEF_InitialAbility_Helper
    {
        public static Type CompProperties_InitialAbilityType = null;
        private static FieldInfo initialAbilityField = null;

        public static List<AbilityDef> TryGetAbilities(List<CompProperties> compPropList)
        {
            if (VanillaExpanded.VEActive == false)
            {
                return null;
            }
            List<AbilityDef> results = [];
            foreach(var props in compPropList)
            {
                try
                {
                    if (CompProperties_InitialAbilityType == null)
                    {
                        CompProperties_InitialAbilityType = AccessTools.TypeByName("VEF.AnimalBehaviours.CompProperties_InitialAbility");
                        if (CompProperties_InitialAbilityType == null)
                        {
                            Log.Error("Big and Small: Could not find VEF.AnimalBehaviours.CompProperties_InitialAbility class.");
                            return null;
                        }
                    }
                    if (props == null || props.GetType() != CompProperties_InitialAbilityType)
                    {
                        continue;
                    }
                    if (initialAbilityField == null)
                    {
                        initialAbilityField = AccessTools.Field(CompProperties_InitialAbilityType, "initialAbility");
                        if (initialAbilityField == null)
                        {
                            Log.Error("Big and Small: Could not find initialAbility field in VEF.AnimalBehaviours.CompProperties_InitialAbility.");
                            continue;
                        }
                    }
                    AbilityDef abilityDef = (AbilityDef)initialAbilityField.GetValue(props);
                    results.Add(abilityDef);
                }
                catch (Exception e)
                {
                    Log.Error($"Big and Small: Exception in VEF_CompProps_InitialAbility_Wrapper.TryGetAbilityFromCompProp: {e}\n{e.StackTrace}");
                }
            }
            return results;
        }
    }
}
