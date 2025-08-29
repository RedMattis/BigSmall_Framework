using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;
using Verse.AI;

namespace BigAndSmall
{
    public class Flagger : DefModExtension
    {
        public float priority = 0;
        public FlagStringList flags = [];

        public static List<FlagString> GetTagStrings(Pawn pawn, bool includeInactive)
        {
			if (pawn == null) 
				return [];

			List<Flagger> result = new List<Flagger>(20);

			result.AddRange(includeInactive ? ModExtHelper.GetAllExtensionsPlusInactive<Flagger>(pawn) : ModExtHelper.GetAllExtensions<Flagger>(pawn, doSort: false));

			FactionDef factionDef = pawn.Faction?.def;
			if (factionDef != null)
				result.AddRange(factionDef.ExtensionsOnDef<Flagger, FactionDef>(doSort: false));

			if (pawn.kindDef != null)
				result.AddRange(pawn.kindDef.ExtensionsOnDef<Flagger, PawnKindDef>(doSort: false));

			result.AddRange(ModExtHelper.GetAllExtensionsOnBackStories<Flagger>(pawn));

            if (result.Count > 0)
            {
                return result.OrderByDescending(x => x.priority).SelectMany(x => x.flags).ToList();
            }
            return [];
        }
    }

    public class FlagString : IExposable
    {
        private const string DEFAULT = "default";

        public string mainTag;
        public string subTag = DEFAULT;
        public Dictionary<string, string> extraData = [];
        public string Label => extraData.TryGetValue("Label", out var label) ? label : mainTag;

        public bool Equals(FlagString other) => this?.mainTag != null && other?.mainTag != null
            && mainTag == other.mainTag && subTag == other.subTag;
        public override bool Equals(object obj)
        {
            if (obj is FlagString other)
            {
                return mainTag == other.mainTag && subTag == other.subTag;
            }
            return false;
        }

        public static bool operator ==(FlagString left, FlagString right) => left?.Equals(right) ?? right is null;
        public static bool operator !=(FlagString left, FlagString right) => !(left == right);

        public bool MainTagEquals(FlagString other) => this?.mainTag != null && other?.mainTag != null
            && mainTag == other.mainTag;

        /// <summary>
        /// If the mainTag and subTag are identical, merges the extraData dictionaries, preferring this.extraData on key conflicts.
        /// </summary>
        public FlagString TryFuseIdentical(FlagString other)
        {
            if (other != this) return null;
            var combinedExtra = new Dictionary<string, string>(extraData);
            foreach (var kvp in other.extraData)
            {
                if (!combinedExtra.ContainsKey(kvp.Key))
                {
                    combinedExtra[kvp.Key] = kvp.Value;
                }
            }
            return new FlagString()
            {
                mainTag = mainTag,
                subTag = subTag,
                extraData = combinedExtra
            };
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
        public override string ToString() => $"{mainTag}/{subTag}" + (extraData.Any() ? $"[{string.Join(",", extraData)}]" : "");

        public void LoadDataFromXML(XmlNode node)
        {
            mainTag = node.Name;
            if (node.InnerText != "")
            {
                subTag = node.InnerText;
            }
            extraData = node.Attributes?
                .OfType<XmlAttribute>()
                .ToDictionary(attr => attr.Name, attr => attr.Value) ?? [];
        }
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            var node = xmlRoot.FirstChild;
            LoadDataFromXML(node);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref mainTag, "mainTag");
            Scribe_Values.Look(ref subTag, "subTag", DEFAULT);
            Scribe_Collections.Look(ref extraData, "extraData", LookMode.Value, LookMode.Value);
        }
    }
    public class FlagStringList : List<FlagString>
    {
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode cNode in xmlRoot.ChildNodes)
            {
                var fs = new FlagString();
                fs.LoadDataFromXML(cNode);
                Add(fs);
            }
        }
    }
}
