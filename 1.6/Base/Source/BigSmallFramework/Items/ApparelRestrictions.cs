using BigAndSmall.FilteredLists;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class ApparelRestrictions
    {
        public bool absolutelyNothing = false;
        public bool noClothes = false;
        public bool noArmor = false;
        public bool exceptNudistFriendly = false;
        public FilterListSet<string> tags = null;

        /// <summary>
        /// Obsoleted because it had issues with cases where ApparelProperties were used with no access to the thing.
        /// </summary>
        [Obsolete]
        public FilterListSet<ThingDef> thingDefs = null;
        
        /// <summary>
        /// OnSkin, Shell, Middle, etc.
        /// </summary>
        public FilterListSet<ApparelLayerDef> apparelLayers = null;
        /// <summary>
        /// Torso, Legs, LeftHand, etc.
        /// </summary>
        public FilterListSet<BodyPartGroupDef> bodyPartGroups = null;

        public bool NoApparel => (noClothes && noArmor) || absolutelyNothing;

        /// <summary>
        /// Returns the error if not, otherwise returns null.
        /// </summary>
        public string CanWear(ApparelProperties apparel, out FilterResult result)
        {
            result = FilterResult.Neutral;
            string err = "";
            if (apparelLayers != null)
            {
                result = apparelLayers.GetFilterResultFromItemList(apparel.layers).Fuse(result);
                if (err == "" && result.Denied()) err = "BS_CannotWearLayer".Translate();
            }
            if (bodyPartGroups != null)
            {
                result = bodyPartGroups.GetFilterResultFromItemList(apparel.bodyPartGroups).Fuse(result);
                if (err == "" && result.Denied()) err = "BS_CannotWearBodyPart".Translate();
            }

            if (exceptNudistFriendly && apparel.countsAsClothingForNudity == false) { result = FilterResult.ForceAllow; return null; }

            if (apparel.tags is List<string> apparelTags)
            {
                if (!apparel.HasRequireApparelTags(tags.ExplicitlyAcceptedItems))
                {
                    err = "BS_CannotWearTag".Translate();
                    return err;
                }
                var subResult = tags.GetFilterResultFromItemList(apparelTags);
                if (tags != null)
                {
                    result = tags.GetFilterResultFromItemList(apparelTags).Fuse(result);
                    if (err == "" && result.Denied()) err = "BS_CannotWearTag".Translate();
                }
            }

            if (NoApparel && !result.ForceAllowed())
            {
                if (exceptNudistFriendly && apparel.countsAsClothingForNudity == false) return null;
                return "BS_CannotWearApparel".Translate();
            }
            return result.Accepted() ? null : err;
        }

        public static void DebugTestAllWearable(Pawn testPawn)
        {
            if (HumanoidPawnScaler.GetCache(testPawn, forceRefresh: true) is BSCache cache)
            {
                DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsApparel).ToList().Do(x =>
                {
                    var result = cache.apparelRestrictions?.CanWear(x);
                    if (result != null)
                    {
                        Log.Message($"{testPawn.LabelCap} cannot wear {x.defName}: {result}");
                    }
                });
            }
            else
            {
                Log.Warning($"[BigAndSmall] {testPawn.def.defName} could not generate a cache..");
            }
        }

        /// <summary>
        /// Returns the error if not, otherwise returns null.
        /// </summary>
        public string CanWear(ThingDef thingDef)
        {
            
            FilterResult result = FilterResult.Neutral;

            if (!thingDef.HasRequiredWeaponClassTags(tags.ExplicitlyAcceptedItems))
            {
                return "BS_LacksRequiredClassTag".Translate();
            }
            if (!thingDef.HasRequiredWeaponTags(tags.ExplicitlyAcceptedItems))
            {
                return "BS_LacksRequiredTag".Translate();
            }

            if (!thingDef.IsApparel) return null;

            string resStr = CanWear(thingDef.apparel, out FilterResult apRes);
            if (resStr != null) return resStr;
            if (apRes.ForceAllowed()) return null;
            result.Fuse(apRes);
            bool isArmor = IsArmor(thingDef);
            if (noArmor && isArmor)
            {
                return "BS_CannotWearArmor".Translate();
            }
            if (noClothes && IsClothing(thingDef))
            {
                return "BS_CannotWearClothing".Translate();
            }
            return result.Accepted() ? null : "BS_CannotWearThis".Translate();
        }

        private bool IsArmor(ThingDef thing)
        {
            bool itemIsArmor = thing.apparel.tags?.Any
                (
                       x => x.ToLower().Contains("armor", StringComparison.OrdinalIgnoreCase)
                    || x.ToLower().Contains("armour", StringComparison.OrdinalIgnoreCase)
                ) == true
                // or it thing categories has ApparelArmor.
                || thing.thingCategories?.Any(x => x.defName.ToLower().Contains("armor", StringComparison.OrdinalIgnoreCase)) == true
                // or trade tags
                || thing.tradeTags?.Any(x => x.ToLower().Contains("armor", StringComparison.OrdinalIgnoreCase)) == true
                || thing.defName.ToLower().Contains("armor", StringComparison.OrdinalIgnoreCase)
                || thing.defName.ToLower().Contains("helmet", StringComparison.OrdinalIgnoreCase)
                || thing.defName.ToLower().Contains("armour", StringComparison.OrdinalIgnoreCase)
                || thing.recipeMaker?.recipeUsers?.Any(x => x.defName.ToLower().Contains("smithy")) == true
                // Or suspicious stuffing.
                || thing.stuffCategories?.Any(x => x.defName.ToLower().Contains("metallic")) == true;

            return itemIsArmor;
        }
        private bool IsClothing(ThingDef thing)
        {
            return !IsArmor(thing);
        }

        public ApparelRestrictions MakeFusionWith(ApparelRestrictions other)
        {
            if (other == null) return this;
            if (this == null) return other;
            if (this == null && other == null) return null;
            var result = new ApparelRestrictions
            {
                absolutelyNothing = absolutelyNothing || other.absolutelyNothing,
                noClothes = noClothes || other.noClothes,
                noArmor = noArmor || other.noArmor,
                exceptNudistFriendly = exceptNudistFriendly || other.exceptNudistFriendly,
                tags = tags.MergeFilters(other.tags),
                apparelLayers = apparelLayers.MergeFilters(other.apparelLayers),
                bodyPartGroups = bodyPartGroups.MergeFilters(other.bodyPartGroups)
            };
            return result;
        }
    }
}
