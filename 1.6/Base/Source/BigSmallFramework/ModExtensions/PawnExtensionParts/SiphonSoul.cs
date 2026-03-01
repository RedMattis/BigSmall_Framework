using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public enum SiphonType
    {
        None = 0,
        KillingBlow,
        ConsumeSoul,
        Influence,
        Lovin,
        Custom,
    }
    
    public record SiphonSoul
    {
        public SiphonType type;
        public float gainFactor = 0.01f;
        public float gainOffset = 0;
        
        public float gainSkill = 0;
        public float architeGeneFactor = 1f;
        public float maxXPDrain = 10000;
        public float maxXpDrainPercent = 0.2f;
        public float fromTargetSoulFactor = 1;
        public float fromTargetPsyfocusFactor = 1;
        public float fromTargetPsyfocusFactor_Max = 1.5f;
        public float targetPsyFocusOffset = -0.80f;
        public float minimumBaseGain = 0.05f;

        public string SiphonSoulDescription
        {
            get
            {
                StringBuilder builder = new();
                string typeStr = type switch {
                    SiphonType.KillingBlow => "BS_SiphonSoulOnHit",
                    SiphonType.ConsumeSoul => "BS_SiphonSoul",
                    SiphonType.Influence => "BS_SiphonInfluence",
                    SiphonType.Lovin => "BS_SiphonLovin",
                    SiphonType.Custom => "BS_SiphonCustom",
                    _ => "BS_SiphonUnknown"
                };
                builder.AppendLine(typeStr.Translate().CapitalizeFirst());
                return builder.ToString();
            }
        }

        public SiphonSoul FuseWith(SiphonSoul other) => this with
        {
            gainOffset = Mathf.Max(gainOffset, other.gainOffset),
            gainFactor = Mathf.Max(gainFactor, other.gainFactor),
            gainSkill = Mathf.Max(gainSkill, other.gainSkill),
            architeGeneFactor = Mathf.Max(architeGeneFactor, other.architeGeneFactor),
            maxXPDrain = Mathf.Max(maxXPDrain, other.maxXPDrain),
            maxXpDrainPercent = Mathf.Max(maxXpDrainPercent, other.maxXpDrainPercent),
            fromTargetSoulFactor = Mathf.Max(fromTargetSoulFactor, other.fromTargetSoulFactor),
            fromTargetPsyfocusFactor = Mathf.Max(fromTargetPsyfocusFactor, other.fromTargetPsyfocusFactor),
            fromTargetPsyfocusFactor_Max = Mathf.Max(fromTargetPsyfocusFactor_Max, other.fromTargetPsyfocusFactor_Max),
            targetPsyFocusOffset = Mathf.Max(targetPsyFocusOffset, other.targetPsyFocusOffset),
            minimumBaseGain = Mathf.Max(minimumBaseGain,other.minimumBaseGain)

        };
    }

    public static class SiphonSoulExtension
    {
        extension(IEnumerable<SiphonSoul> siphons)
        {
            public SiphonSoul FuseAll(SiphonType type)
            {
                siphons = siphons.Where(x => x.type == type);
                if (siphons.Any() == false) return null;
                SiphonSoul result = siphons.First() with { };
                bool first = true;
                foreach (var soul in siphons)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }
                    result = result.FuseWith(soul);
                }
                return result;
            }
        }
    }
}
