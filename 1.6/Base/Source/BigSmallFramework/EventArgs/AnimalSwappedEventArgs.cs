using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall.EventArgs
{
	public class AnimalSwappedEventArgs : System.EventArgs
	{
		public AnimalSwappedEventArgs(Pawn originalPawn, Pawn newPawn)
		{
			OriginalPawn = originalPawn;
			NewPawn = newPawn;
		}

		public Pawn OriginalPawn { get; }
		public Pawn NewPawn { get; }
	}
}
