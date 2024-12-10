using RimWorld;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_LycanInfect : CompProperties_AbilityBloodfeederBite
    {
        public CompProperties_LycanInfect()
        {
            compClass = typeof(CompAbilityEffect_LycanInfect);
        }
    }

    /// <summary>
    /// Just apply a bunch of Vampirism.
    /// </summary>
    public class CompAbilityEffect_LycanInfect : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = target.Pawn ?? (target.Thing as Corpse)?.InnerPawn;

            if (pawn == null)
            {
                return;
            }
            ApplyLycantropy(pawn);
            base.Apply(target, dest);
        }

        private void ApplyLycantropy(Pawn pawn)
        {
            var hediffList = DefDatabase<HediffDef>.AllDefsListForReading.Where(x => x.defName == "VU_Lycantropy");

            var attacker = parent.pawn;

            if (hediffList.Count() > 0)
            {
                var hediff = hediffList.First();

                // If the pawn doesn't have the hediff, add it.
                var lycanHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
                if (lycanHediff == null)
                {
                    lycanHediff = HediffMaker.MakeHediff(hediff, pawn);
                    lycanHediff.Severity = 0.45f;
                    pawn.health.AddHediff(lycanHediff);
                }
            }
            else
            {
                Log.Warning($"Something went wrong, {pawn} hediff VU_Lycantropy could not be found. This is likely an mistake from the mod author.");
            }
        }
    }
}
