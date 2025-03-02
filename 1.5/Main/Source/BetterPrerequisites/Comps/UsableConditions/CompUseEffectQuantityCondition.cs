using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
	public class CompProperties_UseConditionQuantity : CompProperties_UseEffect
	{
		public CompProperties_UseConditionQuantity()
		{
			compClass = typeof(CompUseConditionQuantity);
		}

		public int quantity = 1;
		public string failMessage = "Needs at least 1 to use.";
	}


	public class CompUseConditionQuantity : CompUseEffect
	{
		public override float OrderPriority => -100;
		public CompProperties_UseConditionQuantity Props => (CompProperties_UseConditionQuantity)props;

		public override AcceptanceReport CanBeUsedBy(Pawn p)
		{
			CompProperties_UseConditionQuantity properties = Props;
			if (parent.stackCount < properties.quantity)
			{
				return new AcceptanceReport(properties.failMessage);
			}
			return true;
		}

		public override void DoEffect(Pawn usedBy)
		{
			parent.SplitOff(Props.quantity).Destroy();
		}
	}
}
