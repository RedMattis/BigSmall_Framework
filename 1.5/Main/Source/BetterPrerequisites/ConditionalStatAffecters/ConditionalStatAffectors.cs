using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BigAndSmall
{
    public class ConditionalStatAffecter_AtNight : ConditionalStatAffecter
    {
        public override string Label => "StatsReport_AtNight".Translate();

        public override bool Applies(StatRequest req)
        {
            if (!ModsConfig.BiotechActive)
            {
                return false;
            }
            bool spawned = req.Thing?.Spawned == true;
            if (req.HasThing && (spawned || (req.Thing.ParentHolder is PawnFlyer pf && pf.Spawned)))
            {
                Thing thing = req.Thing;
                if (!spawned)
                {
                    thing = req.Thing.ParentHolder as PawnFlyer;
                }
                return thing.Map.skyManager.CurSkyGlow < 0.3f;
            }
            return false;
        }
    }

    public class ConditionalStatAffecter_Warm : ConditionalStatAffecter
    {
        public override string Label => "BS_StatsReport_Warm".Translate();

        public override bool Applies(StatRequest req)
        {
            if (req.Thing?.Spawned == true && req.Thing is Pawn pawn)
            {
                if (FastAcccess.GetCache(pawn) is BSCache cache)
                {
                    if (cache.alcoholAmount > 0.0f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class ConditionalStatAffecter_Tipsy : ConditionalStatAffecter
    {
        public override string Label => "BS_StatsReport_Tipsy".Translate();

        public override bool Applies(StatRequest req)
        {
            if (req.Thing?.Spawned == true && req.Thing is Pawn pawn)
            {
                if (FastAcccess.GetCache(pawn) is BSCache cache)
                {
                    if (cache.alcoholAmount >= 0.25f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class ConditionalStatAffecter_Drunk : ConditionalStatAffecter
    {
        public override string Label => "BS_StatsReport_Drunk".Translate();

        public override bool Applies(StatRequest req)
        {
            if (req.Thing?.Spawned == true && req.Thing is Pawn pawn)
            {
                if (FastAcccess.GetCache(pawn) is BSCache cache)
                {
                    if (cache.alcoholAmount >= 0.4f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class ConditionalStatAffecter_PsychicSensitivityHigh : ConditionalStatAffecter
    {
        public override string Label => "PsychicSensitivity_High".Translate();
        public virtual float SensitivityThreshold => 1.59f;

        public override bool Applies(StatRequest req)
        {
            if (req.Thing?.Spawned == true && req.Thing is Pawn pawn)
            {
                
                if (pawn.GetStatValue(StatDefOf.PsychicSensitivity, cacheStaleAfterTicks:10000) >= SensitivityThreshold)
                {
                    return true;
                }
                
            }
            return false;
        }
    }

    public class ConditionalStatAffecter_PsychicSensitivityVeryHigh : ConditionalStatAffecter_PsychicSensitivityHigh
    {
        public override string Label => "PsychicSensitivity_VeryHigh".Translate();
        public override float SensitivityThreshold => 2.24f;
    }

    public class ConditionalStatAffecter_PsychicSensitivityNormal : ConditionalStatAffecter_PsychicSensitivityHigh
    {
        public override string Label => "PsychicSensitivity_Normal".Translate();
        public override float SensitivityThreshold => 0.90f;
    }
}
