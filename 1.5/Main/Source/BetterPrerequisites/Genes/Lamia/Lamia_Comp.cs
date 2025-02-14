using RimWorld;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_AbilityLamiaFeast : CompProperties_AbilityEffect
    {
        public FloatRange relativeSizeThreshold = new(0.35f, 0.8f);

        public float maxHungerPercentThreshold = 0.6f;

        public float nutritionPerBodySize = 6.0f;

        public float energyRegained = 0.8f;

        public int maxAgeStage = 3;

        public int bloodFilthToSpawn = 1;

        public bool energyMultipliedByBodySize = false;

        public float internalBaseDamage = 10f;
        public float selfDamageMultiplier = 0.2f;


        public CompProperties_AbilityLamiaFeast()
        {
            compClass = typeof(CompAbilityEffect_LamiaBabyKiller);
        }

        public float GetSizeThreshold(Pawn pawn)
        {
            var sm = HumanoidPawnScaler.GetCacheUltraSpeed(pawn, canRegenerate: false).scaleMultiplier.linear;
            float divisor = StatWorker_MaxNutritionFromSize.GetNutritionMultiplier(sm);
            float mNutrition = pawn.GetStatValue(StatDefOf.MaxNutrition) / divisor * pawn.def.GetStatValueAbstract(StatDefOf.MaxNutrition);
            // Maps maxNut 1 to 0, and 4 to 1.0
            float asRange = (mNutrition - 1) / 4;
            return relativeSizeThreshold.ClampToRange(relativeSizeThreshold.LerpThroughRange(asRange));
        }

        //public override IEnumerable<string> ExtraStatSummary()
        //{
        //    yield return "AbilityHemogenGain".Translate() + ": " + (hemogenGain * 100f).ToString("F0");

        //}
    }
}
