using VEF.Abilities;
using Verse;

namespace BS_VEIntegration
{
	[Verse.StaticConstructorOnStartup]
	public static class OnRaceChange
    {
		static OnRaceChange()
		{
			BigAndSmall.RaceMorpher.OnAnimalSwapped += RaceMorpher_OnAnimalSwapped;
		}

		private static void RaceMorpher_OnAnimalSwapped(Verse.Pawn originalPawn, Verse.Pawn newPawn)
		{
			CompAbilities abilitiesCompOld = originalPawn.GetComp<CompAbilities>();
			if (abilitiesCompOld != null)
			{
				CompAbilities abilitiesCompNew = newPawn.GetComp<CompAbilities>();
				if (abilitiesCompNew != null)
				{
					foreach (Ability ability in abilitiesCompOld.LearnedAbilities)
						abilitiesCompNew.GiveAbility(ability.def);
				}
			}
		}
	}
}
