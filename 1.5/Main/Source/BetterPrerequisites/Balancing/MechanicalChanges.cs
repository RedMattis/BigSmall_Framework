using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.Noise;
using Verse;
using RimWorld;
using UnityEngine;
using System.Reflection.Emit;
using System.Reflection;

namespace BigAndSmall
{
    // Might be causing pawns to constantly regenerate health under some circumstances due to some rounding error or modmismatch.
    // Investigate and restore once fixed.
    [HarmonyPatch(typeof(Pawn), "HealthScale", MethodType.Getter)]
    public static class Pawn_HealthScale
    {
        private readonly static Dictionary<Hediff_Injury, float> injuryRescaling = [];
        private static int lastTick = 0;
        static FieldInfo severityIntField = null;

        public static void SetSeverityNoEvent(this Hediff injury, float value)
        {
            if (severityIntField == null)
            {
                severityIntField = typeof(Hediff).GetField("severityInt", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            severityIntField.SetValue(injury, value);
        }
        public static void Postfix(ref float __result, Pawn __instance)
        {
            int thisTick = Find.TickManager.TicksGame;
            if (FastAcccess.GetCache(__instance) is BSCache sizeCache)
            {
                float newScale = __result * sizeCache.healthMultiplier;
                if (!sizeCache.injuriesRescaled)
                {
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
                                injury.SetSeverityNoEvent(injuryRescaling[injury]);
                            }
                            catch (Exception e)
                            {
                                Log.Error($"Error while restoring injuries: {e}");
                            }
                        }

                    }

                    float oldscale = __result * sizeCache.healthMultiplier_previous;
                    // Scale all injuries by the difference between the old and new health multipliers.
                    // This is meant to make sure a pawns injuries don't get more severe if they shrink.
                    // It shouldn't really be needed, but shrinking pawns seem to make their injuries get worse,
                    // Which can result in Nisses dying after spawning with scars.
                    float injuryScale = newScale / oldscale;

                    // Don't scale up injuries. Or at least test it carefully first. It can cause strange results.
                    // I think the growth ray ticking down might also kill a pawn if we don't refresh at each Hediff stage
                    // which would be a pain.
                    //injuryScale = Mathf.Min(1, injuryScale);

                    var injuries = new List<Hediff_Injury>();
                    __instance.health.hediffSet.GetHediffs(ref injuries);
                    foreach (var injury in injuries)
                    {
                        injuryRescaling[injury] = injury.Severity;
                        injury.SetSeverityNoEvent(injury.Severity * injuryScale);
                        if (injury.Severity < 0.05f) // Delete tiny injuries.
                        {
                            injury.SetSeverityNoEvent(0);
                        }
                    }
                    sizeCache.injuriesRescaled = true;
                }
                lastTick = thisTick;
                __result = newScale;
            }

        }

        /// <summary>
        /// Calculates the health based on some fudged math. Technically it should probably be the cubic change, but that makes large creatures tank antigrain warheads.
        /// </summary>
        /// <param name="scalMult"></param>
        /// <returns></returns>
        
    }

    [HarmonyPatch(typeof(Need_Food), nameof(Need_Food.FoodFallPerTickAssumingCategory))]
    public static class Need_Food_FoodFallPerTickAssumingCategory
    {
        public static void Prefix(ref Pawn ___pawn, out float __state)
        {
            __state = ___pawn.def.race.baseHungerRate;
            if (FastAcccess.GetCache(___pawn) is BSCache sizeCache && ___pawn.DevelopmentalStage > DevelopmentalStage.Baby)
            {
                float hungerRate = __state * Mathf.Max(sizeCache.scaleMultiplier.linear, sizeCache.scaleMultiplier.DoubleMaxLinear);
                float finalHungerRate = Mathf.Lerp(__state, hungerRate, BigSmallMod.settings.hungerRate);

                ___pawn.def.race.baseHungerRate = finalHungerRate;
            }
        }

        public static void Postfix(ref float __result, Pawn ___pawn, float __state)
        {
            ___pawn.def.race.baseHungerRate = __state;
        }
    }



    [HarmonyPatch(typeof(Verb_MeleeAttack), "GetDodgeChance")]
    public static class VerbMeleeAttack_GetDodgeChance
    {
        public static void Postfix(ref float __result, LocalTargetInfo target)
        {
            if (target.Thing is Pawn pawn && __result < 0.99f && HumanoidPawnScaler.GetBSDict(pawn) is BSCache sizeCache)
            {
                __result /= sizeCache.scaleMultiplier.linear;
                if (__result >= 0.96)
                    __result = 0.96f;
            }
        }
    }

}
