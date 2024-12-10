using RimWorld;
using Verse;

namespace BigAndSmall
{
    // Props
    public class CompProperties_MeleeAttackAbility : CompProperties_AbilityEffect
    {
        public DamageDef damageDef;
        public int damageAmount;
        public int armorPenetration;
        public float screenShakeFactor = 0.1f;


        public bool asExplosion = false;

        public CompProperties_MeleeAttackAbility()
        {
            compClass = typeof(CompAbilityEffect_MeleeAttackAbility);
        }
    }

    //Ability
    internal class CompAbilityEffect_MeleeAttackAbility : CompAbilityEffect
    {
        public new CompProperties_MeleeAttackAbility Props => (CompProperties_MeleeAttackAbility)props;


        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                PerformAttack(parent.pawn, pawn);
            }
        }

        public void PerformAttack(Pawn attacker, Thing target)
        {
            if (!Props.asExplosion)
            {
                // Deal damage
                DamageInfo dinfo = new DamageInfo(Props.damageDef, Props.damageAmount, 0, -1, attacker, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                target.TakeDamage(dinfo);
            }
            else
            {
                // Deal damage
                DamageInfo dinfo = new DamageInfo(Props.damageDef, Props.damageAmount, 0, -1, attacker, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                GenExplosion.DoExplosion(target.Position,
                    target.Map,
                    0.9f,
                    Props.damageDef,
                    attacker,
                    Props.damageAmount,
                    Props.armorPenetration,
                    null,
                    null,
                    null,
                    target,
                    null,
                    ignoredThings: null,
                    affectedAngle: null,
                    doVisualEffects: false,
                    excludeRadius: 0f,
                    doSoundEffects: false,
                    screenShakeFactor: Props.screenShakeFactor);
            }

        }
    }
}
