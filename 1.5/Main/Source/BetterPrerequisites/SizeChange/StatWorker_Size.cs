using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace BigAndSmall
{
    // Never call these without "cacheStaleAfterTicks" unless calling from the Cache Itself and trying to refresh.

    //public class StatWorker_Size : StatWorker
    //{
    //    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    //    {
    //        if (!stat.supressDisabledError && Prefs.DevMode && IsDisabledFor(req.Thing))
    //        {
    //            Log.ErrorOnce($"Attempted to calculate value for disabled stat {stat}; this is meant as a consistency check, either set the stat to neverDisabled or ensure this pawn cannot accidentally use this stat (thing={req.Thing.ToStringSafe()})", 75193282 + stat.index);
    //        }
    //        if (req.Thing is not Pawn pawn)
    //        {
    //            return 1;
    //        }
    //        float num = 1; //GetBaseValueFor(req);

    //        if (HumanoidPawnScaler.GetBSDict(pawn, canRegenerate: false) is BSCache cache)
    //        {
    //            num *= cache.scaleMultiplier.linear;
    //        }
    //        return num;
    //    }
    //}

    [StaticConstructorOnStartup]
    public class StatWorker_MaxNutritionFromSize : StatWorker
    {
        // delegate for temporaryStatCache (private Dictionary<Thing, StatCacheEntry> temporaryStatCache;)
        static public AccessTools.FieldRef<StatWorker, Dictionary<Thing, StatCacheEntry>> temporaryCacheDelegate = AccessTools.FieldRefAccess<Dictionary<Thing, StatCacheEntry>>("RimWorld.StatWorker:temporaryStatCache");
        public void SetTemporaryStatCache(Pawn pawn, float value)
        {
            if (temporaryCacheDelegate(this) is Dictionary<Thing, StatCacheEntry> tCache && !tCache.NullOrEmpty())
            {
                value = GetNutritionMultiplier(value);
                if (!tCache.ContainsKey(pawn))
                {
                    tCache[pawn] = new StatCacheEntry(value, Find.TickManager.TicksGame);
                }
                else
                {
                    var entry = tCache[pawn];
                    entry.statValue = value;
                    entry.gameTick = Find.TickManager.TicksGame;
                }
            }
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (!stat.supressDisabledError && Prefs.DevMode && IsDisabledFor(req.Thing))
            {
                Log.ErrorOnce($"Attempted to calculate value for disabled stat {stat}; this is meant as a consistency check, either set the stat to neverDisabled or ensure this pawn cannot accidentally use this stat (thing={req.Thing.ToStringSafe()})", 75193282 + stat.index);
            }
            float nutritionCapMult = 1; //GetBaseValueFor(req);  // This should be 1.
            if (req.Thing is Pawn pawn)
            {
                if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
                {
                    if (cache.developmentalStage <= DevelopmentalStage.Baby) return 1;
                    float scale = cache.scaleMultiplier.linear;
                    return GetNutritionMultiplier(scale);
                    //Log.Message($"Debug : {pawn} StatWorker_MaxNutritionFromSize: nutritionCapMult: {nutritionCapMult}");
                }
            }
            return nutritionCapMult;
        }

        private static float GetNutritionMultiplier(float scale)
        {
            float nutritionCapMult = 1;
            if (scale > 1f)
            {
                scale = Mathf.Clamp01((scale - 1) / 3);
                nutritionCapMult *= Mathf.Lerp(1, 3f, scale);
            }
            else if (scale < 1f) // Don't shrink the food bar too much or they will waste an unreasonably large amount of food from meals.
            {
                nutritionCapMult = (nutritionCapMult / scale + 1f) / 2;
            }
            return nutritionCapMult;
        }

        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess) { }
    }

    public class StatPart_MaxNutritionFromSize : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Pawn pawn)
            {
                val *= pawn.GetStatValue(BSDefs.BS_MaxNutritionFromSize, cacheStaleAfterTicks:int.MaxValue);
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.Thing is Pawn pawn)
            {
                float nutritionFromBodySize = pawn.GetStatValue(BSDefs.BS_MaxNutritionFromSize, cacheStaleAfterTicks: int.MaxValue);
                if (nutritionFromBodySize != 1)
                {
                    return "BS_StatsReport_BodySize".Translate(nutritionFromBodySize.ToString("F2")) + ": x" + nutritionFromBodySize.ToStringPercent();
                }
            }
            return null;
        }
    }
}
