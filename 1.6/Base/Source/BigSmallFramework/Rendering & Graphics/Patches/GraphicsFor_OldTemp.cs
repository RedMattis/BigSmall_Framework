//using BetterPrerequisites;
//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Verse;

//namespace BigAndSmall
//{
//    [HarmonyPatch]
//    public static class GraphicsForPatches
//    {
//        // These probably work fine, but I don't actually USE them for anything yet, so they're commented out. Needs performance-testing, etc.

//        [HarmonyPatch(typeof(PawnRenderNode_Body), "GraphicFor")]
//        [HarmonyPostfix]
//        public static void PawnRenderNode_Body_Postfix(PawnRenderNode_Body __instance, Pawn pawn, ref Graphic __result)
//        {
//            if (HumanoidPawnScaler.GetCacheUltraSpeed(pawn) is BSCache cache)
//            {
//            }

//            if (__instance != null && ModsConfig.BiotechActive && pawn?.RaceProps?.Humanlike == true && pawn?.genes != null)
//            {
//                var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
//                List<GeneExtension> geneExts = activeGenes
//                        .Where(x => x?.def?.modExtensions != null && x.def.modExtensions.Any(y => y.GetType() == typeof(GeneExtension)))?
//                        .Select(x => x.def.GetModExtension<GeneExtension>()).ToList();

//                foreach (var geneExt in geneExts)
//                {
//                    if (pawn.Drawer.renderer.CurRotDrawMode != RotDrawMode.Dessicated)
//                    {
//                        if (geneExt.hideBody)
//                        {
//                            __result = GraphicDatabase.Get<Graphic_Multi>("UI/EmptyImage", ShaderUtility.GetSkinShader(pawn), Vector2.one, pawn.story.SkinColor);
//                        }
//                    }
//                }
//            }
//        }
//        [HarmonyPatch(typeof(PawnRenderNode_Head), "GraphicFor")]
//        [HarmonyPostfix]
//        public static void PawnRenderNode_Head_Postfix(PawnRenderNode_Head __instance, Pawn pawn, ref Graphic __result)
//        {
//            if (__instance != null && ModsConfig.BiotechActive && pawn?.RaceProps?.Humanlike == true && pawn?.genes != null)
//            {
//                var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
//                List<GeneExtension> geneExts = activeGenes
//                        .Where(x => x?.def?.modExtensions != null && x.def.modExtensions.Any(y => y.GetType() == typeof(GeneExtension)))?
//                        .Select(x => x.def.GetModExtension<GeneExtension>()).ToList();

//                foreach (var geneExt in geneExts)
//                {
//                    if (pawn.Drawer.renderer.CurRotDrawMode != RotDrawMode.Dessicated)
//                    {
//                        if (geneExt.hideHead)
//                        {
//                            __result = GraphicDatabase.Get<Graphic_Multi>("UI/EmptyImage", ShaderUtility.GetSkinShader(pawn), Vector2.one, pawn.story.SkinColor);
//                        }
//                    }
//                }
//            }
//        }
//    }




//}