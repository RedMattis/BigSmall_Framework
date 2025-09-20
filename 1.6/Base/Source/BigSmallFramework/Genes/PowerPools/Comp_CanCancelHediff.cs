using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_CanCancelHediff : HediffCompProperties
    {
        public string iconPath = "UI/Designators/Cancel";
        public CompProperties_CanCancelHediff()
        {
            compClass = typeof(Comp_CanCancelHediff);
        }
    }

    public class Comp_CanCancelHediff: HediffComp
    {
        public CompProperties_CanCancelHediff Props => (CompProperties_CanCancelHediff)props;
        public Texture2D Icon => field ??= ContentFinder<Texture2D>.Get(Props.iconPath);
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            yield return new Command_Action
            {
                defaultLabel = "BS_StopSomething".Translate(parent.LabelCap),
                defaultDesc = "BS_StopActiveDesc".Translate(),
                icon = Icon,
                groupable = true,
                groupKey = 43214 + parent.def.GetHashCode(),
                action = delegate
                {
                    Pawn?.health?.RemoveHediff(parent);
                }
            };
        }
    }
}
