using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace BigAndSmall
{
    public class CompProperties_TargetableExtended : CompProperties_Targetable
    {
        public TargetingParameters targetInfo = new();
        public bool playerOwnedOnly = false;

        // Short-form:
        public bool animalsOnly = false;
        public bool humanlikeOnly = false;

        public CompProperties_TargetableExtended()
        {
            compClass = typeof(CompTargetable_Extended);
        }
    }

    public class CompTargetable_Extended : CompTargetable
    {
        public CompProperties_TargetableExtended PropsE => (CompProperties_TargetableExtended)props;

        protected override bool PlayerChoosesTarget => true;

        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
        }

        protected override TargetingParameters GetTargetingParameters()
        {
            var targetingCopy = new TargetingParameters();
            // Use reflection to copy over the parameters
            foreach (var field in typeof(TargetingParameters).GetFields())
            {
                field.SetValue(targetingCopy, field.GetValue(PropsE.targetInfo));
            }
            if (PropsE.animalsOnly)
            {
                targetingCopy.canTargetHumans = false;
                targetingCopy.canTargetSubhumans = false;
                targetingCopy.canTargetMechs = false;
                targetingCopy.canTargetBuildings = false;
            }
            else if (PropsE.humanlikeOnly)
            {
                targetingCopy.canTargetAnimals = false;
                targetingCopy.canTargetMechs = false;
                targetingCopy.canTargetBuildings = false;
            }
            return targetingCopy;
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (PropsE.playerOwnedOnly && target.Thing.Faction != Faction.OfPlayer)
            {
                if (showMessages)
                {
                    Messages.Message("CannotOrderNonControlled".Translate(), MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.ValidateTarget(target, showMessages);
        }
    }
}
