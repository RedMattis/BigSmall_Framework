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
    public class FlagStringStateData(EditPawnWindow.WindowTab? category, string customCategory, string label)
    {
        public EditPawnWindow.WindowTab? displayTab = category;
        public string customCategory = customCategory;
        public string label = label;
    }

    public class FlagStringData : Def
    {
        private static Dictionary<FlagString, FlagStringStateData> allFlagStringData = null;

        public FlagStringList flags = [];
        public EditPawnWindow.WindowTab? displayTab = null;
        public string customCategory = null;

        public static FlagStringStateData DataFor(FlagString fs)
        {
            if (Setup().TryGetValue(fs, out var data))
            {
                return data;
            }
            return new FlagStringStateData(null, null, null);
        }

        public static Dictionary<FlagString, FlagStringStateData> Setup()
        {
            if (allFlagStringData != null)
            {
                return allFlagStringData;
            }
            allFlagStringData = [];
            foreach (var fsd in DefDatabase<FlagStringData>.AllDefs)
            {
                foreach (var fs in fsd.flags)
                {
                    if (allFlagStringData.TryGetValue(fs) is FlagStringStateData existingVal)
                    {
                        existingVal.displayTab = fsd.displayTab ?? existingVal.displayTab;
                        existingVal.customCategory = fsd.customCategory ?? existingVal.customCategory;
                        existingVal.label = fsd.label;
                        allFlagStringData[fs] = existingVal;
                    }
                    else
                    {
                        allFlagStringData[fs] = new FlagStringStateData(fsd.displayTab, fsd.customCategory, fsd.label);
                    }
                }
            }
            Log.Message($"[FlagStringData] Loaded {allFlagStringData.Count}: {string.Join(", ", allFlagStringData.Keys.Select(f => $" {f?.Label} ({f?.ToStringShort()})"))}");
            return allFlagStringData;
        }
    }
}
