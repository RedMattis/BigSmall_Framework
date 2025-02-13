using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class NalsToggles
    {
        private static bool? faLoaded = null;
        public static bool FALoaded => faLoaded ??= ModsConfig.IsActive("Nals.FacialAnimation");

        private static PawnRenderNode GetHead(Pawn pawn)
        {
            var root = pawn?.Drawer?.renderer?.renderTree?.rootNode;
            if (root is null) return null;

            return root.children?.Where(x => x.Props.tagDef == PawnRenderNodeTagDefOf.Head).FirstOrDefault();
        }

        public static void ToggleNalsStuff(Pawn pawn, FacialAnimDisabler options)
        {
            return;
            if (FALoaded == true && GetHead(pawn) is PawnRenderNode head && !head.children.NullOrEmpty())
            {
                foreach (var child in head.children)
                {
                    Log.Message($"Child: {child.Worker.GetType()}");
                    if (child.Worker.GetType().ToString().Contains("NLFacial"))
                    {
                        Log.Message($"Child: {child}");
                        if (child.ToString().Contains("HeadControllerComp"))
                        {
                            child.debugEnabled = !options.headName.Contains("NOT_");
                            child.requestRecache = true;
                        }
                        else if (child.ToString().Contains("SkinControllerComp"))
                        {
                            child.debugEnabled = !options.skinName.Contains("NOT_");
                            child.requestRecache = true;
                        }
                        else if (child.ToString().Contains("BrowControllerComp"))
                        {
                            child.debugEnabled = !options.browName.Contains("NOT_");
                            child.requestRecache = true;
                        }
                        else if (child.ToString().Contains("LidControllerComp"))
                        {
                            child.debugEnabled = !options.lidName.Contains("NOT_");
                            child.requestRecache = true;
                        }
                        else if (child.ToString().Contains("EyeballControllerComp"))
                        {
                            child.debugEnabled = !options.eyeballName.Contains("NOT_");
                            child.requestRecache = true;
                        }
                        else if (child.ToString().Contains("MouthControllerComp"))
                        {
                            child.debugEnabled = !options.mouthName.Contains("NOT_");
                            child.requestRecache = true;
                        }
                    }
                }
            }
        }

        // Experimental.

        //[HarmonyTranspiler]
        //[HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.DebugEnabled), MethodType.Getter)]
        //public static IEnumerable<CodeInstruction> DebugEnabledTranspiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var codes = instructions.ToList();
        //    // Just insert the instructions right at the start, this should render the rest of the method useless unless someone else patches it.
        //    codes.InsertRange(0,
        //    [
        //        new CodeInstruction(OpCodes.Ldarg_0),
        //        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderNode), nameof(PawnRenderNode.debugEnabled))),
        //        new CodeInstruction(OpCodes.Ret)
        //    ]);
        //    return codes.AsEnumerable();
        //}
    }
}