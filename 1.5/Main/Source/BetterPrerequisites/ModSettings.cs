using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static BigAndSmall.SettingsWidgets;

namespace BigAndSmall
{
    [StaticConstructorOnStartup]
    public class BigSmallMod : Mod
    {
        private string cacheTickTxt;
        private string sizeLargerMultTxt;
        private string sizeSmallerMultTxt;
        private string headPowLargeTxt;
        private string headPowSmallTxt;

        private string damageScaleTxt;
        private string damageFlatTxt;
        //private string healthScaleTxt;
        private string hungerScaleTxt;

        public static BSRettings settings = null;

        public BigSmallMod(ModContentPack content) : base(content)
        {
            settings ??= GetSettings<BSRettings>();

            //// Check if pawnmorhper (tachyonite.pawnmorpherpublic) is active
            //if (ModLister.HasActiveModWithName("Pawnmorpher"))
            //{
            //    Log.Warning($"Big and Small: Auotmatically disabled Big and Small's scaling for animals because Pawnmorpher is active.\n" +
            //        $"A compatibility patch might show up at a later date.");
            //    settings.scaleAnimals = false;
            //}
        }

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

            // Reset Cache Button
            if (listStd.ButtonText("BS_ResetCache".Translate()))
            {
                var pawns = HumanoidPawnScaler.Cache.Keys.Select(x=>x).ToList();
                BigAndSmallCache.scribedCache = [];
                BigAndSmallCache.refreshQueue.Clear();
                BigAndSmallCache.queuedJobs.Clear();
                BigAndSmallCache.schedulePostUpdate.Clear();
                BigAndSmallCache.scheduleFullUpdate.Clear();
                HumanoidPawnScaler.Cache = new ConcurrentDictionary<Pawn, BSCache>();
                Log.Message($"Reset Cache. Updating cache for {pawns.Count} pawns.");
                foreach (var pawn in pawns.Where(x => x != null && !x.Discarded && !x.Destroyed))
                {
                    if (HumanoidPawnScaler.GetCache(pawn, forceRefresh:true, canRegenerate:true) is BSCache cache)
                    {
                        Log.Message($"Big and Small: Reset cache for {pawn}");
                        //try
                        //{
                        //    //Log.Message($"Big and Small: Force-Regen...{pawn}");
                        //    //cache.RegenerateCache();
                        //}
                        //catch (Exception e)
                        //{
                        //    Log.Warning($"Big and Small: Error updating cache for {pawn}: {e}");
                        //}
                    }
                }
            }
            if (listStd.ButtonText("BS_ResetSettings".Translate()))
            {
                settings.ResetToDefault();
            }


            listStd.Label("BS_GenesSpecific".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_DoDefGeneration".Translate(), ref settings.generateDefs);
            listStd.GapLine();

            listStd.Label("BS_GameMechanics".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_ScaleAnimals".Translate(), ref settings.scaleAnimals);
            CreateSettingCheckbox(listStd, "BS_PreventUndead".Translate(), ref settings.preventUndead);
            listStd.GapLine();
            listStd.Label("BS_LowestUsed".Translate());

            CreateSettingsSlider(listStd, "BS_MultDamageExplain".Translate(), ref settings.dmgExponent, min:0, max:2, valueFormatter: (f) => $"{f*100:F2}%");
            CreateSettingsSlider(listStd, "BS_FlatDMGExplain".Translate(), ref settings.flatDamageIncrease, 1f, 20f, (f) => $"{f:F0}");

            listStd.GapLine();
            CreateSettingsSlider(listStd, "BS_HungerMultiplierField".Translate(), ref settings.hungerRate, 0f, 1, (f) => $"{f*100:F0}%");
            listStd.GapLine();

            listStd.Label("BS_MiscGameMechanics".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_PatchPlayerFactions".Translate(), ref settings.patchPlayerFactions);
            listStd.GapLine();

            listStd.Label("BS_Rendering".Translate().AsTipTitle());
            CreateSettingCheckbox(listStd, "BS_SizeOffsetPawn", ref settings.offsetBodyPos);
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
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "BS_BigAndSmall".Translate();
        }
    }

    public class BSRettings : ModSettings
    {
        private static readonly bool defaultGenerateDefs = true;
        public bool generateDefs = defaultGenerateDefs;

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

        private static readonly bool defaultScaleAnimals = true;
        public bool scaleAnimals = defaultScaleAnimals;

        private static readonly bool defaultDisableTextureCaching = true;
        public bool disableTextureCaching = defaultDisableTextureCaching;

        private static readonly bool defaultRealTimeUpdates = false;
        public bool realTimeUpdates = defaultRealTimeUpdates;

        private static readonly bool defaultOffsetBodyPos = true;
        public bool offsetBodyPos = defaultOffsetBodyPos;

        private static readonly bool defaultPatchPlayerFactions = true;
        public bool patchPlayerFactions = defaultPatchPlayerFactions;

        public static readonly bool defaultPreventUndead = false;
        public bool preventUndead = defaultPreventUndead;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref generateDefs, "generateDefs", defaultGenerateDefs);
            Scribe_Values.Look(ref visualLargerMult, "visualLargerMult", defaultVisualLargerMult);
            Scribe_Values.Look(ref visualSmallerMult, "visualSmallerMult", defaultVisualSmallerMult);
            Scribe_Values.Look(ref headPowLarge, "headPowLarge", defaultHeadPowLarge);
            Scribe_Values.Look(ref headPowSmall, "headPowSmall2", defaultHeadPowSmall);
            Scribe_Values.Look(ref dmgExponent, "dmgExponent", defaultDmgExponent);
            Scribe_Values.Look(ref hungerRate, "hungerRate", defaultHungerRate);
            Scribe_Values.Look(ref scaleAnimals, "scaleAnimals", defaultScaleAnimals);
            Scribe_Values.Look(ref scaleBodyTypes, "scaleBt", defaultScaleBT);
            Scribe_Values.Look(ref flatDamageIncrease, "flatDmgIncrease", defaultFlatDmgIncrease);
            Scribe_Values.Look(ref disableTextureCaching, "disableBSTextureCaching", defaultDisableTextureCaching);
            Scribe_Values.Look(ref realTimeUpdates, "realTimeUpdates", defaultRealTimeUpdates);
            //Scribe_Values.Look(ref offsetBodyPos, "offsetBodyPos_EXPERIMENTAL", defaultOffsetBodyPos);
            Scribe_Values.Look(ref offsetBodyPos, "offsetBodyPos", defaultOffsetBodyPos);
            Scribe_Values.Look(ref patchPlayerFactions, "patchPlayerFactions", defaultPatchPlayerFactions);
            Scribe_Values.Look(ref preventUndead, "preventUndead", defaultPreventUndead);
            base.ExposeData();
        }

        public void ResetToDefault()
        {
            generateDefs = defaultGenerateDefs;
            visualLargerMult = defaultVisualLargerMult;
            visualSmallerMult = defaultVisualSmallerMult;
            headPowLarge = defaultHeadPowLarge;
            headPowSmall = defaultHeadPowSmall;
            dmgExponent = defaultDmgExponent;
            hungerRate = defaultHungerRate;
            scaleAnimals = defaultScaleAnimals;
            scaleBodyTypes = defaultScaleBT;
            flatDamageIncrease = defaultFlatDmgIncrease;
            disableTextureCaching = defaultDisableTextureCaching;
            realTimeUpdates = defaultRealTimeUpdates;
            offsetBodyPos = defaultOffsetBodyPos;
            patchPlayerFactions = defaultPatchPlayerFactions;
            preventUndead = defaultPreventUndead;
        }
    }
}
