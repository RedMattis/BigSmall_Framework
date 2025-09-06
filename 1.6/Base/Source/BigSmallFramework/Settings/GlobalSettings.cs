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
	public class GlobalSettings : Def
	{
		public static Dictionary<string, GlobalSettings> globalSettings = DefDatabase<GlobalSettings>.AllDefs.ToDictionary(x => x.defName);

		public List<string> enabledFeatures = [];

		public List<List<string>> alienGeneGroups = [];
		public List<XenotypeChance> returnedXenotypes = [];
		public List<XenotypeChance> returnedXenotypesColonist = [];
		public List<InfiltratorData> infiltratorTypes = [];

		[Unsaved(false)]
		private static List<List<GeneDef>> alienGeneGroupsDefs = null;

		public static XenotypeDef GetRandomReturnedXenotype => globalSettings
			.SelectMany(x => x.Value.returnedXenotypes)
			.TryRandomElementByWeight(x => x.chance, out var result) ? result.xenotype : null;

		public static XenotypeDef GetRandomReturnedColonistXenotype => globalSettings
			.SelectMany(x => x.Value.returnedXenotypesColonist)
			.TryRandomElementByWeight(x => x.chance, out var result) ? result.xenotype : null;

		public static void Initialize()
		{
			globalSettings = DefDatabase<GlobalSettings>.AllDefsListForReading.ToDictionary(x => x.defName);
		}

		public static (XenotypeDef def, InfiltratorData data) GetRandomInfiltratorReplacementXenotype(Pawn pawn, int seed, bool forceNeeded, bool isFullRaid)
		{
			List<InfiltratorData> allValidInfiltratorData = globalSettings.Values.SelectMany(x => x.infiltratorTypes).ToList();
			if (pawn.Faction != null)
			{
				allValidInfiltratorData = [.. allValidInfiltratorData.Where(x =>
					x.doubleXenotypes.Any() &&
					(!x.canOnlyBeFullRaid || (x.canOnlyBeFullRaid && isFullRaid)) &&
					(!isFullRaid || x.canBeFullRaid) &&
					(!forceNeeded || x.canSwapXeno) &&
					(x.factionFilter == null || x.factionFilter.GetFilterResult(pawn.Faction.def).Accepted()) &&
					(x.thingFilter == null || x.thingFilter.GetFilterResult(pawn.def).Accepted()) &&
					(x.xenoFilter == null || (pawn.genes?.Xenotype is XenotypeDef pXDef && x.xenoFilter.GetFilterResult(pXDef).Accepted()))
					)];
			}
			if (allValidInfiltratorData.Count == 0 || allValidInfiltratorData.All(x => x.doubleXenotypes?.Count == 0)) return (null, null);
			// Return xenotype based on chance.

			InfiltratorData data;
			// Ensure we're getting infiltrators from the same "group" if doing full infiltrator raid.
			// Mostly to avoid stupid results like succubi mixed with synths.
			using (new RandBlock(seed))
			{
				data = allValidInfiltratorData.RandomElementByWeight(x => x.TotalChance);
			}
			XenotypeDef resultXeno = allValidInfiltratorData.SelectMany(x => x.doubleXenotypes).ToList().RandomElementByWeight(x => x.chance).xenotype;

			return (resultXeno, allValidInfiltratorData.First(x => x.doubleXenotypes.Any(y => y.xenotype == resultXeno)));
		}

		public static List<List<GeneDef>> GetAlienGeneGroups()
		{
			if (alienGeneGroupsDefs == null)
			{
				alienGeneGroupsDefs = [];
				foreach (var settings in globalSettings.Values.Where(x => x.alienGeneGroups != null))
				{
					foreach (var group in settings.alienGeneGroups)
					{
						if (group.NullOrEmpty())
						{
							continue;
						}
						var geneGroup = new List<GeneDef>();
						foreach (var geneDef in group.Select(x => DefDatabase<GeneDef>.GetNamed(x, false)))
						{
							if (geneDef != null)
							{
								geneGroup.Add(geneDef);
							}
						}
						if (geneGroup.Count > 0)
						{
							alienGeneGroupsDefs.Add(geneGroup);
						}
					}
				}
			}
			return alienGeneGroupsDefs;
		}
	}
}
