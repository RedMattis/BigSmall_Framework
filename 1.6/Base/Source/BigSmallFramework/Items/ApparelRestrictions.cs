using BigAndSmall.FilteredLists;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class ApparelRestrictions// : IExposable
    {
        public bool absolutelyNothing = false;
        public bool noClothes = false;
        public bool noArmor = false;
        public bool exceptNudistFriendly = false;
        public FilterListSet<ThingDef> thingDefs = null;
        public FilterListSet<string> tags = null;
        /// <summary>
        /// OnSkin, Shell, Middle, etc.
        /// </summary>
        public FilterListSet<ApparelLayerDef> apparelLayers = null;
        /// <summary>
        /// Torso, Legs, LeftHand, etc.
        /// </summary>
        public FilterListSet<BodyPartGroupDef> bodyPartGroups = null;

        //public List<ThingDef> alwaysAcceptable = null;  // Priority over everything else.
        //public List<ThingDef> whitelist = null;
        //public List<ThingDef> blacklist = null;
        //public List<string> tagWhiteList = null;
        //public List<string> tagBlackList = null;
        //public List<string> tagAlwaysAcceptable = null;

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
            if (tags != null && apparel.tags is List<string> apparelTags)
            {
                result = tags.GetFilterResultFromItemList(apparelTags).Fuse(result);
                if (err == "" && result.Denied()) err = "BS_CannotWearTag".Translate();
            }

            if (NoApparel)
            {
                if (exceptNudistFriendly && apparel.countsAsClothingForNudity == false) return null;
                return "BS_CannotWearApparel".Translate();
            }

            //if (noFootgear && apparel.bodyPartGroups.Contains(BSDefs.Feet))
            //{
            //    return "BS_CannotWearFoot".Translate();
            //}
            //if (noOverHead && apparel.layers.Contains(ApparelLayerDefOf.Overhead)) return "BS_CannotWearOverHead".Translate();
            //if (noOuterLayer && apparel.layers.Contains(ApparelLayerDefOf.Shell)) return "BS_CannotWearOuter".Translate();
            //if (noMidLayer && apparel.layers.Contains(ApparelLayerDefOf.Middle)) return "BS_CannotWearMiddle".Translate();
            //if (noInnerLayer && apparel.layers.Contains(ApparelLayerDefOf.OnSkin)) return "BS_CannotWearInner".Translate();
            //if (noPants && apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs))
            //{
            //    return "BS_CannotWearPants".Translate();
            //}
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
            if (thingDefs != null)
            {
                result = thingDefs.GetFilterResult(thingDef).Fuse(result);
                if (result.Banned()) return "BS_CannotWearExact".Translate();
                if (result.ForceAllowed()) return null;
                
            }
            // Let the banlist ban other things first if for whatever reason it is.
            if (!thingDef.IsApparel) return null;

            string resStr = CanWear(thingDef.apparel, out FilterResult apRes);
            if (resStr != null) return resStr;
            if (apRes.ForceAllowed()) return null;
            result.Fuse(apRes);

            //if (hatsOkay && thingDef.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
            //{
            //    if (!IsArmor(thingDef)) return null;
            //}
            //if (hatsOkay && thingDef.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
            //{
            //    if (IsArmor(thingDef)) return "BS_CannotWearThis".Translate();
            //}
            
            //if (noPants && thingDef.IsApparel && thingDef.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs))
            //{
            //    return "BS_CannotWearPants".Translate();
            //}
            //if (noFootgear && thingDef.IsApparel && thingDef.apparel.bodyPartGroups.Contains(BSDefs.Feet))
            //{
            //    return "BS_CannotWearFoot".Translate();
            //}
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
                //hatsOkay = hatsOkay || other.hatsOkay,
                thingDefs = thingDefs.MergeFilters(other.thingDefs),
                tags = tags.MergeFilters(other.tags),
                apparelLayers = apparelLayers.MergeFilters(other.apparelLayers),
                bodyPartGroups = bodyPartGroups.MergeFilters(other.bodyPartGroups)
            };
            return result;
        }
    }
}
