using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static RedHealth.SettingsWidgets;
using static RedHealth.RedSettings;

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
            CreateSettingCheckbox(listStd, "RED_HealthEnabledForAllPawns".Translate(), ref settings.activeOnAllPawnsByDefault);
            CreateSettingCheckbox(listStd, "RED_HealthDisableForAll".Translate(), ref settings.disableForAll);

            listStd.Label("RED_SpecificTrackers".Translate().AsTipTitle());
            foreach(var aspect in DefDatabase<HealthAspect>.AllDefsListForReading)
            {
                if (aspect != HDefs.RED_OverallHealth)
                {
                    if (settings.aspectsDisabled.Any(x => x.defName == aspect.defName) is false)
                    {
                        settings.aspectsDisabled.Add(new HealthAspectWrapper { defName = aspect.defName, active = true });
                    }
                    var aspectWrapper = settings.aspectsDisabled.First(x => x.defName == aspect.defName);
                    CreateSettingCheckbox(listStd, "  - " + aspect.LabelSimple.CapitalizeFirst(), ref aspectWrapper.active);
                }
            }


            // Check if in dev-mode
            if (Prefs.DevMode)
            {
                // Unused.
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

    public class RedSettings : ModSettings
    {
        public class HealthAspectWrapper { public string defName; public bool active; }

        public static readonly bool activeOnAllPawnsByDefaultDefault = false;
        public bool activeOnAllPawnsByDefault = activeOnAllPawnsByDefaultDefault;

        public static readonly bool disableForAllDefault = false;
        public bool disableForAll = disableForAllDefault;

        public List<HealthAspectWrapper> aspectsDisabled = [];

        public bool IsAspectDisabled(HealthAspect aspect)
        {
            return aspectsDisabled.Any(x => x.defName == aspect.defName && x.active is false);
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref activeOnAllPawnsByDefault, "activeOnAllPawnsByDefault", activeOnAllPawnsByDefaultDefault);
            Scribe_Values.Look(ref disableForAll, "disableForAll", disableForAllDefault);
            Scribe_Collections.Look(ref aspectsDisabled, "aspectsDisabled", LookMode.Value);
            base.ExposeData();
        }
    }

    
}
