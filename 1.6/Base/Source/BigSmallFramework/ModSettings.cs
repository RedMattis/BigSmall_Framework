using BigAndSmall.Utilities;
using HarmonyLib;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using UnityEngine;
using Verse;
using static BigAndSmall.SettingsWidgets;

namespace BigAndSmall
{
    public class BigSmallMod : Mod
    {
        public static BSSettings settings = null;

        public BigSmallMod(ModContentPack content) : base(content)
        {
            settings ??= GetSettings<BSSettings>();
			DebugLog.Message("Initialising...");

			BSCacheExtensions.prepatched = ModsConfig.IsActive("zetrith.prepatcher");
			DebugLog.Message("Initialisation finished.");
		}

        private static Vector2 scrollPosition = Vector2.zero;
        private int selectedTab = 0;

        private static readonly string[] tabKeys = new[] // "BS_GameMechanics"
        {
            "BS_General", "BS_Races", "BS_Size", "BS_AutoCombat", "BS_Extras", "BS_Advanced", "BS_Developer"
        };
        
        private static float columnWidth = 100;
        private const float scrollAreaWidthMod = 20f;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            const float contentHeight = 30f;
            const float tabHeight = 35f;
            Rect tabRect = new(inRect.x, inRect.y + tabHeight, inRect.width, tabHeight);
            Rect contentRect = new( inRect.x, inRect.y + contentHeight, inRect.width, inRect.height - contentHeight);

            Widgets.DrawMenuSection(contentRect);

            // Tab stuff
            int tabCount = tabKeys.Length;
            var tabs = new List<TabRecord>();
            for (int i = 0; i < tabCount; i++)
            {
                int tabIndex = i;
                tabs.Add(new TabRecord(tabKeys[i].Translate(), () => selectedTab = tabIndex, selectedTab == tabIndex));
            }
            TabDrawer.DrawTabs(tabRect, tabs);

            // Content
            Rect innerRect = contentRect.ContractedBy(15f);
            columnWidth = innerRect.width - scrollAreaWidthMod;

            switch (selectedTab)
            {
                case 0: DrawGeneralTab(innerRect); break;
                case 1: DrawRacesTab(innerRect); break;
                case 2: DrawSizeTab(innerRect); break;
                //case 3: DrawGameMechanicsTab(innerRect); break;
                case 3: DrawAutoCombat(innerRect); break;
                case 4: DrawExtrasTab(innerRect); break;
                case 5: DrawAdvancedTab(innerRect); break;
                case 6: DrawDeveloperTab(innerRect); break;
            }
        }

        private void BeginScrollArea(Rect inRect, ref Vector2 scrollPos, out Rect viewRect, float height = 600f)
        {
            Rect scrollRect = inRect;
            viewRect = new Rect(0f, 0f, scrollRect.width - scrollAreaWidthMod, height);
            Widgets.BeginScrollView(scrollRect, ref scrollPos, viewRect);
        }
        private void EndScrollArea()
        {
            Widgets.EndScrollView();
        }

