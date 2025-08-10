using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_AbilitySprayLiquid : CompProperties_AbilityEffect
    {
        public ThingDef projectileDef;

        public int radiusToHit;

        public EffecterDef sprayEffecter;

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

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            foreach (IntVec3 item in AffectedCells(target))
            {
                ((Projectile)GenSpawn.Spawn(Props.projectileDef, Pawn.Position, Pawn.Map)).Launch(Pawn, Pawn.DrawPos, item, item, ProjectileHitFlags.IntendedTarget);
            }
            if (Props.sprayEffecter != null)
            {
                Props.sprayEffecter.Spawn(parent.pawn.Position, target.Cell, parent.pawn.Map).Cleanup();
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
            tmpCellDots.Clear();
            tmpCells.Clear();
            tmpCellDots.Add(new Pair<IntVec3, float>(target.Cell, 999f));

            Vector3 targetVector = target.Cell.ToVector3Shifted().Yto0();

            if (Props.radiusToHit > 0)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, Props.radiusToHit, true))
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
        public int maxDistance = 100;
        public int minDistnace = 0;
        public int maxAngle = 90;
        public int minAngle = 90;
        public int maxConeLength = 9999;
        public int minimumRadiusAroundTarget = 0;

        public CompProperties_AbilityConeAttack()
        {
            compClass = typeof(CompAbilityEffect_ConeAttack);
        }
    }

    public class CompAbilityEffect_ConeAttack : CompAbilityEffect
    {
        private new CompProperties_AbilityConeAttack Props => (CompProperties_AbilityConeAttack)props;
        private Pawn Pawn => parent.pawn;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            foreach (IntVec3 cell in AffectedCells(target))
            {
                ((Projectile)GenSpawn.Spawn(Props.projectileDef, Pawn.Position, Pawn.Map)).Launch(Pawn, Pawn.DrawPos, cell, cell, ProjectileHitFlags.IntendedTarget);
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

            // If the targetPos is closer than the min distance, push it out to that distance.
            if ((targetPos - startPosition).magnitude < Props.minDistnace)
            {
                targetPos = startPosition + (targetPos - startPosition).normalized * Props.minDistnace;
            }
            // If the distance between the targetPos and startPos is longer than the MaxConeLength push out the startPos to that distance.
            if ((targetPos - startPosition).magnitude > Props.maxConeLength)
            {
                startPosition = targetPos - (targetPos - startPosition).normalized * Props.maxConeLength;
            }

            float distanceToTarget = (targetPos - startPosition).magnitude;
            float distanceToTargetFromOriginal = (targetPos - originalStartPosition).magnitude;

            float percentOfMaxDistnace = distanceToTargetFromOriginal / Props.maxDistance;

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
            // Removed duplicates
            affectedCells = affectedCells.Distinct().ToList();
            return affectedCells;
        }
    }

}
