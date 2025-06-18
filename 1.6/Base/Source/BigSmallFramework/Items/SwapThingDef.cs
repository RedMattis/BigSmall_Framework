using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_SwapThingDef : CompProperties
    {
        public bool sapientVersion = false;
        public ThingDef target;

        public CompProperties_SwapThingDef()
        {
            compClass = typeof(CompUseEffect_SwapThingDef);
        }
    }

    public class CompUseEffect_SwapThingDef : CompTargetEffect
    {
        public CompProperties_SwapThingDef Props => (CompProperties_SwapThingDef)props;

        public override void DoEffectOn(Pawn _, Thing target)
        {
            if (target is Pawn pawn)
            {
                if (Props.sapientVersion)
                {
                    if (HumanlikeAnimalGenerator.reverseLookupHumanlikeAnimals.ContainsKey(target.def))
                    {
                        RaceMorpher.SwapAnimalToSapientVersion(pawn);
                    }
                    else Log.Warning($"Tried to swap {pawn.Name} to a sapient version, but no sapient version found for {target.def.defName}.");
                }
                else if (target != null) RaceMorpher.SwapThingDef(pawn, Props.target, true, 999999, force: true);
                else throw new ArgumentNullException(nameof(target), "No valid swap target specified.");
            }
        }
    }

}