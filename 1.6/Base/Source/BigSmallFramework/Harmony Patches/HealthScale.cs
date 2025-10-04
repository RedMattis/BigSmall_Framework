using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(Pawn), "HealthScale", MethodType.Getter)]
    public static class Pawn_HealthScale
    {
        private readonly static Dictionary<Hediff_Injury, float> injuryRescaling = [];
        private static int lastTick = 0;
        public static void Postfix(ref float __result, Pawn __instance)
        {
            if (__instance.GetCachePrepatched() is BSCache cache && !cache.IsTempCache)
            {
                float newScale = __result * cache.healthMultiplier;
                if (!cache.injuriesRescaled)
                {
                    int thisTick = Find.TickManager.TicksGame;
                    if (thisTick != lastTick)
                    {
                        lastTick = thisTick;
                        injuryRescaling.Clear();
                    }
                    else // Make sure multiple calls doesn't result in rescaling multiple times by simply restoring the old values.
                    {
                        var injuryCache = injuryRescaling.Keys.Where(x => x.pawn == __instance).ToList();
                        for (int idx = injuryCache.Count - 1; idx >= 0; idx--)
                        {
                            Hediff_Injury injury = injuryCache[idx];
                            try
                            {
                                injury.severityInt = injuryRescaling[injury];
                            }
                            catch (Exception e)
                            {
                                Log.Error($"Error while restoring injuries: {e}\n{e.StackTrace}");
                            }
                        }
                    }

                    float oldscale = __result * cache.healthMultiplier_previous;
                    float injuryScale = newScale / oldscale;

                    var injuries = new List<Hediff_Injury>();
                    __instance.health.hediffSet.GetHediffs(ref injuries);
                    foreach (var injury in injuries)
                    {
                        float injuryScaleThis = injuryScale;
                        injuryRescaling[injury] = injury.Severity;
                        if (!injury.CanHealNaturally() && (cache.creationTick == null || cache.creationTick == BS.Tick))
                        {
                            // Scars should not be scaled at the point of creation, because of how they are created,
                            // this can result in them getting made much worse.

                            // TODO: Confirm if the abive is the the case.
                            injuryScaleThis = Math.Min(1, injuryScaleThis);
                        }
                        injury.severityInt = injury.Severity * injuryScale;
                        if (injury.Severity < 0.05f) // Delete tiny injuries.
                        {
                            injury.severityInt = 0;
                        }
                    }
                    cache.injuriesRescaled = true;
                }
                __result = newScale;
            }

        }

        /// <summary>
        /// Calculates the health based on some fudged math. Technically it should probably be the cubic change, but that makes large creatures tank antigrain warheads.
        /// </summary>
        /// <param name="scalMult"></param>
        /// <returns></returns>
        
    }

}
