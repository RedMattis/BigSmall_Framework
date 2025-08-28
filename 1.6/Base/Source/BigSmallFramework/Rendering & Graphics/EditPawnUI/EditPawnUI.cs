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
            Gene,
            Hediff,
        }
        private readonly ILoadReferenceable target;
        private static readonly Vector2 ButtonSize = new(200f, 40f);

        private int selectedTab = 0;
        private EditMode activeTab = EditMode.Thing;
        private List<EditMode> tabsWithContent = [];
        private List<ThingGData> EditableThings = [];
        private Pawn pawn = null;

        private Vector2 scrollPosition = Vector2.zero;
        private float scrollViewHeight = 0f;

        private List<Color> colorCache;
        public List<Color> GetPickableColors(Pawn pawn, bool force=false)
        {
            if (force || colorCache == null)
            {
                colorCache =
                [
                    Color.white,
                    new(0.08f, 0.08f, 0.08f),

                    new(0.08f, 0.8f, 0.08f),
                    new(0.15f, 0.15f, 0.15f),
                    new(0.9f, 0.9f, 0.9f),

                    new(0.5f, 0.5f, 0.25f),
                    new(0.9f, 0.9f, 0.5f),
                    new(0.9f, 0.8f, 0.5f),

                    new(0.45f, 0.2f, 0.2f),
                    new(0.5f, 0.25f, 0.25f),
                    new(0.9f, 0.5f, 0.5f),

                    new(0.15f, 0.28f, 0.43f),

                    new(0.98f, 0.92f, 0.84f),
                    new(0.87f, 0.96f, 0.91f),
                    new(0.94f, 0.87f, 0.96f),
                    new(0.96f, 0.87f, 0.87f),
                    new(0.87f, 0.94f, 0.96f),
                ];
                if (ModsConfig.IdeologyActive)
                {
                    if (pawn?.Ideo != null && !Find.IdeoManager.classicMode)
                    {
                        colorCache.Add(pawn.Ideo.ApparelColor);
                    }
                    foreach (var ideo in Find.World.ideoManager.IdeosListForReading)
                    {
                        if (!colorCache.Any((Color c) => ideo.ApparelColor.IndistinguishableFrom(c)))
                        {
                            colorCache.Add(ideo.ApparelColor);
                        }
                    }
                }
                foreach (var color in Find.World.factionManager.AllFactions.Select(x => x.Color))
                {
                    if (!colorCache.Any((Color c) => color.IndistinguishableFrom(c)))
                    {
                        colorCache.Add(color);
                    }
                }
                if (ModsConfig.IdeologyActive && pawn?.story != null && !pawn.DevelopmentalStage.Baby() && pawn.story.favoriteColor != null
                    && !colorCache.Any((Color c) => pawn.story.favoriteColor.color.IndistinguishableFrom(c)))
                {
                    colorCache.Add(pawn.story.favoriteColor.color);
                }
                foreach (ColorDef colDef in DefDatabase<ColorDef>.AllDefs
                    .Where((ColorDef x) => x.colorType == ColorType.Ideo || x.colorType == ColorType.Misc))
                {
                    if (!colorCache.Any((Color x) => x.IndistinguishableFrom(colDef.color)))
                    {
                        colorCache.Add(colDef.color);
                    }
                }
                colorCache.SortByColor((Color x) => x);
            }
            return colorCache;
            
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.x = windowRect.x - InitialSize.x;
        }

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
                            EditableThings.Add(new ThingGData(pawn, EditMode.Gene, cg, def: gene.def));
                            tabsWithContent.Add(EditMode.Gene);
                        }
                    }
                }
                foreach(var hediff in pawn.health.hediffSet.hediffs)
                {
                    foreach(var cg in hediff.def.ExtensionsOnDef<HasCustomizableGraphics, HediffDef>())
                    {
                        EditableThings.Add(new ThingGData(pawn, EditMode.Hediff, cg, def: hediff.def));
                        tabsWithContent.Add(EditMode.Hediff);
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

            Text.Font = GameFont.Medium;
            Rect titleRect = new(inRect)
            {
                height = Text.LineHeight * 2
            };
            Widgets.Label(titleRect, "BS_EditThing_Title".Translate(target));

            Text.Font = GameFont.Small;
            
            Rect tabRect = new(inRect.x, titleRect.y + titleRect.height, inRect.width, tabHeight);
            Rect contentRect = new(inRect.x, inRect.y + contentHeight, inRect.width, inRect.height - contentHeight);

            // Tab stuff
            var tabKeys = GetTabKeys();
            int tabCount = tabKeys.Length;
            var tabs = new List<TabRecord>();
            for (int i = 0; i < tabCount; i++)
            {
                int tabIndex = i;
                tabs.Add(new TabRecord(tabKeys[i].Translate(), () => selectedTab = tabIndex, selectedTab == tabIndex));
            }
            TabDrawer.DrawTabs(tabRect, tabs);

            activeTab = tabsWithContent[selectedTab];

            Rect innerRect = contentRect.ContractedBy(15f);
            DrawMainUI(innerRect, activeTab);
            //Close();
        }

        private void DrawMainUI(Rect rect, EditMode tab)
        {
            rect = rect.ContractedBy(10);

            Rect scrollViewRect = new Rect(rect)
            {
                height = scrollViewHeight
            };
            scrollViewRect.width -= 16f;

            Widgets.BeginScrollView(rect, ref scrollPosition, scrollViewRect);
            float curY = rect.y + 10;

            foreach (var graphicData in EditableThings)
            {
                var gMode = graphicData.tabCat;
                if (gMode != tab) continue;

                var thing = graphicData.thing;
                var hasCustomDef = graphicData.cgExt;
                var cdTag = graphicData.cgExt?.tag;

                if (thing is Pawn pawn)
                {
                    switch (gMode)
                    {
                        case EditMode.Thing:
                            curY = DrawThingSettings(rect, curY, pawn);
                            break;
                        case EditMode.Apparel:
                            curY = DrawApparelSettings(rect, curY, thing, hasCustomDef, pawn);
                            break;
                        case EditMode.Gene:
                            curY = DrawGeneSettings(rect, curY, graphicData, thing, hasCustomDef, cdTag, pawn);
                            break;
                        case EditMode.Hediff:
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
                DrawTitle("BS_Custom".Translate(), rect, ref curY);
                DrawColorPicker(pawn, pawn.DrawColor, rect, ref curY, (Color col) => pawn.SetCustomColorA(col));
                DrawColorPicker(pawn, pawn.DrawColor, rect, ref curY, (Color col) => pawn.SetCustomColorB(col));
                DrawColorPicker(pawn, pawn.DrawColor, rect, ref curY, (Color col) => pawn.SetCustomColorC(col));
                return curY;
            }

            float DrawApparelSettings(Rect rect, float curY, Thing thing, HasCustomizableGraphics hasCustomDef, Pawn pawn)
            {
                var apparel = thing as Apparel;
                DrawTitle(apparel.def.LabelCap, rect, ref curY);
                DrawApparelIcon(apparel, rect, ref curY);
                if (hasCustomDef.colorA)
                {
                    DrawColorPicker(pawn, apparel.GetCustomColorA(), rect, ref curY, (Color col) => thing.SetCustomColorA(apparel.DrawColor = col));
                }
                else
                {
                    DrawColorPicker(pawn, apparel.DrawColor, rect, ref curY, (Color col) => apparel.DrawColor = col);
                }
                if (hasCustomDef.colorB)
                {
                    DrawColorPicker(pawn, apparel.GetCustomColorB(), rect, ref curY, (Color col) => thing.SetCustomColorB(col));
                }
                if (hasCustomDef.colorC)
                {
                    DrawColorPicker(pawn, apparel.GetCustomColorC(), rect, ref curY, (Color col) => thing.SetCustomColorC(col));
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
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 0), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 0, col));
                    }
                    if (hasCustomDef.colorB)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 1), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 1, col));
                    }
                    if (hasCustomDef.colorC)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 2), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 2, col));
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
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 0), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 0, col));
                    }
                    if (hasCustomDef.colorB)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 1), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 1, col));
                    }
                    if (hasCustomDef.colorC)
                    {
                        DrawColorPicker(pawn, pawn.GetTagColor(cdTag, 2), rect, ref curY, (Color col) => thing.SetTagColor(cdTag, 2, col));
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
            GUI.color = Color.white;
            var iconRect = new Rect(rect.x, curY, 64, 64);
            Widgets.DrawTextureFitted(iconRect, apparel.Graphic.MatSouth.mainTexture, 1f);
            curY += iconRect.height + 12;
        }

        private void DrawGeneIcon(GeneDef geneDef, Rect rect, ref float curY)
        {
            if (geneDef.Icon != null)
            {
                GUI.color = Color.white;
                var iconRect = new Rect(rect.x, curY, 64, 64);
                Widgets.DrawTextureFitted(iconRect, geneDef.Icon, 1f);
                curY += rect.height + 24;
            }
        }

        private void DrawColorPicker(Pawn pawn, Color? currClrNullable, Rect rect, ref float curY, Action<Color> setColor)
        {
            if (currClrNullable == null)
            {
                var overrideRect = new Rect(rect.x, curY, ButtonSize.x, ButtonSize.y);
                if (Widgets.ButtonText(overrideRect, "BS_OverrideCustomizableColor".Translate()))
                {
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    setColor(Color.cyan);
                    pawn?.Drawer.renderer.SetAllGraphicsDirty();
                }
                curY = overrideRect.y + overrideRect.yMax + 14f;
                return;
            }
            else
            {
                Color currClr = currClrNullable.Value;
                Color color = currClr;

                Rect colorRect = new(rect.x, curY, rect.width, 140);
                curY += colorRect.height + 0f;

                Widgets.ColorSelector(colorRect, ref color, GetPickableColors(pawn), out float height, null, 22, 2, ColorSelecterExtraOnGUI);
                float num2 = rect.x;
                if (pawn?.Ideo is Ideo pawnIdeo && !Find.IdeoManager.classicMode)
                {
                    colorRect = new Rect(num2, curY, 160f, 24f);
                    if (Widgets.ButtonText(colorRect, "SetIdeoColor".Translate()))
                    {
                        color = pawnIdeo.ApparelColor;
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                    num2 += 110f;
                }
                if (!color.IndistinguishableFrom(currClr))
                {
                    setColor(color);
                    pawn?.Drawer.renderer.SetAllGraphicsDirty();
                }
                curY += 32f;
            }
            
        }

        private void ColorSelecterExtraOnGUI(Color color, Rect boxRect)
        {
            Texture2D texture2D = null;
            TaggedString taggedString = null;
            bool flag = Mouse.IsOver(boxRect);
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
