using RimWorld;
using System.Collections.Generic;
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

        public string GetLabel()
        {
            if (need == null) return "";
            return need.LabelCap + (minValue ? " Min" : "");
        }
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
        }
    }
}
