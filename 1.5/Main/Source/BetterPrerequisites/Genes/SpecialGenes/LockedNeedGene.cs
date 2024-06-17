using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterPrerequisites
{
    public class LockedNeed : DefModExtension
    {
        public List<NeedDef> lockedNeeds;
        public float? value;
    }
        
    public class LockedNeedGene : PGene
    {
        private LockedNeed lockedNeed;
        public override void Tick()
        {
            base.Tick();
            bool tickNow;
            if (pawn == null) return;
            try
            {
                // Somehow some mods break the IsHashIntervalTick method.
                tickNow = pawn.IsHashIntervalTick(100);
            }
            catch
            {
                tickNow = Find.TickManager.TicksGame % 100 == 0;
            }
            if (ModsConfig.BiotechActive && tickNow && pawn.needs != null && Active)
            {
                lockedNeed = lockedNeed ?? def.GetModExtension<LockedNeed>();
                if (lockedNeed != null)
                {
                    foreach (var lockedNeedDef in lockedNeed.lockedNeeds)
                    {
                        float value = lockedNeed.value ?? 1f;

                        var need = pawn.needs.TryGetNeed(lockedNeedDef);

                        if (need != null)
                        {
                            need.CurLevel = need.MaxLevel * value;
                        }
                    }
                }
            }
        }
    }
}
