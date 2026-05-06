using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    /// <summary>
    ///  Basically improved version of auto-clotting.
    /// </summary>
    public class Gene_AutoTending : Gene
    {
        private static readonly FloatRange TendingQualityRange = new(0.35f, 0.75f);
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!pawn.IsHashIntervalTick(360, delta))
            {
                return;
            }
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int num = hediffs.Count - 1; num >= 0; num--)
            {
                var hediff = hediffs[num];
                if (hediff.Bleeding || (hediff.def.tendable && hediff is Hediff_Injury && hediff.TendableNow()))
                {
                    hediffs[num].Tended(TendingQualityRange.RandomInRange, TendingQualityRange.TrueMax, 1);
                }
            }
        }
    }
}
