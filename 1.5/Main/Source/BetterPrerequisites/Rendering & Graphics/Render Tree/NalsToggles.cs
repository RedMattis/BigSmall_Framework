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
    public static class NalsToggles
    {
        private static bool? faLoaded = null;
        public static bool FALoaded => faLoaded ??= ModsConfig.IsActive("Nals.FacialAnimation");
        public static void ApplyNLPatches(Harmony harmony)
        {
            harmony.Patch(
                AccessTools.PropertyGetter(typeof(PawnRenderNode), nameof(PawnRenderNode.DebugEnabled)),
                transpiler: new HarmonyMethod(DebugEnabledTranspiler));
            harmony.Patch(
                AccessTools.Method(typeof(PawnRenderTree), "InitializeAncestors"),
                postfix: new HarmonyMethod(InitializeAncestorsPostfix));
        }

        private static PawnRenderNode GetHead(Pawn pawn)
        {
            var root = pawn?.Drawer?.renderer?.renderTree?.rootNode;
            if (root is null) return null;

            return root.children?.Where(x => x.Props.tagDef == PawnRenderNodeTagDefOf.Head).FirstOrDefault();
        }

        public static void ToggleNalsStuff(Pawn pawn, FacialAnimDisabler options)
        {
            if (FALoaded == true && GetHead(pawn) is PawnRenderNode head && !head.children.NullOrEmpty())
            {
                foreach (var child in head.children)
                {
                    if (child.Worker.GetType().ToString().Contains("NLFacial"))
                    {
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

        public static IEnumerable<CodeInstruction> DebugEnabledTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            // Just insert the instructions right at the start, this should render the rest of the method useless unless someone else patches it.
            codes.InsertRange(0,
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderNode), nameof(PawnRenderNode.debugEnabled))),
                new CodeInstruction(OpCodes.Ret)
            ]);
            return codes.AsEnumerable();
        }
        public static void InitializeAncestorsPostfix(PawnRenderTree __instance)
        {
            if (HumanoidPawnScaler.GetCacheUltraSpeed(__instance.pawn, canRegenerate: false)
                 is BSCache cache && cache.facialAnimDisabler is FacialAnimDisabler fa)
            {
                ToggleNalsStuff(__instance.pawn, fa);
            }
        }
    }
}