        private void DrawGeneralTab(Rect inRect)
        {
            Listing_Standard listStd = new()
            {
                ColumnWidth = columnWidth
            };
            BeginScrollArea(inRect, ref scrollPosition, out Rect viewRect, 600f);
            listStd.Begin(viewRect);

            if (listStd.ButtonText("BS_ResetCache".Translate()))
            {
                var pawns = HumanoidPawnScaler.Cache.Keys.Select(x => x).ToList();
                BigAndSmallCache.ScribedCache = [];
                BigAndSmallCache.refreshQueue.Clear();
                BigAndSmallCache.queuedJobs.Clear();
                BigAndSmallCache.schedulePostUpdate.Clear();
                BigAndSmallCache.scheduleFullUpdate.Clear();
                HumanoidPawnScaler.Cache = new ConcurrentDictionary<Pawn, BSCache>();
                Log.Message($"Reset Cache. Updating cache for {pawns.Count} pawns.");
                foreach (var pawn in pawns.Where(x => x != null && !x.Discarded && !x.Destroyed))
                {
                    if (HumanoidPawnScaler.GetCache(pawn, forceRefresh: true, canRegenerate: true) is BSCache cache)
                    {
                        Log.Message($"Big and Small: Reset cache for {pawn}");
                    }
                }
            }
            if (listStd.ButtonText("BS_ResetSettings".Translate()))
            {
                settings.ResetToDefault();
            }
            if (listStd.ButtonText("BS_ResetToRecommendedSettings".Translate()))
            {
                settings.ResetToRecommended();
            }
            listStd.GapLine();
            listStd.Label("BS_GameMechanics".Translate().AsTipTitle());
            listStd.GapLine();
            CreateSettingCheckbox(listStd, "BS_PreventUndead".Translate(), ref settings.preventUndead);
            CreateSettingsSlider(listStd, "BS_InflitratorChance".Translate(), ref settings.inflitratorChance, 0f, 1f, (f) => $"{f * 100:F1}%");
            CreateSettingsSlider(listStd, "BS_InflitratorRaidChance".Translate(), ref settings.inflitratorRaidChance, 0f, 1f, (f) => $"{f * 100:F1}%");
            listStd.GapLine();
            CreateSettingsSlider(listStd, "BS_ImmortalReturnFactor".Translate(), ref settings.immortalReturnTimeFactor, 0.01f, 5f, (f) => $"{f * 100:F1}%");
            listStd.GapLine();
            CreateSettingsSlider(listStd, "BS_SoulPowerFalloffOffset".Translate(), ref settings.soulPowerFalloffOffset, 0, 20f, (f) => $"{f:F1}");
            CreateSettingsSlider(listStd, "BS_SoulPowerGainMultiplier".Translate(), ref settings.soulPowerGainMultiplier, 0.5f, 5f, (f) => $"{f * 100:F1}%");

            listStd.End();
            EndScrollArea();
        }

