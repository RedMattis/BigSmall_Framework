using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public abstract class TickdownGene : Gene
    {
        protected int tickDown = 0;

        public override void TickInterval(int delta)
        {
            tickDown -= delta;
            base.TickInterval(delta);
            if (tickDown <= 0)
            {
                tickDown = 0;
                TickEvent();
                ResetCountdown();
            }
        }

        public abstract void TickEvent();

        public abstract void ResetCountdown();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickDown, "tickDown");
        }
    }
}
