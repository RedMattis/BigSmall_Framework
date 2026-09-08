using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall;

public static class AgeTrackerExtensions
{
    extension(Pawn pawn)
    {
        public bool IsTeenOrAdult()
        {
            return pawn?.DevelopmentalStage == null || pawn.DevelopmentalStage > DevelopmentalStage.Child;
        }

        public bool IsActuallyAdult()
        {
            if (!pawn.IsTeenOrAdult())
                return false;
            if (pawn?.ageTracker?.CurLifeStage == BSDefs.HumanlikeTeenager)
            {
                return false;
            }
            return true;
        }
    }
}