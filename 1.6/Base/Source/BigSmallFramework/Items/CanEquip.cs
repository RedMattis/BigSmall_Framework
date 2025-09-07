using BigAndSmall.FilteredLists;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class CanEquipPatches
    {
        public static bool CanEquipThing(bool __result, ThingDef thing, Pawn pawn, ref string cantReason)
        {
            if (__result == false || thing == null || pawn?.RaceProps?.Humanlike != true)
            {
                return __result;
            }

            if (FastAcccess.GetCache(pawn) is BSCache cache)
            {
                // Check if the thing is a weapon or equipment
                if (thing.IsWeapon)
                {
                    if (!cache.canWield)
                    {
                        cantReason = "BS_GenePreventsEquipping".Translate();
                        return false;
                    }
                }

                if (cache.apparelRestrictions is ApparelRestrictions restrictions)
                {
                    var result = restrictions.CanWear(thing);
                    if (result != null)
                    {
                        cantReason = result;
                        return false;
                    }
                }
                // Handle cases where there is no apparel restriction setting.
                else
                {
                    if (!thing.HasRequiredWeaponClassTags([]))
                    {
                        cantReason = "BS_LacksRequiredClassTag".Translate();
                        return false;
                    }
                    if (!thing.HasRequiredWeaponTags([]))
                    {
                        cantReason = "BS_LacksRequiredTag".Translate();
                        return false;
                    }
                    if (thing.apparel != null && !thing.apparel.HasRequireApparelTags([]))
                    {
                        cantReason = "BS_LacksRequiredTag".Translate();
                        return false;
                    }
                }
            }
            
            return __result;
        }

        [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip), new Type[]
        {
        typeof(Thing),
        typeof(Pawn),
        typeof(string),
        typeof(bool)
        }, new ArgumentType[]
        {
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Out,
        ArgumentType.Normal
        })]
        [HarmonyPostfix]
        public static void CanEquip_Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
        {
            __result = CanEquipThing(__result, thing.def, pawn, ref cantReason);
        }

        [HarmonyPatch(typeof(ApparelProperties), nameof(ApparelProperties.PawnCanWear), new Type[]
        {
            typeof(Pawn),
            typeof(bool)
        })]
        [HarmonyPostfix]
        public static void PawnCanWear_Postfix(ApparelProperties __instance, ref bool __result, Pawn pawn)
        {
            if (pawn?.RaceProps?.Humanlike != true )
            {
                return;
            }
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache && cache.apparelRestrictions is ApparelRestrictions restrictions)
            {
                var result = cache.apparelRestrictions.CanWear(__instance, out FilterResult fr);
                if (result != null)
                {
                    __result = false;
                }
            }
            else if (ItemRestrictionDef.AllRestrictedTags.Any(__instance.tags.Contains))
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(ApparelRequirement), nameof(ApparelRequirement.AllowedForPawn))]
        [HarmonyPostfix]
        public static void AllowedForPawn_Postfix(ApparelRequirement __instance, ref bool __result, Pawn p, ThingDef apparel, bool ignoreGender)
        {
            if (__result == true)
            {
                string discard = "";
                __result = CanEquipThing(__result, apparel, p, ref discard);
            }
            
        }

        [HarmonyPatch(typeof(ApparelRequirement), nameof(ApparelRequirement.RequiredForPawn))]
        [HarmonyPostfix]
        public static void RequiredForPawn_Postfix(ApparelRequirement __instance, ref bool __result, Pawn p, ThingDef apparel, bool ignoreGender)
        {
            if (__result == true)
            {
                string discard = "";
                __result = CanEquipThing(__result, apparel, p, ref discard);
            }
        }

        [HarmonyPatch(typeof(Apparel), nameof(Apparel.PawnCanWear))]
        [HarmonyPostfix]
        public static void PawnCanWear_Postfix(Apparel __instance, ref bool __result, Pawn pawn, bool ignoreGender)
        {
            if (__result == true)
            {
                string discard = "";
                __result = CanEquipThing(__result, __instance.def, pawn, ref discard);
            }
        }

    }
}
