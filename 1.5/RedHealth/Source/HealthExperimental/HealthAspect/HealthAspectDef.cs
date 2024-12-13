using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RedHealth
{
    public class HealthCurve : Def
    {
        public static List<HealthCurve> healthCurves = null;

        public SimpleCurve curve;
        public float weight;
        public int score = 0;

        public static List<HealthCurve> GetAllCruves()
        {
            healthCurves ??= DefDatabase<HealthCurve>.AllDefsListForReading;
            return healthCurves;
        }
    }

    public class HealthAspect : Def
    {
        private static List<HealthAspect> healthAspects = null;

        private string labelSimple = null;
        public bool addAutomatically = true;
        public StatDef oddsMultipliedByStat = null;
        public List<BodyPartTagDef> associatedPartsTags = [];
        public bool lucky = false; // Means it cannot pick the bad curves, and length will never be less than 1.

        public List<HealthThreshold> thresholds = [];

        public List<Effects> hediffs_lowRisk = [];
        public List<Effects> hediffs_mediumRisk = [];
        public List<Effects> hediffs_highRisk = [];
        public List<Effects> hediffs_justDieAlready = [];

        public List<FleshTypeDef> validFleshTypes = [];

        public bool nullifyIfPartsReplaced = true;
        public bool nullifyIfPartsReplacedBetterThanNatural = true;
        public List<HediffDef> nullifyingHediffs = [];
        public List<GeneDef> nullifyingGenes = [];
        public List<TraitDef> nullifyingTraits = [];
        public List<ThingDef> nullifyingRaces = [];

        public List<WeightedCapacity> offsetFromCapacity = [];

        public string LabelSimple => labelSimple ?? label;

        public static List<HealthAspect> GetHealthAspects()
        {
            healthAspects ??= DefDatabase<HealthAspect>.AllDefsListForReading.Where(x => x.addAutomatically).ToList();
            return healthAspects;
        }
    }

    public class HealthThreshold
    {
        public const int defaultMaxMeanTime = 480000;
        public string label;
        public string description;
        public float threshold = 0;
        public float odds = 0;  // 1 means 100%.
        public Color? labelColor = null;

        /// <summary>
        /// Time until next health even can happen, in ticks. Note that this is used as the UPPER part of a random range.
        /// 
        /// 60'000 is 1 day.
        /// 900'000 is 1 quadrum.
        /// 3'600'000 is 1 year.
        /// Never set to more than 0.5 quadrum. We still need to reevaluate the health aspects in case the pawn was aged up or had organs swapped.
        /// 
        /// Note that 480000 (0-8 days, average 4) should be fine for most pawns unless they are very old and we want to spam them with health events.
        /// </summary>
        public float maxMeanTime = defaultMaxMeanTime;
        public List<Effects> effects = [];

        public override string ToString()
        {
            return $"{label} ({threshold})";
        }

        public int GetNextEventTime()
        {
            return Rand.Range(100, (int)maxMeanTime);
        }

        public Color GetColor()
        {
            return labelColor ?? Color.white;
        }
    }

    public class Effects
    {
        public HediffDef hediff = null;
        public FloatRange? severityRange = null;
        public FloatRange? severityPerDayRange = null;
        public float weight = 1;
        public bool stackWithExisting = true;
        public bool killWorldPawn = false;
        public bool killPawn = false;
        public bool? filterIfExisting = null;
        public List<BodyPartDef> partsToAffect = [];
        public bool canAffectAnyLivePart = false;

        public bool ShouldFilterIfExisting()
        {
            if (hediff == null) return false;
            if (filterIfExisting != null) return filterIfExisting.Value;
            // Check if the Hediff uses Severity. If so default to True.
            bool usesSeverity = severityRange != null || hediff.maxSeverity < float.MaxValue || !hediff.stages.NullOrEmpty();
            return !usesSeverity;
        }

        public bool AppliesToSpecificParts()
        {
            return partsToAffect.Count > 0;
        }
    }
    public class WeightedCapacity  // 
    {
        public PawnCapacityDef capacity;
        public float weight;
        public FloatRange clamp = new(0.25f, 99);
    }
}
