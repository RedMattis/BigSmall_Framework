using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall.EventArgs
{
	public class DefSwappedEventArgs : System.EventArgs
	{
		public DefSwappedEventArgs(Pawn pawn, ThingDef newDef, ThingDef oldDef)
		{
			Pawn = pawn;
			NewDef = newDef;
			OldDef = oldDef;
		}

		public Pawn Pawn { get; }
		public ThingDef NewDef { get; }
		public ThingDef OldDef { get; }
	}
}
