using BigAndSmall;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace BigAndSmall
{

    public class SoulResourceGizmo : Gizmo_ResourceBase
    {
        public SoulResourceGizmo(SoulResourceHediff resource)
        {
            this.resource = resource;
        }
        protected override bool IsDraggable => false;
        protected override Color BarColor => new ColorInt(60, 30, 90).ToColor;
        protected override Color BarHighlightColor => new ColorInt(100, 50, 150).ToColor;

        protected override string GetTooltip()
        {
            return "";
        }
    }

    public class SoulResourceHediff : Hediff, IResourcePool
    {
        const int tickRateRegen = 50;
        protected int rechargeCooldown = 0;
        protected int rechargeCooldownMax = 500;
        public int refreshState = 0;

        protected float targetValue = 1;
        protected float max = 1;
        protected float cur = 1;

        

        public Pawn Pawn => pawn;
        public float TargetValue { get => targetValue; set => targetValue = value; }
        public float Value
        {
            get => cur;
            set
            {
                if (value < cur)
                {
                    rechargeCooldown = rechargeCooldownMax;
                }
                cur = Mathf.Clamp(value, 0, max);
            }
        }
        public float Max { get => max; set => max = value; }
        public float ValueForDisplay => (int)(cur*100);
        public float MaxForDisplay => (int)(max*100);
        public int Increments => 25;
        public float ValuePercent => cur / max;

        


        public void SetTargetValuePct(float value)
        {
            targetValue = value * Max;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            yield return new SoulResourceGizmo(this);
        }

        public override void Tick()
        {
            rechargeCooldown -= 1;
            if (rechargeCooldown < 0)
            {
                if (cur != max)
                {
                    Value += max * 0.01f;
                }
                rechargeCooldown = tickRateRegen;
                refreshState++;

                if (refreshState > 10)
                {
                    if (cur < 0) cur = 0;
                    float oldMax = max;
                    max = pawn.GetStatValue(BSDefs.BS_SoulPower);
                    if (max == 0)
                    {
                        pawn.health.RemoveHediff(this);
                    }
                    else
                    {
                        if (oldMax != max)
                        {
                            Value = Value * max / oldMax;
                        }
                        refreshState = 0;
                    }
                    
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref cur, "cur", 0f);
            Scribe_Values.Look(ref max, "max", 0f);
            Scribe_Values.Look(ref targetValue, "targetValue", 0.5f);
        }
    }
}
