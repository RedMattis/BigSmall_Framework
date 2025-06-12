using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_AbilityLamiaFeast : CompProperties_AbilityEffect
    {
        public float relativeSizeThreshold = 0.6f;

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

        //public override IEnumerable<string> ExtraStatSummary()
        //{
        //    yield return "AbilityHemogenGain".Translate() + ": " + (hemogenGain * 100f).ToString("F0");

        //}
    }
}
