using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class GrowthRayHediff : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();
            if (Severity >= def.lethalSeverity)
            {
                // This will normally happen only if the pawn cannot be downed.

                // Apply this Hediff to all neighbouring cells
                var cells = GenRadial.RadialCellsAround(pawn.Position, pawn.BodySize, true);
                foreach (var cell in cells)
                {

                    foreach (var thing in cell.GetThingList(pawn.Map))
                    {
                        if (thing != null)
                        {
                            if (thing is Pawn pawn2)
                            {
                                var hediff = pawn2.health?.hediffSet?.GetFirstHediffOfDef(def);
                                // add severity based on proximity to the pawn
                                var difference = (pawn.Position - pawn2.Position);
                                // calculate magnitude
                                var magnitude = Mathf.Sqrt(difference.x * difference.x + difference.z * difference.z);
                                float severityChange = 0.25f / Mathf.Max(1, magnitude) * Mathf.Sqrt(pawn.BodySize);
                                if (hediff == null)
                                {
                                    hediff = HediffMaker.MakeHediff(def, pawn2);
                                    hediff.Severity = severityChange;
                                    pawn2.health.AddHediff(hediff);
                                }
                                else
                                {
                                    hediff.Severity += severityChange;
                                }
                            }
                        }
                    }
                }

                // GenExplosion
                GenExplosion.DoExplosion(pawn.Position, pawn.Map, Mathf.Sqrt(pawn.BodySize) * 1.5f, DamageDefOf.Bomb, instigator: null, damAmount: (int)(Mathf.Sqrt(pawn.BodySize) * 6), armorPenetration: 1);

                // Reduce severity to below threshold.
                Severity = 1;
                pawn.Kill(null);
            }
        }

        public override bool CauseDeathNow()
        {
            return false;
        }

    }


    public class ShrinkRayHediff : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            // In case it was a shrink ray, kill the target is they just got unreasonably tiny.
            if (pawn.BodySize <= 0.03f)
            {
                // kill pawn
                KillTarget();
            }
        }

        private void KillTarget()
        {
            DamageInfo dinfo = new DamageInfo(new DamageDef { deathMessage = "{0} popped out of existance" }, 0, intendedTarget: pawn);
            pawn.Kill(dinfo);
            if (MakeCorpse_Patch.corpse != null)
            {
                MakeCorpse_Patch.corpse.Destroy();
                MakeCorpse_Patch.corpse = null;
            }
        }
        public override bool CauseDeathNow()
        {
            return false;
        }
    }
}
