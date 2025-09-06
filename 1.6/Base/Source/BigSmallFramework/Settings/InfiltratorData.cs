using BigAndSmall.FilteredLists;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
	public class InfiltratorData
	{
		public FilterListSet<FactionDef> factionFilter = null;
		public List<XenotypeChance> doubleXenotypes = [];
		public FilterListSet<XenotypeDef> xenoFilter = null;
		public FilterListSet<ThingDef> thingFilter = null;
		public bool canFactionSwap = true;
		public bool canSwapXeno = false;
		public bool disguised = false;
		public FactionDef ideologyOf = null;
		public bool canBeFullRaid = false;
		public bool canOnlyBeFullRaid = false;
		public float? chanceOverride = null;
		public float TotalChance => chanceOverride ?? doubleXenotypes.Sum(x => x.chance);
	}
}
