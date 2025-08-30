using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace BigAndSmall
{
    

    public class EditPawnWindow : Window
    {
        protected struct ThingGData(Thing t, EditMode editMode, HasCustomizableGraphics cgExt, Def def = null)
        {
            public HasCustomizableGraphics cgExt = cgExt;
            public Thing thing = t;
            public Def def = def;
            public EditMode tabCat = editMode;
            
            public override readonly string ToString() => $"[ThingGData] Thing: {thing}, Def: {def}, EditMode: {tabCat}, HasCustomizableGraphics: {cgExt}";
        }

        protected enum EditMode
        {
            Thing,
            Apparel,
            //Gene,
            CustomTag,
        }
        private readonly ILoadReferenceable target;
        private static readonly Vector2 ButtonSize = new(200f, 40f);

        private int selectedTab = 0;
        private EditMode activeTab = EditMode.Thing;
        private readonly List<EditMode> tabsWithContent = [];
        private readonly List<ThingGData> EditableThings = [];
        private readonly Pawn pawn = null;

        private Vector2 scrollPosition = Vector2.zero;
        private float scrollViewHeight = 0f;

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.x = windowRect.x - InitialSize.x;
        }

        public override Vector2 InitialSize => new(600f, 800f);

        public EditPawnWindow(ILoadReferenceable target)
        {
            this.pawn = null;
            _tabs = null;
            tabsWithContent = [EditMode.Thing];
            EditableThings = [];

            this.target = target;
            forcePause = false;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = false;
            preventCameraMotion = false;
            resizeable = true;
            draggable = true;
            doCloseButton = true;

            if (target is Pawn pawn)
            {
                this.pawn = pawn;

                EditableThings.Add(new ThingGData(pawn, EditMode.Thing, null));
                foreach (var item in pawn.apparel.WornApparel)
                {
                    if (item.def.ExtensionsOnDef<HasCustomizableGraphics, ThingDef>()?.FirstOrDefault() is HasCustomizableGraphics cg)
                    {
                        EditableThings.Add(new ThingGData(item, EditMode.Apparel, cg));
                        tabsWithContent.Add(EditMode.Apparel);
                    }
                    else if (item.WornGraphicPath != null)
                    {
                        EditableThings.Add(new ThingGData(item, EditMode.Apparel, null));
                        tabsWithContent.Add(EditMode.Apparel);
                    }
                    else
                    {
                        Log.Message($"[BigAndSmall] Tried to edit apparel {item} but it has no graphics extension or worn graphic path.");
                    }
                }
                if (ModsConfig.BiotechActive)
                {
                    foreach (var gene in GeneHelpers.GetAllActiveGenes(pawn))
                    {
                        foreach (var cg in gene.def.ExtensionsOnDef<HasCustomizableGraphics, GeneDef>())
                        {
                            EditableThings.Add(new ThingGData(pawn, EditMode.CustomTag, cg, def: gene.def));
                            tabsWithContent.Add(EditMode.CustomTag);
                        }
                    }
                }
                foreach(var hediff in pawn.health.hediffSet.hediffs)
                {
                    foreach(var cg in hediff.def.ExtensionsOnDef<HasCustomizableGraphics, HediffDef>())
                    {
                        EditableThings.Add(new ThingGData(pawn, EditMode.CustomTag, cg, def: hediff.def));
                        tabsWithContent.Add(EditMode.CustomTag);
                    }
                }
            }
            tabsWithContent = [.. tabsWithContent.Distinct()];
            
        }

        private string[] _tabs = null;
        private string[] GetTabKeys() => _tabs ??= [.. tabsWithContent.Select(x => $"BS_Tab_{x}").ToArray()];

        public override void DoWindowContents(Rect inRect)
        {
            const float contentHeight = 30f;
            const float tabHeight = 35f;

            if (inRect.width < 400f) inRect.width = 400f;

            Rect tabRect = new(inRect.x, inRect.y + tabHeight - 4, inRect.width, tabHeight);
            Rect contentRect = new(inRect.x, inRect.y + contentHeight, inRect.width, inRect.height - contentHeight-40);

            Widgets.DrawMenuSection(contentRect);

            // Tab stuff
            var tabKeys = GetTabKeys();
            int tabCount = tabKeys.Length;
            var tabs = new List<TabRecord>();
            for (int i = 0; i < tabCount; i++)
            {
                int tabIndex = i;
                if (tabKeys[i] == $"BS_Tab_{nameof(EditMode.Thing)}")
                {
                    tabs.Add(new TabRecord(target.ToString(), () => selectedTab = tabIndex, selectedTab == tabIndex));
                }
                else
                {
                    tabs.Add(new TabRecord(tabKeys[i].Translate(), () => selectedTab = tabIndex, selectedTab == tabIndex));
                }
            }
            TabDrawer.DrawTabs(tabRect, tabs);

            activeTab = tabsWithContent[selectedTab];

            Rect innerRect = contentRect.ContractedBy(12);
            DrawMainUI(innerRect, activeTab);
            //Close();
        }

        private void DrawMainUI(Rect rect, EditMode tab)
        {
            Rect scrollViewRect = new(rect)
            {
                height = scrollViewHeight,
                width = rect.width - 50
            };
            rect = rect.ContractedBy(2);
            Widgets.BeginScrollView(rect, ref scrollPosition, scrollViewRect);
            rect.width -= 48;
            float curY = rect.y;

            foreach (var graphicData in EditableThings)
            {
                var gMode = graphicData.tabCat;
                if (gMode != tab) continue;

                var thing = graphicData.thing;
                var hasCustomDef = graphicData.cgExt;
                var cdTag = graphicData.cgExt?.tag;
                if (thing is Apparel apparel)
                {
                    if (gMode == EditMode.Apparel)
                    {
                        curY = DrawApparelSettings(rect, curY, apparel, hasCustomDef);
                    }
                }
                if (thing is Pawn pawn)
                {
                    switch (gMode)
                    {
                        case EditMode.Thing:
                            curY = DrawThingSettings(rect, curY, pawn);
                            break;
                        case EditMode.CustomTag:
                            curY = DrawGeneSettings(rect, curY, graphicData, thing, hasCustomDef, cdTag, pawn);
                            curY = DrawHediffSettings(rect, curY, graphicData, thing, hasCustomDef, cdTag, pawn);
                            break;
                    }
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = curY - rect.y;
            }

            Widgets.EndScrollView();

            float DrawThingSettings(Rect rect, float curY, Pawn pawn)
            {
                //DrawTitle(pawn.LabelCap, rect, ref curY);
                DrawTitle("BS_Skin".Translate(), rect, ref curY);
                DrawColorPicker(pawn, pawn.story.SkinColor, rect, ref curY, (Color col) => pawn.story.skinColorOverride = col);
                DrawTitle("BS_Hair".Translate(), rect, ref curY);
                DrawColorPicker(pawn, pawn.story.HairColor, rect, ref curY, (Color col) => pawn.story.HairColor = col);
                //DrawTitle("BS_Custom".Translate(), rect, ref curY);
                DrawColorPicker(pawn, pawn.GetCustomColorA(), rect, ref curY, (Color col) => pawn.SetCustomColorA(col),
                            "BS_Customize_Str".Translate("A"));
                DrawColorPicker(pawn, pawn.GetCustomColorB(), rect, ref curY, (Color col) => pawn.SetCustomColorB(col),
                            "BS_Customize_Str".Translate("B"));
                DrawColorPicker(pawn, pawn.GetCustomColorC(), rect, ref curY, (Color col) => pawn.SetCustomColorC(col),
                            "BS_Customize_Str".Translate("C"));
                return curY;
            }

            float DrawApparelSettings(Rect rect, float curY, Apparel apparel, HasCustomizableGraphics hasCustomDef)
            {
                DrawTitle(apparel.def.LabelCap, rect, ref curY);
                DrawApparelIcon(apparel, rect, ref curY);
                if (hasCustomDef?.colorA == true)
                {
                    if (apparel.GetCustomColorA() == null)
                    {
                        DrawColorPicker(pawn, apparel.DrawColor, rect, ref curY, (Color col) => apparel.DrawColor = col);
                    }
                    DrawColorPicker(pawn, apparel.GetCustomColorA(), rect, ref curY, (Color col) => apparel.SetCustomColorA(apparel.DrawColor = col),
                            "BS_Customize_Str".Translate("A"));
                }
                else
                {
                    DrawColorPicker(pawn, apparel.DrawColor, rect, ref curY, (Color col) => apparel.DrawColor = col);
                }
                if (hasCustomDef?.colorB == true)
                {
                    DrawColorPicker(pawn, apparel.GetCustomColorB(), rect, ref curY, (Color col) => apparel.SetCustomColorB(col),
                            "BS_Customize_Str".Translate("B"));
                }
                if (hasCustomDef?.colorC == true)
                {
                    DrawColorPicker(pawn, apparel.GetCustomColorC(), rect, ref curY, (Color col) => apparel.SetCustomColorC(col),
                            "BS_Customize_Str".Translate("C"));
                }

                return curY;
            }

            float DrawGeneSettings(Rect rect, float curY, ThingGData graphicData, Thing thing, HasCustomizableGraphics hasCustomDef, FlagString cdTag, Pawn pawn)
            {
                if (graphicData.def is GeneDef geneDef)
                {
                    DrawTitle(geneDef.LabelCap, rect, ref curY);
                    DrawGeneIcon(geneDef, rect, ref curY);
                    if (hasCustomDef.colorA)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 0), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 0, col),
                            "BS_Customize_Str".Translate("A"));
                    }
                    if (hasCustomDef.colorB)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 1), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 1, col),
                            "BS_Customize_Str".Translate("B"));
                    }
                    if (hasCustomDef.colorC)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 2), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 2, col),
                            "BS_Customize_Str".Translate("C"));
                    }
                }

                return curY;
            }

            float DrawHediffSettings(Rect rect, float curY, ThingGData graphicData, Thing thing, HasCustomizableGraphics hasCustomDef, FlagString cdTag, Pawn pawn)
            {
                if (graphicData.def is HediffDef hediffDef)
                {
                    DrawTitle(hediffDef.LabelCap, rect, ref curY);
                    if (hasCustomDef.colorA)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 0), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 0, col),
                            "BS_Customize_Str".Translate("A"));
                    }
                    if (hasCustomDef.colorB)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 1), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 1, col),
                            "BS_Customize_Str".Translate("B"));
                    }
                    if (hasCustomDef.colorC)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 2), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 2, col),
                            "BS_Customize_Str".Translate("C"));
                    }
                }

                return curY;
            }
        }

        private void DrawTitle(string titleText, Rect rect, ref float curY)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new(rect)
            {
                y = curY,
                height = Text.LineHeight * 2
            };
            Widgets.Label(titleRect, titleText);
            Text.Font = GameFont.Small;
            curY += titleRect.height;
        }

        private void DrawApparelIcon(Apparel apparel, Rect rect, ref float curY)
        {
            //GUI.color = Color.white;
            var iconRect = new Rect(rect.x, curY, 64, 64);
            Widgets.DrawTextureFitted(iconRect, apparel.Graphic.MatSouth.mainTexture, 1f);
            curY += iconRect.height + 12;
        }

        private void DrawGeneIcon(GeneDef geneDef, Rect rect, ref float curY)
        {
            if (geneDef.Icon != null)
            {
                //GUI.color = Color.white;
                var iconRect = new Rect(rect.x, curY, 64, 64);
                Widgets.DrawTextureFitted(iconRect, geneDef.Icon, 1f);
                curY += rect.height + 24;
            }
        }

        public bool draggingSlider = false;
        public bool draggingWheel = false;
        private void DrawColorPicker(Pawn pawn, Color? currClrNullable, Rect rect, ref float curY, Action<Color> setColor,
            string title = null,
            string overrideClrStr=null)
        {
            if (currClrNullable == null)
            {
                overrideClrStr ??= "BS_CustomizableColor".Translate();
                var overrideRect = new Rect(rect.x, curY, ButtonSize.x, ButtonSize.y);
                if (Widgets.ButtonText(overrideRect, overrideClrStr))
                {
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    setColor(Color.cyan);
                    pawn?.Drawer.renderer.SetAllGraphicsDirty();
                }
                curY = overrideRect.yMax + 14f;
                return;
            }
            else
            {
                if (title != null)
                {
                    DrawTitle(title, rect, ref curY);
                }
                Color currClr = currClrNullable.Value;
                Color color = currClr;
                const float height = 180;
                if (SmartColorWidgets.MakeColorPicker(new Rect(rect.x, curY, rect.width, height), color, ref draggingSlider, ref draggingWheel) is Color newColor)
                {
                    setColor(newColor);
                    pawn?.Drawer.renderer.SetAllGraphicsDirty();
                }
                curY += height + 12;
            }
        }

        private void ColorSelecterExtraOnGUI(Color color, Rect boxRect)
        {
            Texture2D texture2D = null;
            TaggedString taggedString = null;
            if (texture2D != null)
            {
                Rect position = boxRect.ContractedBy(4f);
                GUI.color = Color.black.ToTransparent(0.2f);
                GUI.DrawTexture(new Rect(position.x + 2f, position.y + 2f, position.width, position.height), texture2D);
                GUI.color = Color.white.ToTransparent(0.8f);
                GUI.DrawTexture(position, texture2D);
                GUI.color = Color.white;
            }
            if (!taggedString.NullOrEmpty())
            {
                TooltipHandler.TipRegion(boxRect, taggedString);
            }
        }
    }
}
