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
    public class FlagString : IExposable, IEquatable<FlagString>
    {
        private const string DEFAULT = "default";
        public string mainTag;
        public string subTag = DEFAULT;
        public Dictionary<string, string> extraData = [];
        public FlagStringStateData Data { get { return field ??= FlagStringData.DataFor(this); } set { field = value; } }
        public string Label { get { return field ??= GetCustomLabel() ?? Data?.label ?? ToStringShort(); } set { field = value; } }
        public EditPawnWindow.WindowTab? DisplayTab { get { return field ??= Data?.displayTab; } set { field = value; } }
        public string CustomCategory { get { return field ??= Data?.customCategory; } set { field = value; } }

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

        public void ClearCache()
        {
            Label = null;
            DisplayTab = null;
            CustomCategory = null;
            Data = null;
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
        public string GetCustomLabel() => extraData.TryGetValue("Label", out var label) ? label : null;
        public override string ToString() => $"{mainTag}/{subTag}" + (extraData.Any() ? $"[{string.Join(",", extraData)}]" : "");
        public string ToStringShort() => subTag == DEFAULT ? mainTag : $"{mainTag}, {subTag}";

        public void LoadDataFromXML(XmlNode node)
        {
            if (node.Name == "li")
            {
                SetupSimple(node);
                return;
            }
            if (node.NodeType == XmlNodeType.Text)
            {
                SetupSimple(node);
                return;
            }

            mainTag = node.Name;
            if (node.InnerText != "")
            {
                subTag = node.InnerText;
            }
            extraData = node.Attributes?
                .OfType<XmlAttribute>()
                .ToDictionary(attr => attr.Name, attr => attr.Value) ?? [];

            void SetupSimple(XmlNode node)
            {
                mainTag = node.InnerText;
                subTag = DEFAULT;
                extraData = [];
            }
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
