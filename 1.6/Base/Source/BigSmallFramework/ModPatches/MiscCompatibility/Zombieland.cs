using BigAndSmall;
using Verse;

// This cannot be renamed or wrapped in a namespace or the Zombieland Support won't work.
public class ZombielandSupport
{
    public static bool? CanBecomeZombie(Pawn pawn)
    {
        if ((pawn.Dead && pawn.RaceProps.Humanlike) || pawn.needs != null)
        {
            var sizeCache = HumanoidPawnScaler.GetCache(pawn);
            if (sizeCache != null)
            {
                if (sizeCache.isUnliving || sizeCache.isBloodFeeder || sizeCache.willBeUndead)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static bool? AttractsZombies(Pawn pawn)
    {
        if (pawn.needs != null)
        {
            var sizeCache = HumanoidPawnScaler.GetCache(pawn);
            if (sizeCache != null)
            {
                if (sizeCache.deathlike)
                {
                    return false;
                }
            }
        }
        return true;
    }
}