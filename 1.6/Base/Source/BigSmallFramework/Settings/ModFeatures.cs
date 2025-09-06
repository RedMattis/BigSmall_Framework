using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigAndSmall.Settings
{
	public static class ModFeatures
	{
		private static HashSet<string> _enabledFeatures = new();

		public static void Initialise()
		{
			ParseEnabledFeatures();
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
