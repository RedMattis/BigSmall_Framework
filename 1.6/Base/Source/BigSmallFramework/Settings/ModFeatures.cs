using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall.Settings
{
	public static class ModFeatures
	{
		private static HashSet<string> _enabledFeatures = new();
		private static List<Def> _buffer = new(100);

		public static void Initialise()
		{
			ParseEnabledFeatures();
			ProcessConditionalFeatureDefs();
		}


		private static void ProcessConditionalFeatureDefs()
		{
			RemoveDefsOfType<ThingDef>();
			RemoveDefsOfType<RecipeDef>();
			RemoveDefsOfType<ResearchTabDef>();
			RemoveDefsOfType<ResearchProjectDef>();
		}

		private static void RemoveDefsOfType<T>() where T : Def
		{
			_buffer.Clear();
			_buffer.AddRange(DefDatabase<T>.AllDefs);
			foreach (T def in _buffer)
			{
				var conditional = def.GetModExtension<ConditionalDefExtension>();
				if (conditional != null)
				{
					// If none of the required features are enabled, remove the def.
					if (conditional.requiredFeatures.Any(IsFeatureEnabled) == false)
					{
						DefDatabase<T>.AllDefsListForReading.Remove(def);
					}
				}
			}
		}

		private static void ParseEnabledFeatures()
		{
			foreach (KeyValuePair<string, GlobalSettings> item in GlobalSettings.globalSettings)
			{
				List<string> enabledFeatures = item.Value.enabledFeatures;
				for (int i = enabledFeatures.Count - 1; i >= 0; i--)
				{
					string feature = enabledFeatures[i].Trim().ToLower();
					if (_enabledFeatures.Contains(feature))
						enabledFeatures.Add(feature);
				}
			}
		}

		public static bool IsFeatureEnabled(string featureName)
		{
			return _enabledFeatures.Contains(featureName, StringComparer.OrdinalIgnoreCase);
		}
	}
}
