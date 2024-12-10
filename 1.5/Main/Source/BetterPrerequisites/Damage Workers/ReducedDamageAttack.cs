using Verse;

namespace BigAndSmall
{
    public class DamageWorker_OuterAndFullDamage : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            dinfo.SetAmount(dinfo.Amount);
            dinfo.SetAllowDamagePropagation(false);
            return base.Apply(dinfo, thing);
        }

        protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, BodyPartDepth.Outside);
        }
    }

    /// <summary>
    /// Used to make Rimworld's attack picker think the attack deals 50% more damage than it actually does.
    /// </summary>
    public class DamageWorker_TwoThirdsDamage : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            dinfo.SetAmount(dinfo.Amount * 0.66f);
            return base.Apply(dinfo, thing);
        }
    }

    /// <summary>
    /// Used to make Rimworld's attack picker think the attack deals 100% more damage than it actually does.
    /// </summary>
    public class DamageWorker_HalfDamage : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            dinfo.SetAmount(dinfo.Amount * 0.5f);
            return base.Apply(dinfo, thing);
        }
    }

    /// <summary>
    /// Basically this Damageworker deals only half as much damage and attacks only the outer body parts.
    /// </summary>
    public class DamageWorker_OuterAndHalfDamage : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            dinfo.SetAmount(dinfo.Amount * 0.5f);
            dinfo.SetAllowDamagePropagation(false);
            return base.Apply(dinfo, thing);
        }

        protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, BodyPartDepth.Outside);
        }
    }

    public class DamageWorker_OuterAndQuarterDamage : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            dinfo.SetAmount(dinfo.Amount * 0.25f);
            dinfo.SetAllowDamagePropagation(false);
            return base.Apply(dinfo, thing);
        }

        protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, BodyPartDepth.Outside);
        }
    }

    

    public class ProjectileGorgonStareProjectile : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (blockedByShield || def.projectile.explosionDelay == 0)
            {
                Explode();
                return;
            }
            landed = true;
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef, launcher.Faction, launcher);
        }

        protected virtual void Explode()
        {
            Map map = base.Map;
            Destroy();
            if (def.projectile.explosionEffect != null)
            {
                Effecter effecter = def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(base.Position, map), new TargetInfo(base.Position, map));
                effecter.Cleanup();
            }
            GenExplosion.DoExplosion(Position,
                                     map,
                                     def.projectile.explosionRadius,
                                     def.projectile.damageDef,
                                     launcher,
                                     DamageAmount,
                                     ArmorPenetration,
                                     null,
                                     equipmentDef,
                                     def,
                                     intendedTarget.Thing,
                                     def.projectile.postExplosionSpawnThingDef,
                                     postExplosionSpawnThingDefWater: def.projectile.postExplosionSpawnThingDefWater,
                                     postExplosionSpawnChance: def.projectile.postExplosionSpawnChance,
                                     postExplosionSpawnThingCount: def.projectile.postExplosionSpawnThingCount,
                                     postExplosionGasType: def.projectile.postExplosionGasType,
                                     preExplosionSpawnThingDef: def.projectile.preExplosionSpawnThingDef,
                                     preExplosionSpawnChance: def.projectile.preExplosionSpawnChance,
                                     preExplosionSpawnThingCount: def.projectile.preExplosionSpawnThingCount,
                                     applyDamageToExplosionCellsNeighbors: def.projectile.applyDamageToExplosionCellsNeighbors,
                                     chanceToStartFire: def.projectile.explosionChanceToStartFire,
                                     damageFalloff: def.projectile.explosionDamageFalloff,
                                     direction: origin.AngleToFlat(destination),
                                     ignoredThings: null,
                                     affectedAngle: null,
                                     doVisualEffects: false,
                                     propagationSpeed: def.projectile.damageDef.expolosionPropagationSpeed,
                                     excludeRadius: 0f,
                                     doSoundEffects: false,
                                     screenShakeFactor: def.projectile.screenShakeFactor);
        }
    }

}
