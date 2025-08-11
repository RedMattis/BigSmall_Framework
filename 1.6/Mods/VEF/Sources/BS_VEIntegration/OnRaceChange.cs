using VEF.Abilities;
using Verse;

namespace BS_VEIntegration
{
	[Verse.StaticConstructorOnStartup]
	public static class OnRaceChange
    {
		static OnRaceChange()
		{
			Log.Message("Hooked in race change handler.");
			BigAndSmall.RaceMorpher.OnAnimalSwapped += RaceMorpher_OnAnimalSwapped;
		}

		private static void RaceMorpher_OnAnimalSwapped(Verse.Pawn originalPawn, Verse.Pawn newPawn)
		{
			CompAbilities abilitiesCompOld = originalPawn.GetComp<CompAbilities>();

			Log.Message($"Original pawn has abilities: {abilitiesCompOld?.LearnedAbilities?.Count}");
			if (abilitiesCompOld != null)
			{
				CompAbilities abilitiesCompNew = newPawn.GetComp<CompAbilities>();
				if (abilitiesCompNew != null)
				{
					Log.Message($"new pawn has abilities: {abilitiesCompNew?.LearnedAbilities?.Count}");

					foreach (Ability ability in abilitiesCompOld.LearnedAbilities)
						abilitiesCompNew.GiveAbility(ability.def);
				}
			}
		}
	}
}
