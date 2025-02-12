using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    namespace FilteredLists
    {
        public enum FilterResult // Needs to be outside of the FilterList class, otherwise we'll get one for each <T>.
        {
            None,       // No result yet. Can be used to determine if there was a filter hit.
            Neutral,     // Accepts the result, but is a fail-state if explicit permission is required.
            Allow,      // Accepts the result, priority over Neutral.
            Deny,       // Denies the result, but can be overridden by ForceAllow.
            ForceAllow, // Accepts the result. Priority over anything but ForceDeny.
            Banned,  // If this is present, the result is always ForceDeny.
        }
        public enum FType
        {
            Acceptlist,
            Whitelist,
            Blacklist,
            Allowlist,
            Banlist
        }

        public abstract class FilterList<T> : List<T>
        {
            public abstract FType FilterType { get; }

            public FilterList(IEnumerable<T> collection) : base(collection) { }

            public FilterList() : base() { }

            public override string ToString()
            {
                return $"{base.ToString()}_{FilterType}_count:{Count}";
            }

            private bool Match(object a, object b)
            {
                if (a == b) return true ;
                if (a is Def && b is Def)
                {
                    return a == b;
                }
                if (a is string aStr && b is string bStr)
                {
                    return aStr.ToLower() == bStr.ToLower();
                }
                string aAsStr = (a is Def aDef) ? aDef.defName : a.ToString();
                string bAsStr = (b is Def bDef) ? bDef.defName : b.ToString();
                return string.Equals(aAsStr, bAsStr, StringComparison.OrdinalIgnoreCase);
            }
            public FilterResult GetFilterResult(T item)
            {
                return FilterType switch
                {
                    FType.Allowlist => this.Any(t => Match(item, t)) ? FilterResult.ForceAllow : FilterResult.Neutral,
                    FType.Whitelist => this.Any(t => Match(item, t)) ? FilterResult.Allow : FilterResult.Deny,
                    FType.Acceptlist => this.Any(t => Match(item, t)) ? FilterResult.Allow : FilterResult.Neutral,
                    FType.Blacklist => this.Any(t => Match(item, t)) ? FilterResult.Deny : FilterResult.Neutral,
                    FType.Banlist => this.Any(t => Match(item, t)) ? FilterResult.Banned : FilterResult.Neutral,
                    _ => throw(new NotImplementedException($"No filter behaviour for type {FilterType}"))
                };
            }

            public bool AnyMatch(T item) => this.Any(t => Match(item, t));

            public void LoadDataFromXmlCustom(XmlNode xmlRoot)
            {
                foreach (XmlNode cNode in xmlRoot.ChildNodes)
                {
                    if (typeof(T) == typeof(FlagString))
                    {
                        var fs = new FlagString();
                        fs.LoadDataFromXmlCustom(cNode);
                        Add((T)(object)fs);
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        Add((T)(object)cNode.FirstChild.Value);
                    }
                    else
                    {
                        string defName = cNode.FirstChild.Value;
                        string mayRequireMod = cNode.Attributes?["MayRequire"]?.Value;
                        DirectXmlCrossRefLoader.RegisterListWantsCrossRef(this, defName, mayRequireMod: mayRequireMod);
                    }
                }
            }
        }
        public class Allowlist<T> : FilterList<T>
        {
            public override FType FilterType => FType.Allowlist;
            public Allowlist(IEnumerable<T> collection) : base(collection) { }
            public Allowlist() : base() { }
        }
        public class Whitelist<T> : FilterList<T>
        {
            public override FType FilterType => FType.Whitelist;
            public Whitelist(IEnumerable<T> collection) : base(collection) { }
            public Whitelist() : base() { }
        }
        public class AcceptList<T> : FilterList<T>
        {
            public override FType FilterType => FType.Acceptlist;
            public AcceptList(IEnumerable<T> collection) : base(collection) { }
            public AcceptList() : base() { }
        }
        public class Blacklist<T> : FilterList<T>
        {
            public override FType FilterType => FType.Blacklist;
            public Blacklist(IEnumerable<T> collection) : base(collection) { }
            public Blacklist() : base() { }
        }
        public class Banlist<T> : FilterList<T>
        {
            public override FType FilterType => FType.Banlist;
            public Banlist(IEnumerable<T> collection) : base(collection) { }
            public Banlist() : base() { }
        }

        public static class FilterHelpers
        {
            public static FilterResult Max(this FilterResult a, FilterResult b) => (a > b) ? a : b;
 
            public static FilterResult MaxList(this IEnumerable<FilterResult> results) =>
                results.Aggregate(FilterResult.None, (a, b) => a.Max(b));

            public static FilterResult Fuse(this FilterResult previous, FilterResult next) => previous.Max(next);

            public static FilterResult Fuse(this IEnumerable<FilterResult> results) => results.EnumerableNullOrEmpty() ? FilterResult.None : results.MaxList();

            public static FilterResult Fuse(this FilterResult previous, IEnumerable<FilterResult> next) =>
                next.Fuse().Fuse(previous);


            // From single item.
            public static FilterResult GetFilterResult<T>(this IEnumerable<FilterList<T>> filterList, T item)
            {
                return filterList.Select(x => x.GetFilterResult(item)).Fuse();
            }

            public static IEnumerable<FilterResult> GetFilterResults<T>(this IEnumerable<FilterList<T>> filterList, T item)
            {
                return filterList.Select(x => x.GetFilterResult(item));
            }

            // From List of items. A bit slow, don't use in performance critical places.
            public static FilterResult GetFilterResultFromItemList<T>(this IEnumerable<FilterList<T>> filterList, List<T> itemList)
            {
                return filterList.SelectMany(x=> itemList.Select(y => x.GetFilterResult(y))).Fuse(); 
            }


            public static bool Banned(this FilterResult fResult) => fResult == FilterResult.Banned;
            public static bool Denied(this FilterResult fResult) => fResult == FilterResult.Deny || fResult == FilterResult.Banned;
            public static bool NeutralOrWorse(this FilterResult fResult) => Denied(fResult) || fResult == FilterResult.Neutral || fResult == FilterResult.None;
            public static bool Accepted(this FilterResult fResult) => !Denied(fResult);
            public static bool ExplicitlyAllowed(this FilterResult fResult) => fResult == FilterResult.ForceAllow || fResult == FilterResult.Allow;
            public static bool ForceAllowed(this FilterResult fResult) => fResult == FilterResult.ForceAllow;
            public static bool PriorityResult(this FilterResult fResult) => fResult == FilterResult.Banned || fResult == FilterResult.ForceAllow;

            public static FilterListSet<T> MergeFilters<T>(this FilterListSet<T> listOne, FilterListSet<T> listTwo)
            {
                if (listTwo == null) return listOne;
                if (listOne == null) return listTwo;
                var newList = new FilterListSet<T>
                {
                    allowlist = ListHelpers.UnionNullableLists(listOne.allowlist, listTwo.allowlist) as Allowlist<T>,
                    whitelist = ListHelpers.UnionNullableLists(listOne.whitelist, listTwo.whitelist) as Whitelist<T>,
                    blacklist = ListHelpers.UnionNullableLists(listOne.blacklist, listTwo.blacklist) as Blacklist<T>,
                    banlist = ListHelpers.UnionNullableLists(listOne.banlist, listTwo.banlist) as Banlist<T>,
                    acceptlist = ListHelpers.UnionNullableLists(listOne.acceptlist, listTwo.acceptlist) as AcceptList<T>
                };

                return newList;
            }
            public static FilterListSet<T> MergeFilters<T>(this IEnumerable<FilterListSet<T>> lists)
            {
                if (!lists.Any()) return null;
                return lists.Aggregate((x, y) => x.MergeFilters(y));
            }
        }

        public class FilterListSet<T>
        {
            public Allowlist<T> allowlist = null;
            public Whitelist<T> whitelist = null;
            public AcceptList<T> acceptlist = null;
            public Blacklist<T> blacklist = null;
            public Banlist<T> banlist = null;

            protected List<FilterList<T>> items = null;
            public List<FilterList<T>> Items => items ??= new List<FilterList<T>> { allowlist, whitelist, blacklist, banlist, acceptlist }.Where(x => x != null).ToList();
            public bool IsEmpty() => !Items.Any();
            public bool AnyItems() => Items.Any();

            public IEnumerable<FilterResult> GetFilterResults(T item) => Items.GetFilterResults(item);
            public FilterResult GetFilterResult(T item) => Items.GetFilterResult(item);

            public FilterResult GetFilterResultFromItemList(List<T> itemList) => Items.GetFilterResultFromItemList(itemList);

            public List<T> GetAllItermsInAnyFilter() => Items.SelectMany(x => x).ToList();

            public bool AnyContains(T obj)
            {
                return Items.Any(x => x.AnyMatch(obj));
            }


            public void LoadDataFromXmlCustom(XmlNode xmlRoot)
            {
                foreach (XmlNode xmlNode in xmlRoot.ChildNodes)
                {
                    switch (xmlNode.Name)
                    {
                        case "allowlist":
                            allowlist = [];
                            allowlist.LoadDataFromXmlCustom(xmlNode);
                            break;
                        case "whitelist":
                            whitelist = [];
                            whitelist.LoadDataFromXmlCustom(xmlNode);
                            break;
                        case "blacklist":
                            blacklist = [];
                            blacklist.LoadDataFromXmlCustom(xmlNode);
                            break;
                        case "banlist":
                            banlist = [];
                            banlist.LoadDataFromXmlCustom(xmlNode);
                            break;
                        case "acceptlist":
                            acceptlist = [];
                            acceptlist.LoadDataFromXmlCustom(xmlNode);
                            break;
                        default:
                            Log.Error($"Unknown filter list type {xmlNode.Name}");
                            break;
                    }
                }
            }
        }
    }


}
