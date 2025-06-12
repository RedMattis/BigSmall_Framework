using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace BigAndSmall
{
    public class Flagger : DefModExtension
    {
        public float priority = 0;
        public List<FlagString> flags = [];

        public static List<FlagString> GetTagStrings(Pawn pawn, bool includeInactive)
        {
            var flagList = includeInactive ? ModExtHelper.GetAllExtensionsPlusInactive<Flagger>(pawn) : ModExtHelper.GetAllExtensions<Flagger>(pawn);
            if (flagList.Any())
            {
                return [.. flagList.OrderByDescending(x => x.priority).SelectMany(x => x.flags)];
            }
            return [];
        }
    }

    public class FlagString : IEquatable<FlagString>
    {
        private const string DEFAULT = "default";

        public string mainTag;
        public string subTag = DEFAULT;
        public List<string> extraTags = [];

        public bool Equals(FlagString other) => mainTag == other.mainTag && subTag == other.subTag && extraTags.SequenceEqual(other.extraTags);
        public override bool Equals(object obj)
        {
            if (obj is FlagString other)
            {
                return mainTag == other.mainTag && subTag == other.subTag && extraTags.SequenceEqual(other.extraTags);
            }
            return false;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (mainTag?.GetHashCode() ?? 0);
                hash = hash * 23 + (subTag?.GetHashCode() ?? 0);
                return hash;
            }
        }
        public override string ToString() => $"{mainTag}/{subTag}" + (extraTags.Any() ? $"[{string.Join(",", extraTags)}]" : "");

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            mainTag = xmlRoot.Name;
            if (xmlRoot.InnerText != "")
            {
                subTag = xmlRoot.InnerText;
            }
            extraTags = xmlRoot.Attributes?.OfType<XmlAttribute>().Select(x => x.Value).ToList() ?? [];
        }
    }
}
