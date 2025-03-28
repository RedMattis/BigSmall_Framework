﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static RedHealth.SettingsWidgets;

namespace RedHealth
{
    public partial class Main : Mod
    {
        public static RedSettings settings = null;

        private static Vector2 scrollPosition = Vector2.zero;
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            Listing_Standard listStd = new Listing_Standard();

            Rect mainRect = inRect; //.ContractedBy(2f);
            mainRect.height -= listStd.CurHeight;
            mainRect.y += listStd.CurHeight;
            Widgets.DrawBoxSolid(mainRect, Color.grey);
            Rect Border = mainRect.ContractedBy(1f);
            Widgets.DrawBoxSolid(Border, new ColorInt(42, 43, 44).ToColor);
            Rect scrollRect = Border.ContractedBy(5f);
            //rect3.y += 15f;
            //rect3.height -= 15f;
            Rect innerScrollRect = scrollRect;
            innerScrollRect.x = 0f;
            innerScrollRect.y = 0f;
            innerScrollRect.width -= 16f;
            innerScrollRect.height = 950f;
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, innerScrollRect);

            listStd.Begin(innerScrollRect.AtZero());

            listStd.GapLine();
            listStd.Label("RED_HealthSettings".Translate().AsTipTitle());
            if (!StandaloneModActive)
            {
                CreateSettingCheckbox(listStd, "RED_HealthEnabledForAllPawns".Translate(), ref settings._activeOnAllPawnsByDefault);
            }
            if (listStd.ButtonText("RED_AddTrackersToValidPawns".Translate()))
            {
                HealthScheduler.AddTrackersNow();
            }
            if (listStd.ButtonText("RED_RemoveAddTrackersToValidPawns".Translate()))
            {
                HealthScheduler.RemoveAllTrackersNow();
            }
            CreateSettingCheckbox(listStd, "RED_HideHealthTracker".Translate(), ref settings.hideHealthTracker);
            CreateSettingCheckbox(listStd, "RED_ShowPercentages".Translate(), ref settings.showPercentages);
            CreateSettingCheckbox(listStd, "RED_HealthDisableForAll".Translate(), ref settings.disableForAll);

            listStd.Label("RED_SpecificTrackers".Translate().AsTipTitle());
            foreach(var aspect in DefDatabase<HealthAspect>.AllDefsListForReading)
            {
                if (aspect != HDefs.RED_OverallHealth)
                {
                    settings.aspectsDisabled ??= [];
                    if (settings.aspectsDisabled.Any(x => x.defName == aspect.defName) is false)
                    {
                        settings.aspectsDisabled.Add(new HealthAspectWrapper { defName = aspect.defName, active = true });
                    }
                    var aspectWrapper = settings.aspectsDisabled.First(x => x.defName == aspect.defName);
                    CreateSettingCheckbox(listStd, "  - " + aspect.LabelSimple.CapitalizeFirst(), ref aspectWrapper.active);
                }
            }

            CreateSettingsSlider(listStd, "RED_DevTimeAcceleration".Translate(), ref settings.devEventTimeAcceleration, 0.1f, Prefs.DevMode ? 100000f : 10f, x => x.ToString("F1"));


            // Check if in dev-mode
            if (Prefs.DevMode)
            {
                listStd.GapLine();
                listStd.Label("RED_DevSettings".Translate().AsTipTitle());
                
                CreateSettingCheckbox(listStd, "RED_Logging".Translate(), ref settings.logging);
            }

            listStd.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RED_Health".Translate();
        }
    }

    public class HealthAspectWrapper : IExposable
    {
        public string defName; public bool active = false;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref active, "active");
        }
    }
    public class RedSettings : ModSettings
    {
        public const bool activeOnAllPawnsByDefaultDefault = false;
        public bool _activeOnAllPawnsByDefault = activeOnAllPawnsByDefaultDefault;

        public const bool hideHealthTrackerDefault = false;
        public bool hideHealthTracker = hideHealthTrackerDefault;

        public const bool disableForAllDefault = false;
        public bool disableForAll = disableForAllDefault;

        //public List<HealthAspectWrapper> aspectsHidden = [];

        public const float devEventTimeAccelerationDefault = 1f;
        public float devEventTimeAcceleration = devEventTimeAccelerationDefault;
        public bool logging = false;

        public List<HealthAspectWrapper> aspectsDisabled = [];

        public static readonly bool showPercentagesDefault = false;
        public bool showPercentages = showPercentagesDefault;

        public bool ActiveOnAllPawnsByDefault { get => Main.StandaloneModActive || _activeOnAllPawnsByDefault; set => _activeOnAllPawnsByDefault = value; }

        //public bool IsAspectDisabled(HealthAspect aspect)
        //{
        //    return aspectsDisabled.Any(x => x.defName == aspect.defName && x.active is false);
        //}


        public override void ExposeData()
        {
            Scribe_Values.Look(ref _activeOnAllPawnsByDefault, "activeOnAllPawnsByDefault", activeOnAllPawnsByDefaultDefault);
            Scribe_Values.Look(ref hideHealthTracker, "hideHealthTracker", hideHealthTrackerDefault);
            Scribe_Values.Look(ref disableForAll, "disableForAll", disableForAllDefault);
            Scribe_Values.Look(ref showPercentages, "showPercentages", showPercentagesDefault);
            Scribe_Collections.Look(ref aspectsDisabled, "aspectsDisabled", LookMode.Deep);

            Scribe_Values.Look(ref devEventTimeAcceleration, "devEventTimeAcceleration", devEventTimeAccelerationDefault);
            Scribe_Values.Look(ref logging, "logging", false);

            base.ExposeData();
        }
    }

    
}
