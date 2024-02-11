using BetterPrerequisites;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (req.HasThing && req.Thing.Spawned)
            {
                return req.Thing.Map.skyManager.CurSkyGlow < 0.3f; //req.Thing.Position.InSunlight(req.Thing.Map);
            }
            return false;
        }
    }

    public class ConditionalStatAffecter_Warm : ConditionalStatAffecter
    {
        public override string Label => "BS_StatsReport_Warm".Translate();

        public override bool Applies(StatRequest req)
        {
            if (!ModsConfig.BiotechActive)
            {
                return false;
            }
            if (req.HasThing && req.Thing.Spawned && req.Thing is Pawn pawn)
            {
                if (FastAcccess.GetCache(pawn) is BSCache cache)
                {
                    if (cache.alcoholmAmount > 0.0f)
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
            if (!ModsConfig.BiotechActive)
            {
                return false;
            }
            if (req.HasThing && req.Thing.Spawned && req.Thing is Pawn pawn)
            {
                if (FastAcccess.GetCache(pawn) is BSCache cache)
                {
                    if (cache.alcoholmAmount >= 0.25f)
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
            if (!ModsConfig.BiotechActive)
            {
                return false;
            }
            if (req.HasThing && req.Thing.Spawned && req.Thing is Pawn pawn)
            {
                if (FastAcccess.GetCache(pawn) is BSCache cache)
                {
                    if (cache.alcoholmAmount >= 0.4f)
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
        public virtual float SensitivityThreshold => 2.0f;

        public override bool Applies(StatRequest req)
        {
            if (!ModsConfig.BiotechActive)
            {
                return false;
            }
            if (req.HasThing && req.Thing.Spawned && req.Thing is Pawn pawn)
            {
                if (FastAcccess.GetCache(pawn) is BSCache cache)
                {
                    if (cache.psychicSensitivity >= SensitivityThreshold)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class ConditionalStatAffecter_PsychicSensitivityVeryHigh : ConditionalStatAffecter_PsychicSensitivityHigh
    {
        public override string Label => "PsychicSensitivity_VeryHigh".Translate();
        public override float SensitivityThreshold => 3.0f;
    }

    public class ConditionalStatAffecter_PsychicSensitivityNormal : ConditionalStatAffecter_PsychicSensitivityHigh
    {
        public override string Label => "PsychicSensitivity_Normal".Translate();
        public override float SensitivityThreshold => 0.90f;
    }
}
