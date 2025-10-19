using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Noise;
using static UnityEngine.GridBrushBase;

namespace BigAndSmall
{
    

    

    public class JobGiver_UseCharger : ThinkNode_JobGiver
    {
        private const float maxLevelPercentage = 1f;
        public override float GetPriority(Pawn pawn)
        {
            Need_Food food = pawn.needs.food;
            if (food == null)
            {
                return 0;
            }
            if (food.CurLevelPercentage >= pawn.RaceProps.FoodLevelPercentageWantEat)
            {
                return 0; 
            }
            if (pawn.GetCachePrepatched() is BSCache cache)
            {
                if (cache.canUseChargers)
                {
                    if (cache.poorUserOfChargers)
                    {
                        return 9.45f; // Prefer food instead of power if possible.
                    }
                    return 9.55f;
                }
            }
            return 0;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            bool predicate(Thing t) => t is IRobotCharger charger && pawn.CanReserve(t) && charger.PawnCanUse(pawn, isNew:true);

            Need_Food food = pawn.needs.food;
            if (food == null || food.CurLevelPercentage > maxLevelPercentage)
            {
                return null;
            }
            float searchRange = food.CurCategory switch
            {
                HungerCategory.Hungry => 24f,
                HungerCategory.UrgentlyHungry => 48f,
                HungerCategory.Starving => 99999,
                _ => 0f,
            };

            Thing recharger = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(pawn),
                maxDistance: searchRange, validator: predicate);

            if (recharger != null)
            {
                return JobMaker.MakeJob(BSDefs.BS_UseCharger, recharger);
            }
            return null;
        }
    }
}
