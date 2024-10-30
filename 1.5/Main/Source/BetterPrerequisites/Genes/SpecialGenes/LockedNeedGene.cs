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
        // OBSOLETE!
        public List<NeedDef> lockedNeeds;
        public float? value;
    }

    public class LockedNeedClass
    {
        public NeedDef need;
        public float value;
        public bool minValue = false;
    }
        
    public class LockedNeedGene : PGene
    {
        //private LockedNeed lockedNeed;

        public override void PostAdd()
        {
            Log.Warning("LockedNeedGene is obsolete. Use a regular PGene instead anlong with the BetterPrerequisites.GeneExtension");
        }
        public override void Tick()
        {
            //base.Tick();
            //bool tickNow;
            //if (pawn == null) return;
            //try
            //{
            //    // Somehow some mods break the IsHashIntervalTick method.
            //    tickNow = pawn.IsHashIntervalTick(100);
            //}
            //catch
            //{
            //    tickNow = Find.TickManager.TicksGame % 100 == 0;
            //}
            //if (ModsConfig.BiotechActive && tickNow && pawn.needs != null && Active)
            //{
            //    lockedNeed = lockedNeed ?? def.GetModExtension<LockedNeed>();
            //    if (lockedNeed != null)
            //    {
            //        foreach (var lockedNeedDef in lockedNeed.lockedNeeds)
            //        {
            //            float value = lockedNeed.value ?? 1f;

            //            var need = pawn.needs.TryGetNeed(lockedNeedDef);

            //            if (need != null)
            //            {
            //                need.CurLevel = need.MaxLevel * value;
            //            }
            //        }
            //    }
            //}
        }
    }
}
