using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

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

}