        private void DrawRacesTab(Rect inRect)
        {
            Listing_Standard listStd = new()
            {
                ColumnWidth = columnWidth
            };
            BeginScrollArea(inRect, ref scrollPosition, out Rect viewRect, 600f);
            listStd.Begin(viewRect);

            listStd.Label("BS_ToggleFeatures".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_Surgery".Translate(), ref settings.surgeryAndBionics);

            //listStd.Label("BS_GameMechanics".Translate().AsTipTitle());
            listStd.GapLine();
            listStd.Label("BS_SapientSettings".Translate().AsTipTitle());
            if (BigSmall.BSSapientAnimalsActive_ForcedByMods)
            {
                CreateSettingCheckbox(listStd, "BS_SapientAnimals_Forced".Translate(), ref settings.forcedOn, disabled: true);
            }
            else
            {
                CreateSettingCheckbox(listStd, "BS_SapientAnimals".Translate(), ref settings.sapientAnimals);
            }
            //CreateSettingsSlider(listStd, "BS_SapientAnimalsChance".Translate(), ref settings.sapientAnimalsChance, 0f, 1f, (f) => $"{f * 100:F1}%");
            CreateSettingCheckbox(listStd, "BS_AnimalsNoSkillPenalty".Translate(), ref settings.animalsLowSkillPenalty);
            CreateSettingCheckbox(listStd, "BS_AllAnimalsHaveHands".Translate(), ref settings.allAnimalsHaveHands);
            CreateSettingCheckbox(listStd, "BS_SapientAnimalsCanRomanceAnySapientAnimals".Translate(), ref settings.animalOnAnimal);
            CreateSettingCheckbox(listStd, "BS_SapientMechanoids".Translate(), ref settings.sapientMechanoids);

            listStd.End();
            EndScrollArea();
        }

        private void DrawSizeTab(Rect inRect)
        {
            Listing_Standard listStd = new()
            {
                ColumnWidth = columnWidth
            };
            BeginScrollArea(inRect, ref scrollPosition, out Rect viewRect, 700f);
            listStd.Begin(viewRect);

            
            CreateSettingCheckbox(listStd, "BS_ScaleAnimals".Translate(), ref settings.scaleAnimals);
            listStd.GapLine();
            listStd.Label("BS_LowestUsed".Translate());

            CreateSettingsSlider(listStd, "BS_MultDamageExplain".Translate(), ref settings.dmgExponent, min: 0, max: 2, valueFormatter: (f) => $"{f * 100:F2}%");
            CreateSettingsSlider(listStd, "BS_FlatDMGExplain".Translate(), ref settings.flatDamageIncrease, 1f, 20f, (f) => $"{f:F0}");

            listStd.GapLine();
            CreateSettingsSlider(listStd, "BS_HungerMultiplierField".Translate(), ref settings.hungerRate, 0f, 1, (f) => $"{f * 100:F0}%");
            listStd.GapLine();

            listStd.Label("BS_Rendering".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_SizeOffsetPawn".Translate(), ref settings.offsetBodyPos);
            CreateSettingCheckbox(listStd, "BS_SizeOffsetAnimalPawn".Translate(), ref settings.offsetAnimalBodyPos);
            CreateSettingCheckbox(listStd, "BS_DisabeVFCachine".Translate(), ref settings.disableTextureCaching);
            listStd.Label("BS_ScalePawnDefault".Translate());
            CreateSettingsSlider(listStd, "BS_ScaleLargerPawns".Translate(), ref settings.visualLargerMult, min: 0.05f, max: 20f, (f) => $"{f:F2}");
            CreateSettingsSlider(listStd, "BS_ScaleSmallerPawns".Translate(), ref settings.visualSmallerMult, min: 0.05f, max: 1f, (f) => $"{f:F2}");
            listStd.GapLine();
            listStd.Label("BS_HeadSizeExplain".Translate());
            CreateSettingsSlider(listStd, "BS_HeadExponentLargeField".Translate(), ref settings.headPowLarge, min: -2.00f, max: 2f, (f) => $"{f:F2}");
            listStd.Label("BS_HeadExponentSmallExplain".Translate());
            CreateSettingsSlider(listStd, "BS_HeadExponentSmalleField".Translate(), ref settings.headPowSmall, min: -1.00f, max: 2f, (f) => $"{f:F2}");
            listStd.GapLine();
            CreateSettingCheckbox(listStd, "BS_NormalizeBodyType".Translate(), ref settings.scaleBodyTypes);

            listStd.End();
            EndScrollArea();
        }

        //private void DrawGameMechanicsTab(Rect inRect)
        //{
        //    Listing_Standard listStd = new Listing_Standard();
        //    BeginScrollArea(inRect, ref scrollPosition, out Rect viewRect, 300f);
        //    listStd.Begin(viewRect);



        //    listStd.End();
        //    EndScrollArea();
        //}

        private void DrawAutoCombat(Rect inRect)
        {
            Listing_Standard listStd = new()
            {
                ColumnWidth = columnWidth
            };
            BeginScrollArea(inRect, ref scrollPosition, out Rect viewRect, 600f);
            listStd.Begin(viewRect);
            
            listStd.Label("BS_AutoCombatExplain".Translate());
            listStd.GapLine();
            CreateSettingCheckbox(listStd, "BS_EnabledDraftedJobs".Translate(), ref settings.enableDraftedJobs);
            listStd.GapLine();
            CreateSettingCheckbox(listStd, "BS_AutoCombatResets".Translate(), ref settings.autoCombatResets);
            CreateSettingCheckbox(listStd, "BS_ShowMeleeChargeBtn".Translate(), ref settings.showMeleeChargeBtn);
            CreateSettingCheckbox(listStd, "BS_ShowTakeCoverBtn".Translate(), ref settings.showTakeCoverBtn);
            CreateSettingCheckbox(listStd, "BS_ShowAutoUseAllAbilitiesBtn".Translate(), ref settings.showAutoUseAllAbilitiesBtn);

            CreateRadioButtonsTwoOptions(listStd, "BS_RightClickAutoCombat".Translate(), ref settings.rightClickAutoCombatShowsMenu,
                "BS_RightClickAutoCombat_ShowMenu".Translate(), "BS_RightClickAutoCombat_Toggle".Translate());

            listStd.End();
            EndScrollArea();
        }

        private void DrawExtrasTab(Rect inRect)
        {
            Listing_Standard listStd = new()
            {
                ColumnWidth = columnWidth
            };
            BeginScrollArea(inRect, ref scrollPosition, out Rect viewRect, 600f);
            listStd.Begin(viewRect);

            CreateSettingCheckbox(listStd, "BS_ForceDisableExtraUIWidgets".Translate(), ref settings.disableExtraWidgets);

            if (settings.disableExtraWidgets)
            {
                listStd.GapLine();
                listStd.Label("BS_ExtraUIForceDisabledWarning".Translate().AsTipTitle());
            }

            if (!settings.disableExtraWidgets)
            {
                if (BigSmall.BSGenesActive || GlobalSettings.IsFeatureEnabled("RecolorButton"))
                {
                    CreateSettingCheckbox(listStd, "BS_ShowColorPaletteBtn_Forced".Translate(), ref settings.forcedOn, disabled: true);
                }
                else
                {
                    CreateSettingCheckbox(listStd, "BS_ShowColorPaletteBtn".Translate(), ref settings.showClrPaletteBtn);
                }
                if (BigSmall.BSGenesActive || GlobalSettings.IsFeatureEnabled("RaceButton"))
                {
                    CreateSettingCheckbox(listStd, "BS_ShowRaceBtn_Forced".Translate(), ref settings.forcedOn, disabled: true);
                }
                else
                {
                    CreateSettingCheckbox(listStd, "BS_ShowRaceBtn".Translate(), ref settings.showRaceBtn);
                }
            }

            //CreateSettingCheckbox(listStd, "BS_PatchPlayerFactions".Translate(), ref settings.patchPlayerFactions);
            listStd.GapLine();
            CreateSettingCheckbox(listStd, "BS_SciFiNames".Translate(), ref settings.useSciFiNames);
            CreateSettingCheckbox(listStd, "BS_FantasyNames".Translate(), ref settings.useFantasyNames);

            listStd.End();
            EndScrollArea();
        }

        private void DrawAdvancedTab(Rect inRect)
        {
            Listing_Standard listStd = new()
            {
                ColumnWidth = columnWidth
            };
            BeginScrollArea(inRect, ref scrollPosition, out Rect viewRect, 600f);
            listStd.Begin(viewRect);

            listStd.Label("BS_RecolourAnything".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_MakeBionicsAndGenesRecolourable".Translate(), ref settings.makeDefsRecolorable);
            listStd.GapLine();

            listStd.Label("BS_ActivateExperimental".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_ActivateExperimental".Translate(), ref settings.experimental);
            listStd.GapLine();

            listStd.Label("BS_GenesSpecific".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_DoDefGeneration".Translate(), ref settings.generateDefs);

            listStd.End();
            EndScrollArea();
        }

        private void DrawDeveloperTab(Rect inRect)
        {
            Listing_Standard listStd = new()
            {
                ColumnWidth = columnWidth
            };
            BeginScrollArea(inRect, ref scrollPosition, out Rect viewRect, 600f);
            listStd.Begin(viewRect);

            listStd.Label("BS_DevSettings".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_JesusMode".Translate(), ref settings.jesusMode);
            CreateSettingCheckbox(listStd, "BS_RecruitDevSpawned".Translate(), ref settings.recruitDevSpawned);

            listStd.End();
            EndScrollArea();
        }

        

        public override string SettingsCategory()
        {
            return "BS_BigAndSmall".Translate();
        }
    }

    public class BSSettings : ModSettings
    {
        // SYSTEM. Don't sort.
        public bool forcedOn = true;
        public bool forcedOff = false;

        // Sort these

        private static readonly bool defaultGenerateDefs = true;
        public bool generateDefs = defaultGenerateDefs;

        private static readonly bool defaultPathRacesFromOtherMods = true;
        public bool pathRacesFromOtherMods = defaultPathRacesFromOtherMods;

        private static readonly bool defaultMakeDefsRecolorable = false;
        public bool makeDefsRecolorable = defaultMakeDefsRecolorable;

        // 6. Advanced Tab
        private static readonly bool defaultExperimental = false;
        public bool experimental = defaultExperimental;

        private static readonly bool defaultSapientAnimals = false;
        public bool sapientAnimals = defaultSapientAnimals;

        private static readonly float defaultSapientAnimalsChance = 0.00f;
        public float sapientAnimalsChance = defaultSapientAnimalsChance;

        private static readonly bool defaultSapientMechanoids = false;
        public bool sapientMechanoids = defaultSapientMechanoids;

        // 2. Races Tab
        private static readonly bool defaultSurgeryAndBionics = true;
        public bool surgeryAndBionics = defaultSurgeryAndBionics;

        private static readonly float defaultVisualLargerMult = 1f;
        public float visualLargerMult = defaultVisualLargerMult;
        
        private static readonly float defaultVisualSmallerMult = 1f;
        public float visualSmallerMult = defaultVisualSmallerMult;

        private static readonly float defaultHeadPowLarge = 0.8f;
        public float headPowLarge = defaultHeadPowLarge;

        private static readonly float defaultHeadPowSmall = 0.65f;
        public float headPowSmall = defaultHeadPowSmall;

        private static readonly float defaultDmgExponent = 0.75f;
        public float dmgExponent = defaultDmgExponent;

        private static readonly float defaultFlatDmgIncrease = 8;
        public float flatDamageIncrease = defaultFlatDmgIncrease;

        private static readonly float defaultHungerRate = 1f;
        public float hungerRate = defaultHungerRate;

        private static readonly bool defaultScaleBT = false;
        public bool scaleBodyTypes = defaultScaleBT;

        // 3. Size Tab
        private static readonly bool defaultScaleAnimals = true;
        public bool scaleAnimals = defaultScaleAnimals;

        private static readonly bool defaultDisableTextureCaching = true;
        public bool disableTextureCaching = defaultDisableTextureCaching;

        private static readonly bool defaultRealTimeUpdates = false;
        public bool realTimeUpdates = defaultRealTimeUpdates;

        private static readonly bool defaultOffsetBodyPos = true;
        public bool offsetBodyPos = defaultOffsetBodyPos;

        private static readonly bool defaultOffsetAnimalBodyPos = true;
        public bool offsetAnimalBodyPos = defaultOffsetAnimalBodyPos;

        // Misc
        private static readonly bool defaultPatchPlayerFactions = true;
        public bool patchPlayerFactions = defaultPatchPlayerFactions;

        // 1. General Tab
        public static readonly bool defaultPreventUndead = false;
        public bool preventUndead = defaultPreventUndead;

        public static readonly bool defaultUseSciFiNaming = false;
        public bool useSciFiNames = defaultUseSciFiNaming;

        public static readonly bool defaultUseFantasyNaming = false;
        public bool useFantasyNames = defaultUseFantasyNaming;

        public static readonly float inflitratorChanceDefault = 0.01f;
        public float inflitratorChance = inflitratorChanceDefault;

        public static readonly float inflitratorRaidChanceDefault = 0.005f;
        public float inflitratorRaidChance = inflitratorRaidChanceDefault;

        public static readonly float immortalReturnTimeFactorDefault = 1f;
        public float immortalReturnTimeFactor = immortalReturnTimeFactorDefault;

        public static readonly float soulPowerFalloffOffsetDefault = 0f;
        public float soulPowerFalloffOffset = soulPowerFalloffOffsetDefault;

        public static readonly float soulPowerGainMultiplierDefault = 1f;
        public float soulPowerGainMultiplier = soulPowerGainMultiplierDefault;

        private static readonly bool defaultAllAnimalsHaveHands = false;
        public bool allAnimalsHaveHands = defaultAllAnimalsHaveHands;

        private static readonly bool defaultAnimalOnAnimal = false;
        public bool animalOnAnimal = defaultAnimalOnAnimal;

        private static readonly bool defaultAnimalsLowSkillPenalty = false;
        public bool animalsLowSkillPenalty = defaultAnimalsLowSkillPenalty;

        // 4. AutoCombat Tab
        private static readonly bool defaultEnableDraftedJobs = false;
        public bool enableDraftedJobs = defaultEnableDraftedJobs;

        public static readonly bool defaultAutoCombatResets = false;
        public bool autoCombatResets = defaultAutoCombatResets;

        public static readonly bool defaultShowMeleeChargeBtn = true;
        public bool showMeleeChargeBtn = defaultShowMeleeChargeBtn;

        public static readonly bool defaultShowTakeCoverBtn = true;
        public bool showTakeCoverBtn = defaultShowTakeCoverBtn;

        public static readonly bool defaultShowAutoUseAllAbilitiesBtn = true;
        public bool showAutoUseAllAbilitiesBtn = defaultShowAutoUseAllAbilitiesBtn;

        public static readonly bool defaultRightClickAutoCombatShowsMenu = false;
        public bool rightClickAutoCombatShowsMenu = defaultRightClickAutoCombatShowsMenu;

        // 5. Extras Tab
        private static readonly bool defaultShowClrPaletteBtn = false;
        public bool showClrPaletteBtn = defaultShowClrPaletteBtn;

        private static readonly bool defaultShowRaceBtn = false;
        public bool showRaceBtn = defaultShowRaceBtn;


        // Special
        public static readonly bool defaultDisableExtraWidgets = false;
        public bool disableExtraWidgets = defaultDisableExtraWidgets;


        // 7. Developer Tab
        public static readonly bool defaultJesusMode = false;
        public bool jesusMode = defaultJesusMode;

        public static readonly bool defaultRecruitDevSpawned = true;
        public bool recruitDevSpawned = defaultRecruitDevSpawned;

        public bool GetAndroidsEnabled()
        {
            return sapientMechanoids || ModsConfig.IsActive("RedMattis.BigSmall.SimpleAndroids") || ModsConfig.IsActive("RedMattis.BigSmall.Core");
        }

        public override void ExposeData()
        {
            // 1. General Tab
            Scribe_Values.Look(ref preventUndead, "preventUndead", defaultPreventUndead);
            Scribe_Values.Look(ref inflitratorChance, "inflitratorChance", inflitratorChanceDefault);
            Scribe_Values.Look(ref inflitratorRaidChance, "inflitratorRaidChance", inflitratorRaidChanceDefault);
            Scribe_Values.Look(ref immortalReturnTimeFactor, "immortalReturnTimeFactor", immortalReturnTimeFactorDefault);
            Scribe_Values.Look(ref soulPowerFalloffOffset, "soulPowerFalloffOffset", soulPowerFalloffOffsetDefault);
            Scribe_Values.Look(ref soulPowerGainMultiplier, "soulPowerGainMultiplier", soulPowerGainMultiplierDefault);

            // 2. Races Tab
            Scribe_Values.Look(ref surgeryAndBionics, "surgeryAndBionics", defaultSurgeryAndBionics);
            Scribe_Values.Look(ref sapientAnimals, "sapientAnimals", defaultSapientAnimals);
            Scribe_Values.Look(ref sapientAnimalsChance, "sapientAnimalsChance", defaultSapientAnimalsChance);
            Scribe_Values.Look(ref sapientMechanoids, "sapientMechanoids", defaultSapientMechanoids);
            Scribe_Values.Look(ref allAnimalsHaveHands, "allAnimalsHaveHands", defaultAllAnimalsHaveHands);
            Scribe_Values.Look(ref animalOnAnimal, "sapientAnimalsCanRomanceAnySapientAnimals", defaultAnimalOnAnimal);
            Scribe_Values.Look(ref animalsLowSkillPenalty, "animalsNoSkillPenalty", defaultAnimalsLowSkillPenalty);

            // 3. Size Tab
            Scribe_Values.Look(ref scaleAnimals, "scaleAnimals", defaultScaleAnimals);
            Scribe_Values.Look(ref dmgExponent, "dmgExponent", defaultDmgExponent);
            Scribe_Values.Look(ref flatDamageIncrease, "flatDmgIncrease", defaultFlatDmgIncrease);
            Scribe_Values.Look(ref hungerRate, "hungerRate", defaultHungerRate);
            Scribe_Values.Look(ref offsetBodyPos, "offsetBodyPos", defaultOffsetBodyPos);
            Scribe_Values.Look(ref offsetAnimalBodyPos, "offsetAnimalBodyPos", defaultOffsetAnimalBodyPos);
            Scribe_Values.Look(ref disableTextureCaching, "disableBSTextureCaching", defaultDisableTextureCaching);
            Scribe_Values.Look(ref visualLargerMult, "visualLargerMult", defaultVisualLargerMult);
            Scribe_Values.Look(ref visualSmallerMult, "visualSmallerMult", defaultVisualSmallerMult);
            Scribe_Values.Look(ref headPowLarge, "headPowLarge", defaultHeadPowLarge);
            Scribe_Values.Look(ref headPowSmall, "headPowSmall2", defaultHeadPowSmall);
            Scribe_Values.Look(ref scaleBodyTypes, "scaleBt", defaultScaleBT);

            // 4. AutoCombat Tab
            Scribe_Values.Look(ref enableDraftedJobs, "enableDraftedJobs", defaultEnableDraftedJobs);
            Scribe_Values.Look(ref autoCombatResets, "autoCombatResets", defaultAutoCombatResets);
            Scribe_Values.Look(ref showMeleeChargeBtn, "showMeleeChargeBtn", defaultShowMeleeChargeBtn);
            Scribe_Values.Look(ref showTakeCoverBtn, "showTakeCoverBtn", defaultShowTakeCoverBtn);
            Scribe_Values.Look(ref showAutoUseAllAbilitiesBtn, "showAutoUseAllAbilitiesBtn", defaultShowAutoUseAllAbilitiesBtn);
            Scribe_Values.Look(ref rightClickAutoCombatShowsMenu, "rightClickAutoCombatShowsMenu", defaultRightClickAutoCombatShowsMenu);

            // 5. Extras Tab
            Scribe_Values.Look(ref showClrPaletteBtn, "showClrPaletteBtn", defaultShowClrPaletteBtn);
            Scribe_Values.Look(ref showRaceBtn, "showRaceBtn", defaultShowRaceBtn);
            Scribe_Values.Look(ref disableExtraWidgets, "disableExtraWidgets", defaultDisableExtraWidgets);
            Scribe_Values.Look(ref useSciFiNames, "useSciFiNames", defaultUseSciFiNaming);
            Scribe_Values.Look(ref useFantasyNames, "useFantasyNames", defaultUseFantasyNaming);

            // 6. Advanced Tab
            Scribe_Values.Look(ref experimental, "experimental", defaultExperimental);
            Scribe_Values.Look(ref makeDefsRecolorable, "makeDefsRecolorable", defaultMakeDefsRecolorable);
            Scribe_Values.Look(ref pathRacesFromOtherMods, "pathRacesFromOtherMods", defaultPathRacesFromOtherMods);
            Scribe_Values.Look(ref generateDefs, "generateDefs", defaultGenerateDefs);

            // 7. Developer Tab
            Scribe_Values.Look(ref jesusMode, "jesusMode", defaultJesusMode);
            Scribe_Values.Look(ref recruitDevSpawned, "recruitDevSpawned", defaultRecruitDevSpawned);

            // Misc
            Scribe_Values.Look(ref patchPlayerFactions, "patchPlayerFactions", defaultPatchPlayerFactions);
            Scribe_Values.Look(ref realTimeUpdates, "realTimeUpdates", defaultRealTimeUpdates);

            base.ExposeData();
        }

        public void ResetToDefault()
        {
            // 1. General Tab
            preventUndead = defaultPreventUndead;
            inflitratorChance = inflitratorChanceDefault;
            inflitratorRaidChance = inflitratorRaidChanceDefault;
            immortalReturnTimeFactor = immortalReturnTimeFactorDefault;
            soulPowerFalloffOffset = soulPowerFalloffOffsetDefault;
            soulPowerGainMultiplier = soulPowerGainMultiplierDefault;

            // 2. Races Tab
            surgeryAndBionics = defaultSurgeryAndBionics;
            sapientAnimals = defaultSapientAnimals;
            sapientAnimalsChance = defaultSapientAnimalsChance;
            sapientMechanoids = defaultSapientMechanoids;
            allAnimalsHaveHands = defaultAllAnimalsHaveHands;
            animalOnAnimal = defaultAnimalOnAnimal;
            animalsLowSkillPenalty = defaultAnimalsLowSkillPenalty;

            // 3. Size Tab
            scaleAnimals = defaultScaleAnimals;
            dmgExponent = defaultDmgExponent;
            flatDamageIncrease = defaultFlatDmgIncrease;
            hungerRate = defaultHungerRate;
            offsetBodyPos = defaultOffsetBodyPos;
            offsetAnimalBodyPos = defaultOffsetAnimalBodyPos;
            disableTextureCaching = defaultDisableTextureCaching;
            visualLargerMult = defaultVisualLargerMult;
            visualSmallerMult = defaultVisualSmallerMult;
            headPowLarge = defaultHeadPowLarge;
            headPowSmall = defaultHeadPowSmall;
            scaleBodyTypes = defaultScaleBT;

            // 4. AutoCombat Tab
            enableDraftedJobs = defaultEnableDraftedJobs;
            autoCombatResets = defaultAutoCombatResets;
            showMeleeChargeBtn = defaultShowMeleeChargeBtn;
            showTakeCoverBtn = defaultShowTakeCoverBtn;
            showAutoUseAllAbilitiesBtn = defaultShowAutoUseAllAbilitiesBtn;
            rightClickAutoCombatShowsMenu = defaultRightClickAutoCombatShowsMenu;

            // 5. Extras Tab
            showClrPaletteBtn = defaultShowClrPaletteBtn;
            showRaceBtn = defaultShowRaceBtn;
            disableExtraWidgets = defaultDisableExtraWidgets;
            useSciFiNames = defaultUseSciFiNaming;
            useFantasyNames = defaultUseFantasyNaming;

            // 6. Advanced Tab
            experimental = defaultExperimental;
            makeDefsRecolorable = defaultMakeDefsRecolorable;
            pathRacesFromOtherMods = defaultPathRacesFromOtherMods;
            generateDefs = defaultGenerateDefs;

            // 7. Developer Tab
            jesusMode = defaultJesusMode;
            recruitDevSpawned = defaultRecruitDevSpawned;

            // Misc
            patchPlayerFactions = defaultPatchPlayerFactions;
            realTimeUpdates = defaultRealTimeUpdates;
        }
        public void ResetToRecommended()
        {
            ResetToDefault();
            scaleBodyTypes = true;
            enableDraftedJobs = true;

        }
    }
}