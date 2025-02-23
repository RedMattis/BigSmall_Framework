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
            if (__result == false || thing == null || pawn == null || pawn?.RaceProps?.Humanlike == false)
            {
                return __result;
            }
            Pawn_GeneTracker genes = pawn.genes;
            if (genes == null)
            {
                return __result;
            }

            if (FastAcccess.GetCache(pawn) is BSCache cache)
            {
                if (cache.apparelRestrictions is ApparelRestrictions restrictions)
                {
                    var result = restrictions.CanWear(thing);
                    if (result != null)
                    {
                        cantReason = result;
                        return false;
                    }
                }


                // Check if the thing is a weapon or equipment
                if (thing.IsWeapon)
                {
                    if (!cache.canWield)
                    {
                        cantReason = "BS_GenePreventsEquipping".Translate();
                        return false;
                    }

                    bool hasGenePreventingEquippingAnything = genes.GenesListForReading.Any(x => x.def.defName.Contains("BS_NoEquip"));
                    if (hasGenePreventingEquippingAnything)
                    {
                        cantReason = "BS_GenePreventsEquipping".Translate();
                        return false;
                    }
                }
            }

            
            if (thing.IsApparel)
            {
                bool isGiant = pawn?.story?.traits?.allTraits?.Any(x => x.def.defName.ToLower().Contains("bs_giant")) == true || pawn.BodySize > 1.99;
                if (thing.apparel.tags.Any(x => x.ToLower() == "giantonly") && !isGiant)
                {
                    cantReason = "BS_PawnIsNotAGiant".Translate();
                    return false;
                }
            }

            List<WeaponClassDef> weaponClasses = thing.weaponClasses;
            if (weaponClasses == null || !weaponClasses.Any(x => x.defName == BSDefs.BS_GiantWeapon.defName))
            {
                return __result;
            }
            else if (pawn.genes != null)
            {
                bool hasValidGene = genes.GenesListForReading.Any(x => x.def.defName.ToLower().Contains("herculean"));

                // Get all traits on pawn
                bool hasValidTrait = pawn.story.traits.allTraits.Any(x =>
                    x.def.defName.ToLower().Contains("bs_giant") ||
                    x.def.defName.ToLower().Contains("warcasket"));

                if (genes != null && hasValidGene || hasValidTrait)
                {
                    return true;
                }

                // Note that this won't help you wield e.g. warcasket weapons since those work based on a tag.
                if (pawn?.BodySize >= 1.999f)
                {
                    return true;
                }
                else
                {
                    cantReason = "BS_PawnIsNotAGiant".Translate();
                    return false;
                }
            }
            else
            {
                return __result;
            }
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
                else
                {
                    bool isGiant = pawn.story?.traits?.HasTrait(BSDefs.BS_Giant) == true || pawn.BodySize > 1.99;
                    if (__instance.tags.Any(x => x.ToLower() == "giantonly") && !isGiant)
                    {
                        __result = false;
                    }
                }
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
