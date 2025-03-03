﻿using HarmonyLib;
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
    public class HealthManager : Hediff
    {
        public static HealthManager defaultCache = new();
        public bool firstTimeSetup = true;

        public List<HealthEvent> queuedHealthEvents = [];

        public HealthAspectTracker overalHealth = null;
        public List<HealthAspectTracker> healthAspects = [];
        public float lifeSpanMultiplier = 1.25f;

        public override void PostAdd(DamageInfo? dinfo)
        {
            if (Main.loggingV) Log.Message("HealthManager - PostAdd");
            if (firstTimeSetup)
            {
                lifeSpanMultiplier = Rand.Range(1.0f, 1.5f);
                if (Main.DoCheapLogging)
                {
                    try
                    {
                        Log.Message($"--------------------------\n" +
                        $"Setup for {pawn?.def?.LabelCap} {pawn}.\n" +
                        $"They are {pawn?.ageTracker?.AgeBiologicalYearsFloat:f1} years a maximum expected " +
                        $"({pawn.RaceProps?.AnyPawnKind?.RaceProps?.lifeExpectancy}*{pawn.GetStatValue(StatDefOf.LifespanFactor):f1}).\n" +
                        $"They are at {GetAgePercentOfLifeMax()*100:f0}% of the expected lifespan when adjusted by their random scaling");
                    }
                    catch (Exception e)
                    {
                        Log.Message($"Failed to print debug message for {pawn}. Possibly attempts to check a property their race lacks.\n{e}");
                    }
                }

                firstTimeSetup = false;
                // At most 120. But due to how the curves work they might well die long before that even if they roll 120.

                overalHealth = new HealthAspectTracker(HDefs.RED_OverallHealth, true);
                var overall = GetOverallHealthLoss();
                if (!overall.HasValue) return;
                foreach (var def in HealthAspect.GetHealthAspects())
                {
                    var tracker = new HealthAspectTracker(def, false);
                    healthAspects.Add(tracker);
                    var threshold = tracker.GetThresholdFromScore(pawn, overall.Value);
                    int nextEventTime = threshold.GetNextEventTime();
                    QueueHealthEvent(def.defName, nextEventTime);

                    if (Main.loggingV) Log.Message($"Added {def.defName}");
                    //if (Main.debug) tracker.HealthEvent(pawn, overalRating.Value, forceEvent:true);
                }
            }
        }

        public override string Label
        {
            get
            {
                return ToStateString(GetAgePercentOfLifeMax(), overalHealth, parenthesis: true);
                //return $"{overalHealth.def.LabelCap} ({GetOveralThreshold().label}, {GetHealthPercentString()})";
            }
        }

        public override Color LabelColor
        {
            get
            {
                return GetOveralThreshold().GetColor();
            }
        }

        private static StringBuilder tipSb = new();
        public override string Description
        {
            get
            {
                return "";
            }
        }

        private string ToStateString(float? healthRating, HealthAspectTracker aspect, bool parenthesis)
        {
            healthRating ??= 0;
            float score = aspect.GetScore(pawn, healthRating.Value) ?? 0;
            var threshold = aspect.GetThreshold(score);
            string innerText;
            if (Main.settings.showPercentages) // Prefs.DevMode
            {
                innerText = $"{threshold.label}, {aspect.AsHealthDisplay(score)}".Colorize(threshold.GetColor());
            }
            else
            {
                innerText = $"{threshold.label}".Colorize(threshold.GetColor());
            }
            string aspectString;
            if (parenthesis)
            {
                aspectString = $"{aspect.def.LabelCap} ({innerText})";
            }
            else
            {
                aspectString = $"{aspect.def.LabelCap}: {innerText}";
            }

            return aspectString;
        }

        public override string GetTooltip(Pawn pawn, bool showHediffsDebugInfo)
        {
            tipSb.Clear();
            if (!LabelCap.NullOrEmpty())
            {
                tipSb.AppendTagged(LabelCap.Colorize(ColoredText.TipSectionTitleColor));
            }

            string severityLabel = SeverityLabel;
            if (!severityLabel.NullOrEmpty())
            {
                tipSb.Append(": ").Append(severityLabel);
            }

            tipSb.AppendLine();
            var healthRating = GetOverallHealthLoss();
            if (healthRating != null)
            {
                var threshold = GetOveralThreshold();

                tipSb.AppendLine(threshold.description);

                // Maybe make the ability to view these dependent on research?
                tipSb.AppendLine();
                foreach (var aspect in healthAspects)
                {
                    var aspectRating = aspect.GetScore(pawn, healthRating.Value);
                    if (aspectRating == null) continue;
                    string aspectString = ToStateString(healthRating, aspect, parenthesis: false);
                    tipSb.AppendLine(aspectString);
                }

                if (Main.settings.logging)
                {
                    tipSb.AppendLine();
                    tipSb.AppendLine("DEBUG VARS");
                    tipSb.AppendLine($"PercentOfLife Input: {GetAgePercentOfLifeMax() * 100:f1}%");
                    tipSb.AppendLine($"Main Health Curve: ({overalHealth.curve.Join(x => x.y.ToString(), ", ")})");
                }
            }


            return tipSb.ToString().TrimEnd();
        }

        public override bool Visible => !Main.settings.hideHealthTracker;

        public float GetAgePercentOfLifeMax()
        {
            float pawnAge = pawn.ageTracker.AgeBiologicalYearsFloat;
            float averageLifeSpan = pawn.RaceProps.lifeExpectancy * pawn.GetStatValue(StatDefOf.LifespanFactor);
            return pawnAge / (averageLifeSpan * lifeSpanMultiplier);
        }

        /// <summary>
        /// Note that this is more a rating of health ISSUES. So a low rating means the pawn is healthy.
        /// 
        /// The reason for this is to avoid negative values for really low health.
        /// </summary>
        public float? GetOverallHealthLoss()
        {
            var result = overalHealth.GetScore(pawn, GetAgePercentOfLifeMax());
            if (result == null) return null;
            // Max is 120%, which if reduced due to modifiers or e.g. parts replaced with bionics (validPartsPercent)
            return Mathf.Clamp(result.Value, 0, 1.2f);
        }

        public HealthThreshold GetOveralThreshold()
        {
            return overalHealth.GetThreshold(GetOverallHealthLoss() ?? -2);
        }

        public float GetPawnAgingRate() // The biologicalAgeTickFactorFromAgeCurve
        {
            float biologicalAgeTickFactor = pawn.genes?.BiologicalAgeTickFactor ?? 1;
            float ageFactorComparedToHuman = pawn.RaceProps.lifeExpectancy / 80f;
            return biologicalAgeTickFactor * ageFactorComparedToHuman;
        }

        public void QueueHealthEvent(string name, int time)
        {
            const int margin = 10; // Just so we don't have to worry about order of events.
            
            float timeAcceleration = Main.settings.devEventTimeAcceleration;
            if (timeAcceleration > 1.2f || timeAcceleration < 0.8f)
            {
                int newTime = (int)(time / timeAcceleration);
                if (Main.loggingV) Log.Message($"[DEBUG] Reducing queue time for {pawn}'s {name} from {time} to {newTime}.\nChange: {time / (float)60000:f1} days to {newTime / (float)2500:f2} hours");
                time = newTime;
            }
            int totalTime = Find.TickManager.TicksGame + time + margin;
            var newEvent = new HealthEvent(this, name, totalTime);

            if (!HealthScheduler.instance.schedule.ContainsKey(totalTime))
            {
                HealthScheduler.instance.schedule[totalTime] = [];
            }
            else
            {
                if (Main.loggingV) Log.Message($"Adding another event named {name} to existing time {totalTime}");
            }
            if (Main.loggingV) Log.Message($"Next possible {name} event in {totalTime / (float)60000:f1} days ({Find.TickManager.TicksGame}/{totalTime})");

            // Remove all previous events with the same name, then add the current one.
            queuedHealthEvents.RemoveAll(x => x.name == name);
            queuedHealthEvents.Add(newEvent);

            HealthScheduler.instance.schedule[totalTime].Add(newEvent);
        }

        public void DoHealthEvent(string trackerName)
        {
            if (Main.loggingV) Log.Message($"-----------------------------------\n {pawn}'s {trackerName} Health Event");


            // Get the tracker that corresponds to the event
            var tracker = healthAspects.FirstOrDefault(x => x.def.defName == trackerName);
            if (tracker == null) { Log.Error($"Tracker not found for {trackerName}"); return; }

            float? overalHealthValue = GetOverallHealthLoss();

            int nextEventTime = HealthThreshold.defaultMaxMeanTime;
            if (overalHealthValue.HasValue)
            {
                nextEventTime = tracker.HealthEvent(pawn, overalHealthValue);
                nextEventTime = (int)(nextEventTime * GetPawnAgingRate());
                if (Main.loggingV) Log.Message($"Next possible {trackerName} event in {nextEventTime / 60000:f1} days");
            }
            QueueHealthEvent(trackerName, nextEventTime);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref firstTimeSetup, "firstTimeSetup");
            Scribe_Collections.Look(ref queuedHealthEvents, "queuedHealthEvents", LookMode.Deep);
            Scribe_Deep.Look(ref overalHealth, "overalHealth");
            Scribe_Collections.Look(ref healthAspects, "healthAspects", LookMode.Deep);
            Scribe_Values.Look(ref lifeSpanMultiplier, "lifeSpanMultiplier");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Repopulate with the correct instance, since the instance itself is not scribed.
                var qhe = queuedHealthEvents.Select(x => x).ToList();
                queuedHealthEvents.Clear();
                qhe.ForEach(x => QueueHealthEvent(x.name, x.time));
            }
        }
    }
}
