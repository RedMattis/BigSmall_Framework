using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public abstract class TickdownHediffComp : HediffComp
    {
        protected int tickDown = 0;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            tickDown -= delta;
            if (tickDown <= 0)
            {
                tickDown = 0;
                TickEvent();
                ResetCountdown();
            }
        }

        public abstract void TickEvent();

        public abstract void ResetCountdown();
    }
}
