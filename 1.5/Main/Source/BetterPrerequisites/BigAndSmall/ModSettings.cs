using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

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

            Rect rect = inRect.ContractedBy(10f);
            rect.height -= listStd.CurHeight;
            rect.y += listStd.CurHeight;
            Widgets.DrawBoxSolid(rect, Color.grey);
            Rect rect2 = rect.ContractedBy(1f);
            Widgets.DrawBoxSolid(rect2, new ColorInt(42, 43, 44).ToColor);
            Rect rect3 = rect2.ContractedBy(5f);
            //rect3.y += 15f;
            //rect3.height -= 15f;
            Rect rect4 = rect3;
            rect4.x = 0f;
            rect4.y = 0f;
            rect4.width -= 20f;
            rect4.height = 950f;
            Widgets.BeginScrollView(rect3, ref scrollPosition, rect4);

            listStd.Begin(rect4.AtZero());
            //listStd.TextFieldNumericLabeled("BS_CacheUpdateTickRate".Translate(), ref settings.cacheUpdateFrequency, ref cacheTickTxt, 250, 10000);
            //listStd.CheckboxLabeled("BS_RealtimeUpdate".Translate(), ref settings.realTimeUpdates, 1);
            listStd.Label("BS_GenesSpecific".Translate().AsTipTitle());
            listStd.Label("BS_DoDefGeneration".Translate());
            listStd.CheckboxLabeled("", ref settings.generateDefs, 0);
            listStd.GapLine();

            listStd.Label("BS_GameMechanics".Translate().AsTipTitle());
            listStd.Label("BS_ScaleAnimals".Translate());
            listStd.CheckboxLabeled("", ref settings.scaleAnimals, 0);
            listStd.GapLine();
            listStd.Label("BS_LowestUsed".Translate());
            listStd.Label("BS_MultDamageExplain".Translate());
            listStd.TextFieldNumericLabeled("BS_DamageExponentExplain".Translate(), ref settings.dmgExponent, ref damageScaleTxt, min: 0.00f, max: 2f);
            listStd.Label("BS_FlatDMGExplain".Translate());
            listStd.TextFieldNumericLabeled("BS_FlatDamageBonunsField".Translate(), ref settings.flatDamageIncrease, ref damageFlatTxt, min: 1, max: 999f);
            listStd.GapLine();
            listStd.Label("BS_NutritionBurnExplain".Translate());
            listStd.TextFieldNumericLabeled("BS_HungerMultiplierField".Translate(), ref settings.hungerRate, ref hungerScaleTxt, min: 0.0f, max: 1f);
            listStd.GapLine();

            listStd.Label("BS_MiscGameMechanics".Translate().AsTipTitle());
            listStd.CheckboxLabeled("BS_PatchPlayerFactions", ref settings.patchPlayerFactions, 1);
            listStd.GapLine();

            listStd.Label("BS_Rendering".Translate().AsTipTitle());
            listStd.CheckboxLabeled("Size offsets pawn", ref settings.offsetBodyPos, 0);
            listStd.CheckboxLabeled("BS_DisabeVFCachine".Translate(), ref settings.disableTextureCaching, 1);
            listStd.Label("BS_ScalePawnDefault".Translate());
            listStd.TextFieldNumericLabeled("BS_ScaleLargerPawns".Translate(), ref settings.visualLargerMult, ref sizeLargerMultTxt, min: 0.05f, max: 20f);
            listStd.TextFieldNumericLabeled("BS_ScaleSmallerPawns".Translate(), ref settings.visualSmallerMult, ref sizeSmallerMultTxt, min: 0.05f, max: 1f);
            listStd.GapLine();
            listStd.Label("BS_HeadSizeExplain".Translate());
            listStd.TextFieldNumericLabeled("BS_HeadExponentLargeField".Translate(), ref settings.headPowLarge, ref headPowLargeTxt, min: -2.00f, max: 2f);
            listStd.Label("BS_HeadExponentSmallExplain".Translate());
            listStd.TextFieldNumericLabeled("BS_HeadExponentSmalleField".Translate(), ref settings.headPowSmall, ref headPowSmallTxt, min: -1.00f, max: 2f);
            listStd.GapLine();
            listStd.Label("BS_NormalizeBodyType".Translate());
            listStd.CheckboxLabeled("", ref settings.scaleBodyTypes, 0);

            

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
            base.ExposeData();
        }
    }
}
