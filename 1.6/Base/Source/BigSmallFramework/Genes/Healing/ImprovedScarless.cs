using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class Gene_FastHealing : TickdownGene
    {
        public override void ResetCountdown()
        {
            tickDown = Rand.Range(30000, 90000);
        }

        public override void TickEvent()
        {
            HealthHelpers.CureWorstInjury(pawn);
        }
    }

    public class Gene_SelfRestoration : TickdownGene
    {
        public override void ResetCountdown()
        {
            tickDown = Rand.Range(30000, 90000);
        }

        public override void TickEvent()
        {
            HealthUtility.FixWorstHealthCondition(pawn);
        }
    }
}
