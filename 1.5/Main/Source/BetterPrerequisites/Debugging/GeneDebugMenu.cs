using BetterPrerequisites;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall.Debugging
{
    [HarmonyPatch]
    public static class DebugUIPatches
    {
        [HarmonyPatch(typeof(GeneUIUtility), "DoDebugButton")]
        [HarmonyPostfix]
        public static void DoDebugButton_Postfix(ref Rect buttonRect, Thing target, GeneSet genesOverride)
        {
            if (target is not Pawn)
            {
                return;
            }
            Pawn pawn = target as Pawn;
            float widthOfPrevious = buttonRect.size.x;
            buttonRect = new Rect(buttonRect.x - widthOfPrevious - 10, buttonRect.y, buttonRect.width, buttonRect.height);
            if (!Widgets.ButtonText(buttonRect, "Big & Small"))
            {
                return;
            }

            List<FloatMenuOption> list =
            [
                new FloatMenuOption("Force-Set RaceDef", delegate
                {
                    List<DebugMenuOption> list = [];
                    foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
                    {
                        list.Add(new DebugMenuOption(def.defName + " (force)", DebugMenuOptionMode.Action, delegate
                        {
                            RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 999, force: true, permitFusion:false);
                        }));
                    }
                    foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
                    {
                        list.Add(new DebugMenuOption(def.defName, DebugMenuOptionMode.Action, delegate
                        {
                            RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 100, force: false);
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
                }),
                new FloatMenuOption("Set to exact xenotype (also sets race)", delegate
                {
                    List<DebugMenuOption> xenotypeLister = [];
                    foreach (XenotypeDef allDef in DefDatabase<XenotypeDef>.AllDefs)
                    {
                        XenotypeDef xenotype = allDef;
                        xenotypeLister.Add(new DebugMenuOption(xenotype.LabelCap, DebugMenuOptionMode.Action, delegate
                        {
                            GeneHelpers.RemoveAllGenesSlow_ExceptColor(pawn);
                            pawn.genes.SetXenotype(xenotype);
                            pawn.TrySwapToXenotypeThingDef();
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(xenotypeLister));
                }),
                new FloatMenuOption("Apply xenotype.", delegate
                {
                    List<DebugMenuOption> xenotypeLister = [];
                    foreach (XenotypeDef allDef in DefDatabase<XenotypeDef>.AllDefs)
                    {
                        XenotypeDef xenotype = allDef;
                        xenotypeLister.Add(new DebugMenuOption(xenotype.LabelCap, DebugMenuOptionMode.Action, delegate
                        {
                            pawn.genes.SetXenotype(xenotype);
                            pawn.TrySwapToXenotypeThingDef();
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(xenotypeLister));
                }),

                new FloatMenuOption("Spawn Xenogerm", delegate
                {
                    Discombobulator.CreateXenogerm(pawn, archite:true, type:"allAndInactive");
                }),

                new FloatMenuOption("Reapply Genes", delegate
                {
                    //List<DebugMenuOption> xenotypeLister = [];
                    foreach (XenotypeDef allDef in DefDatabase<XenotypeDef>.AllDefs)
                    {
                        var endoGeneDefs = pawn.genes.Endogenes.Select(g => g.def).ToList();
                        var xenoGeneDefs = pawn.genes.Xenogenes.Select(g => g.def).ToList();
                        GeneHelpers.RemoveAllGenesSlow(pawn);
                        foreach (var geneDef in endoGeneDefs)
                        {
                            pawn.genes.AddGene(geneDef, false);
                        }
                        foreach (var geneDef in xenoGeneDefs)
                        {
                            pawn.genes.AddGene(geneDef, true);
                        }

                    }
                    //Find.WindowStack.Add(new Dialog_DebugOptionListLister(xenotypeLister));
                }),

                new FloatMenuOption("Remove overriden genes", delegate
                {
                    var inactiveGenes = GeneHelpers.GetAllInactiveGenes(pawn);
                    foreach (var gene in inactiveGenes)
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                }),

                new FloatMenuOption("Remove all Endogenes", delegate
                {
                    var endoGenes = pawn.genes.Endogenes.Select(g => g).ToList();
                    foreach (var geneDef in endoGenes)
                    {
                        pawn.genes.RemoveGene(geneDef);
                    }
                }),

                new FloatMenuOption("Remove all Xenogenes", delegate
                {
                    var xenoGenes = pawn.genes.Xenogenes.Select(g => g).ToList();
                    foreach (var geneDef in xenoGenes)
                    {
                        pawn.genes.RemoveGene(geneDef);
                    }
                }),

                new FloatMenuOption("Discombobulate", delegate
                {
                    Discombobulator.Discombobulate(pawn, addComa:false);
                }),

                new FloatMenuOption("Set to random xenotype", delegate
                {
                    GeneHelpers.RemoveAllGenesSlow_ExceptColor(pawn);
                    pawn.genes.SetXenotype(DefDatabase<XenotypeDef>.AllDefs.RandomElement());
                    pawn.TrySwapToXenotypeThingDef();
                }),




            ];
            Find.WindowStack.Add(new FloatMenu(list));
        }
    }

}
