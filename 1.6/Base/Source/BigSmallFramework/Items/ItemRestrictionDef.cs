using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class ItemRestrictionDef : Def
    {
        public List<string> restrictedTags = null;

        public static HashSet<string> AllRestrictedTags
        {
            get
            {
                if (field != null) return field;
                return field = [.. DefDatabase<ItemRestrictionDef>.AllDefsListForReading
                    .Where(x => x.restrictedTags != null)
                    .SelectMany(x => x.restrictedTags).Distinct()];
            }
        }

        public static List<string> RestrictedTags(IEnumerable<string> tags)
        {
            if (tags == null || !tags.Any()) return [];
            return [.. tags.Where(AllRestrictedTags.Contains)];
        }
    }

    public static class ItemRestrictionHelper
    {
        public static bool HasAllRequiredTags(List<string> requiredTags, List<string> pawnTags)
        {
            if (requiredTags == null || requiredTags.Count == 0) return true;
            if (pawnTags.NullOrEmpty()) return false;
            return requiredTags.All(pawnTags.Contains);
        }

        public static bool HasRequiredApparelTags(this ApparelProperties apparel, List<string> pawnTags)
        {
            if (apparel.tags is List<string> apparelTags
                && ItemRestrictionDef.RestrictedTags(apparelTags) is List<string> restrictedTags
                && !HasAllRequiredTags(restrictedTags, pawnTags))
            {
                return false;
            }
            return true;
        }

        public static bool HasRequiredWeaponTags(this ThingDef thingDef, List<string> pawnTags)
        {
            if (thingDef.weaponTags is List<string> weaponTags
                && ItemRestrictionDef.RestrictedTags(weaponTags) is List<string> restrictedTags
                && !HasAllRequiredTags(restrictedTags, pawnTags))
            {
                return false;
            }
            return true;
        }

        public static bool HasRequiredWeaponClassTags(this ThingDef thingDef, List<string> pawnTags)
        {
            if (thingDef.weaponClasses is List<WeaponClassDef> weaponClasses
                && ItemRestrictionDef.RestrictedTags(weaponClasses.Select(x => x.defName)) is List<string> restrictedClassTags
                && !HasAllRequiredTags(restrictedClassTags, pawnTags))
            {
                return false;
            }
            return true;
        }
    }
}
