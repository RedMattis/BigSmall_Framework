using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class GiantTraitPatches
    {
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

        public static bool CanEquipThing(bool __result, ThingDef thing, Pawn pawn, ref string cantReason)
        {
            if (__result == false || thing == null || pawn == null || pawn?.RaceProps?.Humanlike == false)
            {
                return __result;
            }
            Pawn_GeneTracker genes = pawn.genes;

            // Check if the thing is a weapon or equipment
            if (genes != null)
            {
                if (thing.IsWeapon)
                {
                    bool hasGenePreventingEquippingAnything = genes.GenesListForReading.Any(x => x.def.defName.Contains("BS_NoEquip"));
                    if (hasGenePreventingEquippingAnything)
                    {
                        cantReason = "BS_GenePreventsEquipping".Translate();
                        return false;
                    }
                }
                if (thing.IsApparel)
                {
                    if (!CanWearClothing(__result, thing, ref cantReason, pawn))
                    {
                        return false;
                    }
                    bool isGiant = pawn?.story?.traits?.allTraits?.Any(x => x.def.defName.ToLower().Contains("bs_giant")) == true || pawn.BodySize > 1.99;
                    if (thing.apparel.tags.Any(x => x.ToLower() == "giantonly") && !isGiant)
                    {
                        cantReason = "BS_PawnIsNotAGiant".Translate();
                        return false;
                    }
                }
            }

            List<WeaponClassDef> weaponClasses = thing.weaponClasses;
            if (weaponClasses == null || !weaponClasses.Any(x=>x.defName == BSDefs.BS_GiantWeapon.defName))
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

        public static bool CanWearClothing(bool __result, ThingDef thing, ref string cantreason, Pawn pawn)
        {
            if (thing.IsApparel && FastAcccess.GetCache(pawn) is BSCache cache)
            {
                // Skip nudity-permitted clothes unless it is headgear. E.g. permit Yttakin Bandoliers, but not power armor helmets.
                if (!thing.apparel?.countsAsClothingForNudity == true)
                {
                    if (thing?.thingCategories?.Any(x => x.defName == "Headgear") == false)
                    {
                        return __result;
                    }
                    return true;
                }
                if (!cache.canWearApparel)
                {
                    cantreason = "BS_CannotWearApparel".Translate();
                    return false;
                }
                bool itemIsArmor = thing.apparel.tags?.Any(x => x.ToLower().Contains("armor") || x.ToLower().Contains("armour")) == true ||
                        // or it thing categories has ApparelArmor.
                        thing.thingCategories?.Any(x => x.defName.ToLower().Contains("armor")) == true ||
                        // or trade tags
                        thing.tradeTags?.Any(x => x.ToLower().Contains("armor")) == true ||
                        thing.defName.ToLower().Contains("armor") ||
                        thing.defName.ToLower().Contains("helmet") ||
                        thing.defName.ToLower().Contains("armour") ||
                        // Or suspicious stuffing.
                        thing.recipeMaker?.recipeUsers?.Any(x => x.defName.ToLower().Contains("smithy")) == true ||
                        thing.stuffCategories?.Any(x => x.defName.ToLower().Contains("metallic")) == true;
                if (thing.defName == "Apparel_VisageMask" ||
                    thing.defName == "Apparel_Tailcap" ||
                    thing.defName == "Apparel_Sash" ||
                    thing.defName == "Apparel_Bandolier" ||
                    thing.defName == "Apparel_Crown" ||
                    thing.defName == "Apparel_CrownStellic"
                    )
                {
                    return true;
                    //itemIsArmor = false;
                }

                if (!cache.canWearArmor && itemIsArmor)
                {
                    __result = false;
                    cantreason = "BS_CannotWearArmor".Translate();
                }
                else if (!cache.canWearClothing && !itemIsArmor)
                {
                    __result = false;
                    cantreason = "BS_CannotWearClothing".Translate();
                }
            }
            return __result;
        }
    }

    [HarmonyPatch]
    public static class CanEquip
    {
        [HarmonyPatch(typeof(ApparelProperties), nameof(ApparelProperties.PawnCanWear), new Type[]
        {
            typeof(Pawn),
            typeof(bool)
        })]
        [HarmonyPostfix]
        public static void PawnCanWear(ApparelProperties __instance, ref bool __result, Pawn pawn)
        {
            if (pawn == null || pawn?.RaceProps?.Humanlike != true )
            {
                return;
            }
            if (!__instance.countsAsClothingForNudity)
            {
                return;
            }
            if (pawn?.needs != null)
            {
                var cache = HumanoidPawnScaler.GetBSDict(pawn);
                if (cache != null)
                {
                    
                    if (!cache.canWearApparel)
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
                    // Skip these two, since they can't check all the requipred tags...
                    //else if (!cache.canWearClothing && !__instance.tags.Contains("Armor"))
                    //{
                    //    __result = false;
                    //}
                    //else if (!cache.canWearArmor && __instance.tags.Contains("Armor"))
                    //{
                    //    __result = false;
                    //}
                }
            }
        }

        [HarmonyPatch(typeof(ApparelRequirement), nameof(ApparelRequirement.AllowedForPawn))]
        [HarmonyPostfix]
        public static void AllowedForPawn(ApparelRequirement __instance, ref bool __result, Pawn p, ThingDef apparel, bool ignoreGender)
        {
            if (__result == true)
            {
                string discard = "";
                // CanEquipThing
                __result = GiantTraitPatches.CanEquipThing(__result, apparel, p, ref discard);
            }
            
        }

        [HarmonyPatch(typeof(ApparelRequirement), nameof(ApparelRequirement.RequiredForPawn))]
        [HarmonyPostfix]
        public static void RequiredForPawn(ApparelRequirement __instance, ref bool __result, Pawn p, ThingDef apparel, bool ignoreGender)
        {
            if (__result == true)
            {
                string discard = "";
                // CanEquipThing
                __result = GiantTraitPatches.CanEquipThing(__result, apparel, p, ref discard);
            }
        }

        [HarmonyPatch(typeof(Apparel), nameof(Apparel.PawnCanWear))]
        [HarmonyPostfix]
        public static void RequiredForPawn(Apparel __instance, ref bool __result, Pawn pawn, bool ignoreGender)
        {
            if (__result == true)
            {
                string discard = "";
                // CanEquipThing
                __result = GiantTraitPatches.CanEquipThing(__result, __instance.def, pawn, ref discard);
            }
        }

    }
}
