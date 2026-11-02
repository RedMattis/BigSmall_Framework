using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    public class StatScaling
    {
        public string tag;
        public StatDef stat;
        public SimpleCurve curve;
        public bool isOffset;
    }
    public class ProjectileByStat
    {
        public class ProjectileAtValue
        {
            public float value;
            public ThingDef def;
        }
        public StatDef stat;
        public List<ProjectileAtValue> projectileAtValue = [];

        public ThingDef GetProjectileByStat(ThingDef previous, Pawn pawn)
        {
            float statV = stat == null ? 1 : pawn.GetStatValue(stat, cacheStaleAfterTicks: 100);
            ProjectileAtValue result = null;
            foreach(var pData in projectileAtValue)
            {
                if (pData.value > statV)
                    continue;
                if (result == null || result.value < pData.value)
                    result = pData;
            }
            if (result == null) return previous;
            return result.def;
        }
    }

    public static class EffectByStatExtension
    {
        extension(IEnumerable<StatScaling> scalings)
        {
            public float ApplyScaling(float offset, string tag, Pawn pawn)
            {
                float factor = 1;
                foreach (var scaling in scalings.Where(x => x.isOffset && x.tag == tag))
                {
                    offset += scaling.curve.Evaluate(pawn.GetStatValue(scaling.stat, cacheStaleAfterTicks: 100));
                }
                foreach (var scaling in scalings.Where(x => !x.isOffset && x.tag == tag))
                {
                    factor += scaling.curve.Evaluate(pawn.GetStatValue(scaling.stat, cacheStaleAfterTicks: 100));
                }
                return offset * factor;
            }
        }
    }

    public class CompProperties_AbilitySprayLiquid : CompProperties_AbilityEffect
    {
        public ThingDef projectileDef;

        public int radiusToHit;

        public EffecterDef sprayEffecter;
        public int projectileCount = 1;

        public List<StatScaling> scaling = [];
        public ProjectileByStat projectileByStat;
        public ThingDef GetProjectile(Pawn pawn) => projectileByStat?.GetProjectileByStat(projectileDef, pawn) ?? projectileDef;
        public CompProperties_AbilitySprayLiquid()
        {
            compClass = typeof(CompAbilityEffect_SprayLiquid);
        }
    }

    public class CompAbilityEffect_SprayLiquid : CompAbilityEffect
    {
        private List<Pair<IntVec3, float>> tmpCellDots = [];

        private List<IntVec3> tmpCells = [];

        private new CompProperties_AbilitySprayLiquid Props => (CompProperties_AbilitySprayLiquid)props;

        private Pawn Pawn => parent.pawn;

        public ThingDef GetProjectile() => Props.GetProjectile(Pawn);
        public int GetRadius() => (int)Props.scaling.ApplyScaling(Props.radiusToHit, "AoE", Pawn);

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var projectileDef = GetProjectile();
            foreach (IntVec3 item in AffectedCells(target))
            {
                ((Projectile)GenSpawn.Spawn(projectileDef, Pawn.Position, Pawn.Map)).Launch(Pawn, Pawn.DrawPos, item, item, ProjectileHitFlags.IntendedTarget);
            }
            if (Props.sprayEffecter != null)
            {
                Props.sprayEffecter.Spawn(parent.pawn.Position, target.Cell, parent.pawn.Map).Cleanup();
            }
            base.Apply(target, dest);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            int radius = GetRadius();
            var projectileDef = GetProjectile();
            if (projectileDef?.projectile?.explosionRadius > 0)
            {
                radius += Mathf.FloorToInt(projectileDef.projectile.explosionRadius);
            }
            GenDraw.DrawFieldEdges(AffectedCells(target, radius));
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (Pawn.Faction != null)
            {
                int radius = GetRadius();
                var projectileDef = GetProjectile();
                if (projectileDef?.projectile?.explosionRadius > 0)
                {
                    radius += Mathf.FloorToInt(projectileDef.projectile.explosionRadius);  
                }

                foreach (IntVec3 item in AffectedCells(target, radiusOverride: radius))
                {
                    List<Thing> thingList = item.GetThingList(Pawn.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i].Faction == Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        protected List<IntVec3> AffectedCells(LocalTargetInfo target, int? radiusOverride=null)
        {
            tmpCellDots.Clear();
            tmpCells.Clear();
            tmpCellDots.Add(new Pair<IntVec3, float>(target.Cell, 999f));

            Vector3 targetVector = target.Cell.ToVector3Shifted().Yto0();

            int radius = radiusOverride ?? GetRadius();

            if (radius > 0)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, radius, true))
                {
                    Vector3 cellVector = cell.ToVector3Shifted().Yto0();
                    float dotProduct = Vector3.Dot((cellVector - targetVector).normalized, targetVector.normalized);
                    tmpCellDots.Add(new Pair<IntVec3, float>(cell, dotProduct));
                }

                tmpCellDots.SortByDescending((Pair<IntVec3, float> x) => x.Second);
            }

            foreach (Pair<IntVec3, float> cellDot in tmpCellDots)
            {
                IntVec3 cell = cellDot.First;
                if (cell.InBounds(Pawn.Map) && !cell.Filled(Pawn.Map) &&
                    GenSight.LineOfSight(Pawn.Position, cell, Pawn.Map, skipFirstCell: true))
                {
                    tmpCells.Add(cell);
                }
            }

            return tmpCells;
        }
    }

    // Cone version.

    public class CompProperties_AbilityConeAttack : CompProperties_AbilityEffect
    {
        public ThingDef projectileDef;
        public int maxDistance = 10;
        [Obsolete("Mispelling")]
        public int minDistnace = 0;
        public int minDistance = 0;
        public int maxAngle = 90;
        public int minAngle = 90;
        public int maxConeLength = 9999;
        public int minimumRadiusAroundTarget = 0;
        public List<StatScaling> scaling = [];
        public ProjectileByStat projectileByStat;
        public ThingDef GetProjectile(Pawn pawn) => projectileByStat?.GetProjectileByStat(projectileDef, pawn) ?? projectileDef;

        public CompProperties_AbilityConeAttack()
        {
            compClass = typeof(CompAbilityEffect_ConeAttack);
        }
    }

    public class CompAbilityEffect_ConeAttack : CompAbilityEffect
    {
        private new CompProperties_AbilityConeAttack Props => (CompProperties_AbilityConeAttack)props;
        private Pawn Pawn => parent.pawn;

        public int GetMaxDistance() => (int)Props.scaling.ApplyScaling(Props.maxDistance, "MaxRange", Pawn);
        public int GetMinDistance() => (int)Props.scaling.ApplyScaling(Mathf.Max(Props.minDistance, Props.minDistnace), "MinDistance", Pawn);

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var projectile = Props.GetProjectile(Pawn);
            foreach (IntVec3 cell in AffectedCells(target))
            {
                ((Projectile)GenSpawn.Spawn(projectile, Pawn.Position, Pawn.Map)).Launch(Pawn, Pawn.DrawPos, cell, cell, ProjectileHitFlags.IntendedTarget);
            }
            base.Apply(target, dest);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(AffectedCells(target));
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (Pawn.Faction != null)
            {
                foreach (IntVec3 item in AffectedCells(target))
                {
                    List<Thing> thingList = item.GetThingList(Pawn.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i].Faction == Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            var affectedCells = new List<IntVec3>();
            Vector3 targetPos = target.Cell.ToVector3Shifted();
            Vector3 startPosition = Pawn.Position.ToVector3Shifted();
            var originalStartPosition = startPosition;

            var minDistance = GetMinDistance();
            var maxDistance = GetMaxDistance();

            // If the targetPos is closer than the min distance, push it out to that distance.
            if ((targetPos - startPosition).magnitude < minDistance)
            {
                targetPos = startPosition + (targetPos - startPosition).normalized * minDistance;
            }
            // If the distance between the targetPos and startPos is longer than the MaxConeLength push out the startPos to that distance.
            // Basically I'm making the cone origin be pushed out.
            if ((targetPos - startPosition).magnitude > Props.maxConeLength)
            {
                startPosition = targetPos - (targetPos - startPosition).normalized * Props.maxConeLength;
            }

            float distanceToTarget = (targetPos - startPosition).magnitude;
            float distanceToTargetFromOriginal = (targetPos - originalStartPosition).magnitude;

            float percentOfMaxDistnace = distanceToTargetFromOriginal / maxDistance;

            float angleAtDistance = Mathf.Lerp(Props.maxAngle, Props.minAngle, percentOfMaxDistnace);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(startPosition.ToIntVec3(), distanceToTarget, true))
            {
                Vector3 cellPos = cell.ToVector3Shifted();
                Vector3 direction = (cellPos - startPosition).normalized;
                float currentDistance = (targetPos - startPosition).magnitude;
                float angle = Vector3.Angle(direction, targetPos - startPosition);

                if (angle <= angleAtDistance / 2f &&
                    GenSight.LineOfSight(startPosition.ToIntVec3(), cell, Pawn.Map, skipFirstCell: true) &&
                    !cell.Equals(Pawn.Position)) // Check if it's not the cell the pawn is standing on
                {
                    affectedCells.Add(cell);
                }
            }
            // Same thing around the target cell based on minimumRadiusAroundTarget
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, Props.minimumRadiusAroundTarget, true))
            {
                Vector3 cellPos = cell.ToVector3Shifted();
                Vector3 direction = (cellPos - targetPos).normalized;
                float currentDistance = (targetPos - startPosition).magnitude;
                if (GenSight.LineOfSight(target.Cell, cell, Pawn.Map, skipFirstCell: true) &&
                                                          !cell.Equals(Pawn.Position)) // Check if it's not the cell the pawn is standing on
                {
                    affectedCells.Add(cell);
                }
            }
            affectedCells = [.. affectedCells.Distinct()];
            return affectedCells;
        }
    }

}
