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
}
