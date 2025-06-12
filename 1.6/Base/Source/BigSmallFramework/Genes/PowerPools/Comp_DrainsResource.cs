using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_DrainResource : HediffCompProperties
    {
        public const float drainAmount = 0.01f;

        public bool removeOnZero = false;
        public int ticksBetweenDrain = 1000;
        public bool canCancel = false;
        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            yield break;
        }
    }

    public abstract class Comp_DrainsResource : HediffComp
    {
        public CompProperties_DrainResource Props => (CompProperties_DrainResource)props;
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Find.TickManager.TicksGame % Props.ticksBetweenDrain == 0)
            {
                DrainResource();
            }
        }
        protected abstract void DrainResource();

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Props.canCancel)
            {
                yield return new Command_Action
                {
                    defaultLabel = "BS_StopSomething".Translate(parent.LabelCap),
                    defaultDesc = "BS_StopActiveDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                    action = delegate
                    {
                        parent.Severity = 0;
                    }
                };
            }
        }
    }
}
