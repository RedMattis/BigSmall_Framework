using BigAndSmall.EventArgs;
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

		private static void RaceMorpher_OnAnimalSwapped(object sender, AnimalSwappedEventArgs e)
		{
			CompAbilities abilitiesCompOld = e.OriginalPawn.GetComp<CompAbilities>();
			if (abilitiesCompOld != null)
			{
				CompAbilities abilitiesCompNew = e.NewPawn.GetComp<CompAbilities>();
				if (abilitiesCompNew != null)
				{
					foreach (Ability ability in abilitiesCompOld.LearnedAbilities)
						abilitiesCompNew.GiveAbility(ability.def);
				}
			}
		}
	}
}
