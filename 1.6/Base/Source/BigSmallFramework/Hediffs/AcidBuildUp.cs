﻿using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class AcidBuildUp : Hediff
    {
        public static DamageDef acidDmgDef;

        public static DamageDef AcidDmgDef
        {
            get
            {
                if (acidDmgDef == null)
                {
                    acidDmgDef = DefDatabase<DamageDef>.GetNamed("BS_AcidDmgDirect");
                    // Fallback to the vanilla AcidBurn if for whatever reason the def is missing.
                    acidDmgDef ??= DefDatabase<DamageDef>.GetNamed("AcidBurn");
                }
                
                return acidDmgDef;
            }
        }

        const float totalDamageAtMaxSeverity = 40;
        const float totalDurationAtOneSeverity = 2500; // In ticks
        const int ticksBetweenDamage = 200;

        public override string LabelInBrackets
        {
            get
            {
                return Severity.ToStringPercent();
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % ticksBetweenDamage == 0 && pawn != null && !pawn.Dead)
            {
                if (Severity > 3) Severity = 3;
                float baseDamage = totalDamageAtMaxSeverity * ticksBetweenDamage / totalDurationAtOneSeverity;

                float damage = baseDamage * Mathf.Lerp(pawn.BodySize, pawn.HealthScale, 0.5f);
                Severity -= ticksBetweenDamage / totalDurationAtOneSeverity;

                pawn.TakeDamage(new DamageInfo(AcidDmgDef, damage, armorPenetration: 300));
            }
        }
    }
}
