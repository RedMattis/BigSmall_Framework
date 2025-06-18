using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class PawnExtensionExtension
    {
        private class TooltipSection
        {
            public string Header { get; }
            public List<string> Entries { get; }
            public TooltipSection(string header, IEnumerable<string> entries = null)
            {
                Header = header;
                Entries = entries?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? [];
            }

            public override string ToString()
            {
                if (Entries.Count == 0) return string.Empty;

                var sb = new StringBuilder();
                bool isOneLiner = Entries.First() == "SKIP";

                if (!string.IsNullOrWhiteSpace(Header))
                {
                    if (isOneLiner)
                    {
                        sb.AppendLine(Header.Colorize(ColoredText.TipSectionTitleColor).CapitalizeFirst());
                    }
                    else
                    {
                        sb.AppendLine(Header.Colorize(ColoredText.TipSectionTitleColor).CapitalizeFirst() + ":");
                    }
                }
                if (!isOneLiner)
                {

                    foreach (var entry in Entries)
                    {
                        if (entry.StartsWith("  - "))
                        {
                            sb.AppendLine(entry);
                        }
                        else
                        {
                            sb.AppendLine($"  - {entry}");
                        }
                    }
                }
                return sb.ToString();
            }
        }

        public static bool TryGetDescription(this List<PawnExtension> extList, out string content)
        {
            StringBuilder sb = new();
            List<TooltipSection> sections =
            [
                // General/Important Attributes
                CreateListSection("BS_Aptitudes".Translate(), extList, ext => ext.AptitudeDescription),
                CreateListSection("BS_ConditionalDescription".Translate(), extList, ext => ext.ConditionalDescription),
                CreateListSection("BS_SizeByAgeOffset".Translate(), extList, ext => ext.SizeByAgeDescription),
                CreateListSection("BS_SizeByAgeOffset".Translate(), extList, ext => ext.SizeByAgeMultDescription),
                CreateListSection("BS_StatChangesDescriptions".Translate(), extList, ext => ext.StatChangeDescriptions),
                // Health and Body Modifications
                CreateListSection("BS_RacialFeatures".Translate(), extList, ext => ext.RacialFeaturesDescription),
                CreateListSection("BS_Applies".Translate(), extList, ext => ext.ApplyBodyHediffDescription),
                CreateListSection("BS_Applies".Translate(), extList, ext => ext.RaceForcedHediffsDesc),
                CreateListSection("BS_Applies".Translate(), extList, ext => ext.ApplyPartHediffDescription),
                CreateListSection("BS_ThingDefSwap".Translate(), extList, ext => ext.ThingDefSwapDescription),
                CreateIndividualSection("BS_ForceUnarmed".Translate(), extList, ext => ext.ForceUnarmedDescription),
                CreateIndividualSection("BS_PreventDisfigurement".Translate(), extList, ext => ext.PreventDisfigurementDescription),
                CreateIndividualSection("BS_CanWalkOnCreep".Translate(), extList, ext => ext.CanWalkOnCreepDescription),
                
                // Traits and Genetics
                CreateIndividualSection("ForcedTraits".Translate(), extList, ext => ext.forcedTraits ?? Enumerable.Empty<object>()),
                CreateIndividualSection("BS_FocedEndoImmutable".Translate(), extList, ext => ext.immutableEndogenes?.Select(e => e.LabelCap)),
                CreateIndividualSection("BS_FocedEndo".Translate(), extList, ext => ext.forcedEndogenes?.Select(e => e.LabelCap)),
                CreateIndividualSection("BS_FocedXeno".Translate(), extList, ext => ext.forcedXenogenes?.Select(e => e.LabelCap)),
                // Needs and Diet
                CreateIndividualSection("BS_PawnDiet".Translate(), extList, ext => ext.PawnDietDescription),
                CreateIndividualSection("BS_LockedNeeds".Translate(), extList, ext => ext.LockedNeedsDescription),
                CreateAggregatedSection("BS_BleedRateDesc".Translate(), extList.Where(x=>x.bleedRate != null).ToList(), ext => ext.bleedRate == null ? 1 : ext.bleedRate, rates => rates.Aggregate(1f, (acc, rate) => acc * rate.Value).ToStringPercent()),
                CreateIndividualSection("BS_ConsumeSoulOnHit".Translate(), extList, ext => ext.ConsumeSoulOnHitDescription),
                // Apparel and Restrictions
                CreateIndividualSection("BS_HasApparelRestrictions".Translate(), extList, ext => ext.apparelRestrictions != null ? "BS_Modified".Translate().CapitalizeFirst() : null),
                CreateIndividualSection("BS_CanWieldThings".Translate(), extList, ext => ext.canWieldThings != null ? "BS_Modified".Translate().CapitalizeFirst() : null),
                // Thoughts and Relationships
                CreateAggregatedSection("BS_HasNullThoughtsCount", [.. extList.Where(x=>x.nullsThoughts != null)], ext => ext.nullsThoughts?.Count ?? 0, counts => counts.Sum().ToString()),
                CreateListSection("BS_RomanceTags".Translate(), extList, ext => ext.RomanceTagsDescription),
                CreateIndividualSection("BS_CreatureTag".Translate(), extList, ext => ext.TagDescriptions)
            ];

            // Process all sections and append to the StringBuilder
            foreach (var section in sections.Where(x => x != null))
            {
                sb.Append(section.ToString());
            }

            content = RemoveDuplicateLines(sb).ToString();

            bool hasContent = !string.IsNullOrWhiteSpace(content);
            // Remove duplicate lines and return the final description
            return hasContent;

            // --- Helper Methods ---
            TooltipSection CreateAggregatedSection<T>(
                string untranslatedString,
                List<PawnExtension> list,
                Func<PawnExtension, T> selector,
                Func<IEnumerable<T>, string> aggregateFormatter)
            {
                var selectedData = list
                    .Select(selector)
                    .Where(data => !NoData(data));

                if (!selectedData.Any())
                    return null;

                // Use the aggregateFormatter to process the aggregation
                string aggregatedText = aggregateFormatter(selectedData);

                return new TooltipSection(untranslatedString.Translate($"  - {aggregatedText}"), ["SKIP"]);
            }

            TooltipSection CreateIndividualSection(string header, List<PawnExtension> list, Func<PawnExtension, object> selector)
            {
                var entries = list
                    .Select(selector)
                    .Where(entry => !NoData(entry))
                    .Select(FormatIndividualEntry)
                    .ToList();

                return new TooltipSection(header, entries);
            }

            static string FormatIndividualEntry(object entry)
            {
                return entry switch
                {
                    Def def => def.LabelCap,
                    string str => str.CapitalizeFirst(),
                    TaggedString taggedStr => taggedStr.CapitalizeFirst(),
                    IEnumerable<Def> defList => string.Join(", ", defList.Select(d => d.LabelCap)),
                    IEnumerable<string> strList => string.Join(", ", strList.Select(s => s.CapitalizeFirst())),
                    IEnumerable<TaggedString> taggedStrList => string.Join(", ", taggedStrList.Select(ts => ts.CapitalizeFirst())),
                    _ => entry.ToString()
                };
            }

            TooltipSection CreateListSection(string header, List<PawnExtension> list, Func<PawnExtension, object> selector)
            {
                var entries = list
                    .Select(selector)
                    .Where(entry => !NoData(entry))
                    .Select(FormatListSections)
                    .ToList();

                return new TooltipSection(header, entries);
            }

            // Makes newlines with " -  " and capitalizes the items
            static string FormatListSections(object entry)
            {
                return entry switch
                {
                    IEnumerable<string> strList => strList.ToLineList("  - ", capitalizeItems: true),
                    IEnumerable<TaggedString> taggedStrList => taggedStrList.Select(ts => ts.ToString()).ToLineList("  - ", capitalizeItems: true),
                    IEnumerable<object> objList => objList.Select(o => o.ToString()).ToLineList("  - ", capitalizeItems: true),
                    _ => entry.ToString()
                };
            }

            static bool NoData(object entry)
            {
                return entry == null ||
                       entry is bool ||
                       (entry is string str && string.IsNullOrWhiteSpace(str)) ||
                       (entry is TaggedString tStr && string.IsNullOrWhiteSpace(tStr)) ||
                       (entry is IEnumerable<object> enumerable && !enumerable.Cast<object>().Any());
            }

            static StringBuilder RemoveDuplicateLines(StringBuilder sb)
            {
                var uniqueLines = new HashSet<string>(sb.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
                sb.Clear();
                foreach (var line in uniqueLines)
                {
                    sb.AppendLine(line);
                }
                return sb;
            }
        }
    }
}
