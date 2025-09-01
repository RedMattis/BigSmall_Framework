using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace BigAndSmall.SimpleCustomRaces.SapientAnimals
{
	[HarmonyPatch(typeof(CompAbilityEffect_RequiresTrainable))]
	internal static class TrainableAbilitiesPatch
	{
		[HarmonyPatch("HasLearnedTrainable", MethodType.Getter), HarmonyPrefix]
		public static bool HasLearnedTrainable_Prefix(CompAbilityEffect_RequiresTrainable __instance, ref bool __result)
		{
			if (__instance?.parent?.pawn?.def.race.Humanlike == true)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}
}
