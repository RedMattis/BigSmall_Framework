using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;
using static BigAndSmall.RomanceTags;

namespace BigAndSmall
{
    public class RomanceTags
    {
        public class Compatibility
        {
            public float chance = 0;
            public float factor = 1;
            public bool exclude = false;
            public int priority = 0;
        }
        public Dictionary<string, Compatibility> compatibilities = [];
        public static HashSet<ThingDef> defaultUsers = [];
        public static RomanceTags simpleRaceDefault = new()
        {
            compatibilities = new()
            {
                ["Humanlike"] = new() { chance = 1.0f, factor = 0.75f },
                ["Human"] = new() { chance = 1.0f, factor = 1.0f }
            }
        };

        public readonly string TAG_IGNORE = "Exclude";
        public readonly string FACTOR = "Factor";
        public readonly string PRIORITY = "Priority";

        // Simply picks the highest chance from the list.
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode xmlNode in xmlRoot.ChildNodes)
            {
                var compat = new Compatibility();
                if (xmlNode.Attributes != null && xmlNode.Attributes[TAG_IGNORE] != null)
                {
                    compat.exclude = true;
                }
                if (xmlNode.Attributes != null && xmlNode.Attributes[FACTOR] != null)
                {
                    compat.factor = float.TryParse(xmlNode.InnerText, out float value) ? value : 1.0f;
                }
                else
                {
                    compat.chance = float.TryParse(xmlNode.InnerText, out float value) ? value : 1.0f;
                }

                compatibilities[xmlNode.Name] = compat;
            }
        }

        public List<string> GetDescriptions()
        {
            List<string> descStr = [];

            foreach (var tag in compatibilities)
            {
                string prioS = tag.Value.priority != 0 ? "BS_Priority".Translate(tag.Value.priority) : "";
                if (tag.Value.exclude) descStr.Add($"{tag.Key.Replace("_", " ")}: N/A");
                else if (tag.Value.factor != 1) descStr.Add($"{tag.Key.Replace("_", " ")}: {tag.Value.chance:f1} * {tag.Value.factor}");
                else descStr.Add($"{tag.Key.Replace("_", " ")}: {tag.Value.chance * 100:f0}%" + prioS);
            }
            return descStr;
        }
    }
    public static class RomanceTagsExtensions
    {
        public static float? GetHighestSharedTag(BSCache first, BSCache second)
        {
            Dictionary<string, Compatibility> compatibilities = [];
            void CheckTags(RomanceTags rOne, RomanceTags rTwo)
            {
                foreach (var tagOne in rOne.compatibilities.Where(x=>!x.Value.exclude))
                {
                    foreach (var tagTwo in rTwo.compatibilities.Where(x => x.Key == tagOne.Key && !x.Value.exclude))
                    {
                        compatibilities[tagOne.Key] = new Compatibility
                        {
                            chance = Math.Max(tagOne.Value.chance, tagTwo.Value.chance),
                            factor = tagOne.Value.factor * tagTwo.Value.factor
                        };
                    }
                }
            }
            if (first?.romanceTags == null || second?.romanceTags == null)
            {
                Log.Message($"Debug: One of the romance tags is null. First: {first?.romanceTags}, Second: {second?.romanceTags}");
                return null;
            }
            if (first == second)
            {
                Log.ErrorOnce($"Debug: Attempted to compare romance tags of the same BSCache instance. This should not happen.", 123456);
                return 0;
            }
            CheckTags(first.romanceTags, second.romanceTags);
            CheckTags(second.romanceTags, first.romanceTags);
            if (compatibilities.Count == 0)
            {
                return 0;
            }

            var bestOption = compatibilities.Where(x => !x.Value.exclude).OrderByDescending(x => x.Value.chance).FirstOrDefault();
            return bestOption.Value?.chance * bestOption.Value?.factor;
        }

        public static RomanceTags GetMerged(this IEnumerable<RomanceTags> romanceTags)
        {
            if (romanceTags == null || !romanceTags.Any()) return null;
            if (romanceTags.Count() == 1) return romanceTags.First();

            var merged = new RomanceTags { compatibilities = [] };

            // Group compatibilities by tag and combine their properties
            foreach (var group in romanceTags.SelectMany(rt => rt.compatibilities).GroupBy(x => x.Key))
            {
                string tag = group.Key;

                int highestPrio = group.Max(c => c.Value.priority);

                // Maximum chance across all compatibilities with this tag
                float maxChance = group.Where(x=>x.Value.priority == highestPrio).Max(c => c.Value.chance);

                // Product of all factors for this tag
                float totalFactor = group.Where(x => x.Value.priority == highestPrio).Aggregate(1.0f, (acc, c) => acc * c.Value.factor);

                // Combine into the merged result
                merged.compatibilities[tag] = new RomanceTags.Compatibility
                {
                    chance = maxChance,
                    factor = totalFactor,
                    exclude = group.Any(c => c.Value.exclude) // Exclude if any in the group are excluded
                };
            }
            merged.compatibilities.RemoveAll(x => x.Value.exclude);

            return merged;
        }

    }
}
