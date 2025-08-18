using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class RenderTreeOverride
    {
        public List<string> thingDefNames;
        public string renderTreeDefName;
    }

    public class AnimalFamilySettings
    {
        public List<string> members = [];
        public List<string> membersExact = [];
        public RomanceTags romanceTags = null;
    }

    public class HumanlikeAnimalSettings : Def
    {
        private static List<HumanlikeAnimalSettings> allSettings = null;
        public static List<HumanlikeAnimalSettings> AllHASettings =>
            allSettings ??= DefDatabase<HumanlikeAnimalSettings>.AllDefsListForReading;

        public List<string> hasHandsWildcards = [];
        public List<string> hasPoorHandsWildcards = [];
        public List<string> compWhitelist = [];
        public List<string> tabWhitelist = [];
        public List<string> modExtensionWhitelist = [];
        public List<RenderTreeOverride> renderTreeWhitelist = [];
        public List<AnimalFamilySettings> animalFamilySettings = [];
    }
}
