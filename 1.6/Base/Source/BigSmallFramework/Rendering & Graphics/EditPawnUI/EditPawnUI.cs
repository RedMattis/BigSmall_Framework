using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib;
using static HarmonyLib.Code;
using System.Reflection.Emit;
using System.Security.Cryptography;

namespace BigAndSmall
{

    [HarmonyPatch]
    public static class CharacterCardUtilityUIPatch
    {
        public static Texture2D ColorPawn_Icon { get { return field ??= ContentFinder<Texture2D>.Get("BS_UI/ColorPawn"); } }
        public static Texture2D Mechanical_Icon { get { return field ??= ContentFinder<Texture2D>.Get("BS_Traits/BS_Mechanical"); } }
        public static Texture2D AlienIcon_Icon { get { return field ??= ContentFinder<Texture2D>.Get("BS_Traits/Alien"); } }
        public static readonly Color StackElementBackground = new Color(1f, 1f, 1f, 0.1f);
        //public static string BSShowPawnRaceTooltip {get { return field ??= "BS_ShowPawnRaceTooltip".Translate(); } }
        public static string BSEditPawnTooltip { get { return field ??= "BS_EditPawnTooltip".Translate(); } }

        [HarmonyPatch(typeof(CharacterCardUtility), "DoTopStack")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DoTopStack_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var found = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!found && codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 44f)
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharacterCardUtilityUIPatch), nameof(InsertPawnMutationWindow)));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharacterCardUtilityUIPatch), nameof(InsertEditPawnApperanceWindow)));
                }
                yield return codes[i];
            }
            if (!found)
            {
                Log.Error("[BigAndSmall] Failed to apply CharacterCardUtilityUI transpiler.");
            }
        }

        public static void InsertEditPawnApperanceWindow(Pawn pawn)
        {
            var tmpElms = CharacterCardUtility.tmpStackElements;
            tmpElms.Add(new GenUI.AnonymousStackElement
            {
                drawer = delegate (Rect inRect)
                {
                    var backColor = StackElementBackground;
                    GUI.color = backColor;
                    Widgets.DrawBox(inRect);
                    GUI.color = Color.white;
                    //Widgets.DrawRectFast(inRect, backColor);

                    Widgets.DrawTextureFitted(inRect, ColorPawn_Icon, 1);
                    if (Widgets.ButtonInvisible(inRect))
                    {
                        Find.WindowStack.Add(new EditPawnWindow(pawn));
                    }
                    //GUI.color = Color(;
                    //GUI.color = Color.white;
                    TooltipHandler.TipRegion(inRect, BSEditPawnTooltip);
                    //Widgets.DrawBox(inRect);
                    if (Mouse.IsOver(inRect))
                    {
                        Widgets.DrawHighlight(inRect);
                    }
                },
                width = 22f
            });
        }

        public static void InsertPawnMutationWindow(Pawn pawn)
        {
            if (pawn.def == ThingDefOf.Human) return;
            var tmpElms = CharacterCardUtility.tmpStackElements;
            tmpElms.Add(new GenUI.AnonymousStackElement
            {
                drawer = delegate (Rect inRect)
                {
                    var backColor = StackElementBackground;
                    GUI.color = backColor;
                    Widgets.DrawBox(inRect);
                    GUI.color = Color.white;
                    //Widgets.DrawRectFast(inRect, backColor);

                    Widgets.DrawTextureFitted(inRect, Mechanical_Icon, 1);
                    if (Widgets.ButtonInvisible(inRect))
                    {
                        Find.WindowStack.Add(new Dialog_ViewMutations(pawn));
                    }
                    //GUI.color = Color(;
                    //GUI.color = Color.white;
                    TooltipHandler.TipRegion(inRect, () => "BS_ShowPawnRaceTooltip".Translate(pawn.LabelCap, pawn.def.LabelCap).Resolve(), 1289589431);
                    //Widgets.DrawBox(inRect);
                    if (Mouse.IsOver(inRect))
                    {
                        Widgets.DrawHighlight(inRect);
                    }
                },
                width = 22f
            });
        }
    }


    public class EditPawnWindow : Window
    {
        public class ThingGData(Thing thing, WindowTab editMode, Def def = null)
        {
            public const string DEFAULT = "DEFAULT";
            readonly public Thing thing = thing;

            public Dictionary<string, SectionData> customData = [];
            readonly public Def def = def;
            readonly public WindowTab editMode = editMode;
            public SectionData TryGetGeneric => customData.TryGetValue(DEFAULT, out var data) ? data : null;
            public SectionData GetOrAddGeneric() => customData.TryGetValue(DEFAULT, out var data) ? data : (customData[DEFAULT] = new SectionData(null, editMode));
        }

        public class SectionData(FlagString flag, WindowTab editMode)
        {
            public WindowTab tab = editMode;
            public readonly FlagString flag = flag;
            public bool colorA = false;
            public bool colorB = false;
            public bool colorC = false;

            public bool HasMultipleClrs => (colorA ? 1 : 0) + (colorB ? 1 : 0) + (colorC ? 1 : 0) > 1;
        }

        public enum WindowTab
        {
            Thing,
            Apparel,
            CustomTag,
        }
        private readonly ILoadReferenceable target;
        private static readonly Vector2 ButtonSize = new(200f, 40f);

        private int selectedTab = 0;
        private WindowTab activeTab = WindowTab.Thing;
        private readonly List<WindowTab> tabsWithContent = [];
        private readonly Dictionary<Thing, ThingGData> EditableThings = [];
        private readonly Dictionary<(string, FlagString), SectionData> CSectionBuilder = [];
        private readonly Dictionary<string, List<SectionData>> CustomSections = [];
        private readonly Pawn pawn = null;
        private readonly Thing thing = null;

        private Vector2 scrollPosition = Vector2.zero;
        private float scrollViewHeight = 0f;
        private const string NONE = "NONE";
        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.x = windowRect.x - InitialSize.x;
        }

        public override Vector2 InitialSize => new(600f, 800f);


        private static WindowTab EditModeFrom(HasCustomizableGraphics cg, WindowTab @default) => FlagStringData.DataFor(cg?.Flag).displayTab ?? @default;
        
        public EditPawnWindow(ILoadReferenceable target)
        {
            thing = target as Thing;
            if (thing == null)
            {
                return;
            }
            pawn = target as Pawn;
            _tabs = null;
            tabsWithContent = [WindowTab.Thing];
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

            AddEditable(null, WindowTab.Thing);
            if (pawn != null)
            {
                var wornApparel = pawn.apparel?.WornApparel is List<Apparel> apparel ? apparel: [] ;
                foreach (var item in wornApparel)
                {
                    var customGfx = item.def.ExtensionsOnDef<HasCustomizableGraphics, ThingDef>();
                    if (customGfx.Count != 0)
                    {
                        HasCustomizableGraphics first = customGfx[0];
                        AddEditable(customGfx, WindowTab.Apparel, item);
                    }
                    else if (item.WornGraphicPath != null)
                    {
                        AddEditable(null, WindowTab.Apparel, item);
                        tabsWithContent.Add(WindowTab.Apparel);
                    }
                    else
                    {
                        Log.Message($"[BigAndSmall] Tried to edit apparel {item} but it has no graphics extension or worn graphic path.");
                    }
                }

                if (ModsConfig.BiotechActive && pawn.genes != null)
                {
                    foreach (var gene in GeneHelpers.GetAllActiveGenes(pawn))
                    {
                        var customGfx = gene.def.ExtensionsOnDef<HasCustomizableGraphics, GeneDef>();
                        if (customGfx.Count != 0)
                        {
                            AddEditable(customGfx, WindowTab.CustomTag);
                        }
                    }
                }
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    var customGfx = hediff.def.ExtensionsOnDef<HasCustomizableGraphics, HediffDef>();
                    if (customGfx.Count != 0)
                    {
                        AddEditable(customGfx, WindowTab.CustomTag);
                    }
                }
            }
            
            tabsWithContent = [.. tabsWithContent.Distinct()];
            CustomSections = CSectionBuilder.Values.GroupBy(x => x.flag.CustomCategory ?? NONE).ToDictionary(g => g.Key, g => g.ToList());
        }

        private void AddEditable(List<HasCustomizableGraphics> cgList, WindowTab defaultMode, Thing overrideThing=null)
        {
            Thing target = overrideThing ?? thing;

            ThingGData data = GetMakeMainSection(defaultMode, target);
            if (cgList.NullOrEmpty())
            {
                return;
            }
            cgList = [.. cgList.OrderByDescending(cg => cg?.Flag == null ? 1 : 0)];
            foreach (var cg in cgList)
            {
                if (cg?.Flag is FlagString flag)
                {
                    var catName = flag.CustomCategory ?? NONE;
                    Log.Message($"[BigAndSmall] Adding custom graphics for {target} with flag {flag} (cat: {catName})");
                    if (!CSectionBuilder.TryGetValue((catName, flag), out var sData))
                    {
                        var tab = cg.Flag.DisplayTab ?? defaultMode;
                        tabsWithContent.Add(tab);
                        CSectionBuilder[(catName, flag)] = sData = new SectionData(flag, tab);
                    }
                    sData.colorA |= cg.colorA;
                    sData.colorB |= cg.colorB;
                    sData.colorC |= cg.colorC;
                }
                else
                {
                    PopulateShared(cg, data);

                }
            }

            static void PopulateShared(HasCustomizableGraphics cg, ThingGData data)
            {
                var generic = data.GetOrAddGeneric();
                generic.colorA |= cg.colorA;
                generic.colorB |= cg.colorB;
                generic.colorC |= cg.colorC;
            }

            ThingGData GetMakeMainSection(WindowTab defaultMode, Thing target)
            {
                tabsWithContent.Add(defaultMode);
                if (!EditableThings.TryGetValue(target, out var data))
                {
                    EditableThings[target] = data = new ThingGData(target, defaultMode);
                    var generic = data.GetOrAddGeneric();
                    if (target is Pawn && defaultMode == WindowTab.Thing)
                    {
                        generic.colorA = true;
                        generic.colorB = true;
                        generic.colorC = true;
                    }
                }
                return data;
            }
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
                if (tabKeys[i] == $"BS_Tab_{nameof(WindowTab.Thing)}")
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

        public List<SectionData> TryFetchAllByCustomCat(string cat) => CustomSections.Any() && CustomSections.TryGetValue(cat, out var list) ? list : [];

        public bool IsSpecialCategory(string cat) => cat.Equals("Hair") || cat.Equals("Skin");

        private void DrawMainUI(Rect rect, WindowTab tab)
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

            foreach ((var thing, var data) in EditableThings)
            {
                var targetTab = data.editMode;
                if (targetTab != tab) continue;
                curY = MakeStandardSection(rect, curY, thing, data, targetTab);
                foreach (var custom in data.customData.Values)
                {
                    if (custom == data.TryGetGeneric) continue;
                    curY = DrawCustom(rect, curY, thing, custom);
                }
            }

            foreach (var customCat in CustomSections.Keys)
            {
                if (IsSpecialCategory(customCat)) continue;
                var catData = CustomSections[customCat];
                if (catData.Count == 0 || catData.All(x => x.tab != tab)) continue;
                foreach (var section in catData)
                {
                    if (section.tab != tab) continue;
                    if (customCat != NONE)
                    {
                        DrawTitle(section.flag.CustomCategory, rect, ref curY);
                    }
                    curY = DrawCustom(rect, curY, thing, section);
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = curY - rect.y;
            }

            Widgets.EndScrollView();

        }

        private float MakeStandardSection(Rect rect, float curY, Thing thing, ThingGData data, WindowTab tab)
        {
            var pawn = thing as Pawn;
            var generic = data.TryGetGeneric;
            DrawTitle(thing.LabelCap, rect, ref curY);

            if (thing is Pawn && tab == WindowTab.Thing)
            {
                curY = PawnDefaulSection(rect, curY, thing);
            }
            else if (thing is Apparel apparel && tab == WindowTab.Apparel)
            {
                DrawApparelIcon(apparel, rect, ref curY, apparel.DrawColor);
            }
            curY = MakeSharedCustomColorSection(rect, curY, thing, generic);

            return curY;

            float PawnDefaulSection(Rect rect, float curY, Thing thing)
            {
                var skinColor = pawn.story?.SkinColor ?? Color.white;
                DrawColorPicker(skinColor, rect, ref curY, (Color col) => pawn.story.skinColorOverride = col, title: "BS_Skin".Translate());
                foreach (var customSkin in TryFetchAllByCustomCat("Skin"))
                {
                    curY = DrawCustom(rect, curY, thing, customSkin);
                }
                var hairColor = pawn.story?.HairColor ?? Color.white;
                DrawColorPicker(hairColor, rect, ref curY, (Color col) => pawn.story.HairColor = col, title: "BS_Hair".Translate());
                foreach (var customHair in TryFetchAllByCustomCat("Hair"))
                {
                    curY = DrawCustom(rect, curY, thing, customHair);
                }

                return curY;
            }

            float MakeSharedCustomColorSection(Rect rect, float curY, Thing inThing, SectionData data)
            {
                var isBaseColorable = inThing.HasComp<CompColorable>();
                if (data?.colorC == true)
                {
                    
                    if (isBaseColorable)
                    {
                        if (inThing.GetCustomColorA() == null)
                        {
                            DrawColorPicker(inThing.DrawColor, rect, ref curY, (Color col) => inThing.DrawColor = col,
                                "BS_BaseColor".Translate());
                        }
                        DrawColorPicker(inThing.GetCustomColorA(), rect, ref curY, (Color col) => inThing.SetCustomColorA(inThing.DrawColor = col),
                            $"{"BS_Color".Translate()} {"BS_Primary".Translate()}");
                    }
                    else
                    {
                        DrawColorPicker(inThing.GetCustomColorA(), rect, ref curY, (Color col) => inThing.SetCustomColorA(col),
                            $"{"BS_Color".Translate()} {"BS_Primary".Translate()}");
                    }
                }
                else
                {
                    DrawColorPicker(inThing.DrawColor, rect, ref curY, (Color col) => inThing.DrawColor = col);
                }
                if (data?.colorB == true)
                {
                    DrawColorPicker(inThing.GetCustomColorB(), rect, ref curY, (Color col) => inThing.SetCustomColorB(col),
                        $"{"BS_Color".Translate()} {"BS_Secondary".Translate()}");
                }
                if (data?.colorC == true)
                {
                    DrawColorPicker(inThing.GetCustomColorC(), rect, ref curY, (Color col) => inThing.SetCustomColorC(col),
                        $"{"BS_Color".Translate()} {"BS_Tertiary".Translate()}");
                }

                return curY;
            }
        }

        public string ColorIdxLabel(int idx) => idx switch
        {
            0 => "BS_PrimaryColor".Translate(),
            1 => "BS_SecondaryColor".Translate(),
            2 => "BS_TertiaryColor".Translate(),
            _ => "BS_Color".Translate() + " " + $"{idx} ",
        };
        private float DrawCustom(Rect rect, float curY, Thing thing, SectionData data)
        {
            bool flagManyClr = data.HasMultipleClrs;
            if (data.colorA)
            {
                int idx = 0;
                if (data.flag is FlagString flag)
                {
                    DrawColorPicker(thing.GetFlagColor(flag, idx), rect, ref curY, (Color col) => thing.SetFlagColor(flag, idx, col),
                        flagManyClr ? $"{flag.Label} {ColorIdxLabel(idx)}" : $"{flag.Label}");
                }
                else
                {
                    Log.WarningOnce($"[BigAndSmall] Tried to draw custom color for {thing} with null flag.", 7123745);
                }
            }
            if (data.colorB)
            {
                int idx = 1;
                if (data.flag is FlagString flag)
                {
                    DrawColorPicker(thing.GetFlagColor(flag, idx), rect, ref curY, (Color col) => thing.SetFlagColor(flag, idx, col),
                        flagManyClr ? $"{flag.Label} {ColorIdxLabel(idx)}" : $"{flag.Label}");
                }
                else
                {
                    Log.WarningOnce($"[BigAndSmall] Tried to draw custom color for {thing} with null flag.", 7123745);
                }
            }
            if (data.colorC)
            {
                int idx = 2;
                if (data.flag is FlagString flag)
                {
                    DrawColorPicker(thing.GetFlagColor(flag, idx), rect, ref curY, (Color col) => thing.SetFlagColor(flag, idx, col),
                        flagManyClr ? $"{flag.Label} {ColorIdxLabel(idx)}" : $"{flag.Label}");
                }
                else
                {
                    Log.WarningOnce($"[BigAndSmall] Tried to draw custom color for {thing} with null flag.", 7123745);
                }
            }

            return curY;
        }

        private void DrawTitle(string titleText, Rect rect, ref float curY)
        {
            Text.Font = GameFont.Medium;
            curY += 4;
            Rect titleRect = new(rect)
            {
                y = curY,
                height = Text.LineHeight * 1.2f
            };
            Widgets.Label(titleRect, titleText.CapitalizeFirst());
            Text.Font = GameFont.Small;
            curY += titleRect.height;
        }

        private void DrawApparelIcon(Apparel apparel, Rect rect, ref float curY, Color color)
        {
            GUI.color = color;
            const int size = 64;
            var iconRect = new Rect(rect.x, curY, size, size);

            var scaledIconRect = new Rect(iconRect);
            if (apparel.Graphic?.drawSize != null)
            {
                scaledIconRect = scaledIconRect.ExpandedBy(0.5f * size * (apparel.Graphic.drawSize.x - 1.0f));
            }
            Widgets.DrawTextureFitted(scaledIconRect, apparel.Graphic.MatSouth.mainTexture, 1f);

            // Looks super-fancy, but it doesn't stay inside the window.
            //Widgets.DrawTextureFitted(scaledIconRect, apparel.Graphic.MatSouth.mainTexture, 1f, apparel.Graphic.MatSouth, alpha:1);
            GUI.color = Color.white;
            curY += iconRect.height + 12;
        }

        private void DrawGeneTitleArea(GeneDef geneDef, Rect rect, ref float curY, Color color)
        {
            DrawTitle(geneDef.LabelCap, rect, ref curY);
            DrawGeneIcon(geneDef, rect, ref curY, color);
        }

        private void DrawGeneIcon(GeneDef geneDef, Rect rect, ref float curY, Color color)
        {
            if (geneDef.Icon != null)
            {
                GUI.color = color;
                //GUI.color = Color.white;
                var iconRect = new Rect(rect.x, curY, 64, 64);
                Widgets.DrawTextureFitted(iconRect.ExpandedBy(6), geneDef.Icon, 1f);
                curY += rect.height + 24;
                GUI.color = Color.white;
            }
        }

        public bool draggingSlider = false;
        public bool draggingWheel = false;
        private void DrawColorPicker(Color? currClrNullable, Rect rect, ref float curY, Action<Color> setColor,
            string title = null)
        {
            if (currClrNullable == null)
            {
                string overrideClrStr = ("BS_Enable".Translate() + " " + title).CapitalizeFirst();
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
