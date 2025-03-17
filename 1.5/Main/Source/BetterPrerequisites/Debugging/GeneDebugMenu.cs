using BetterPrerequisites;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
using static BigAndSmall.Debugging.BigAndSmallDebugActions;

namespace BigAndSmall.Debugging
{
    [HarmonyPatch]
    public static class DebugUIPatches
    {
        [HarmonyPatch(typeof(GeneUIUtility), "DoDebugButton")]
        [HarmonyPostfix]
        public static void DoDebugButton_Postfix(ref Rect buttonRect, Thing target, GeneSet genesOverride)
        {
            DoGeneDebugButton(ref buttonRect, target);
        }
        public static void DoGeneDebugButton(ref Rect buttonRect, Thing target, string title = "Big & Small")
        {
            if (target is not Pawn)
            {
                return;
            }
            Pawn pawn = target as Pawn;
            float widthOfPrevious = buttonRect.size.x;
            buttonRect = new Rect(buttonRect.x - widthOfPrevious - 10, buttonRect.y, buttonRect.width, buttonRect.height);
            if (!Widgets.ButtonText(buttonRect, title))
            {
                return;
            }

            List<FloatMenuOption> list =
            [
                new FloatMenuOption("Set to Race...", delegate
                {
                    List<DebugMenuOption> list = [];
                    foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
                    {
                        list.Add(new DebugMenuOption($"{def.defName,lblPad}\t ({def.LabelCap})", DebugMenuOptionMode.Action, delegate
                        {
                            RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 999, force: true, permitFusion: false);
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
                }),
            ];
            if (ModsConfig.BiotechActive)
            {
                List<FloatMenuOption> biotechList =
                [
                    new FloatMenuOption("Apply/Append RaceDef", delegate
                    {
                        List<DebugMenuOption> list = [];

                        foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
                        {
                            list.Add(new DebugMenuOption($"{def.defName,lblPad}\t ({def.LabelCap})", DebugMenuOptionMode.Action, delegate
                            {
                                RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 100, force: false);
                            }));
                        }
                        foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
                        {
                            list.Add(new DebugMenuOption($"{def.defName,lblPad}\t ({def.LabelCap})" + " (force)", DebugMenuOptionMode.Action, delegate
                            {
                                RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 999, force: true, permitFusion:false);
                            }));
                        }
                        Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
                    }),
                    new FloatMenuOption("Set exact xenotype + race", delegate
                    {
                        List<DebugMenuOption> xenotypeLister = [];
                        foreach (XenotypeDef allDef in DefDatabase<XenotypeDef>.AllDefs)
                        {
                            XenotypeDef xenotype = allDef;
                            xenotypeLister.Add(new DebugMenuOption($"{xenotype.defName,lblPad}\t ({xenotype.LabelCap})", DebugMenuOptionMode.Action, delegate
                            {
                                SetXenotypeAndRace(pawn, xenotype);
                            }));
                        }
                        Find.WindowStack.Add(new Dialog_DebugOptionListLister(xenotypeLister));
                    }),
                    new FloatMenuOption("Apply xenotype", delegate
                    {
                        List<DebugMenuOption> xenotypeLister = [];
                        foreach (XenotypeDef allDef in DefDatabase<XenotypeDef>.AllDefs)
                        {
                            XenotypeDef xenotype = allDef;
                            xenotypeLister.Add(new DebugMenuOption($"{xenotype.defName,lblPad}\t ({xenotype.LabelCap})", DebugMenuOptionMode.Action, delegate
                            {
                                pawn.genes.SetXenotype(xenotype);
                                pawn.TrySwapToXenotypeThingDef();
                            }));
                        }
                        Find.WindowStack.Add(new Dialog_DebugOptionListLister(xenotypeLister));
                    }),

                    new FloatMenuOption("Spawn Xenogerm", delegate
                    {
                        CompTargetEffect_CreateXenogerm.CreateXenogerm(pawn, archite:true, endoGenes:true, xenoGenes:true, inactive:true);
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
                        GeneHelpers.RemoveAllGenesSlow(pawn);
                        pawn.genes.SetXenotype(DefDatabase<XenotypeDef>.AllDefs.RandomElement());
                        pawn.TrySwapToXenotypeThingDef();
                    }),

                    new FloatMenuOption("Set to Baseline Human [Force]", delegate
                    {
                        GeneHelpers.RemoveAllGenesSlow_ExceptColor(pawn);
                        RaceMorpher.SwapThingDef(pawn, ThingDefOf.Human, true, targetPriority: 999, force: true, permitFusion: false);
                    }),
                ];
                list.AddRange(biotechList);
            }
            else
            {
                // Add options here which don't require Biotech, if any.
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public static void SetXenotypeAndRace(Pawn pawn, XenotypeDef xenotype)
        {
            GeneHelpers.RemoveAllGenesSlow(pawn);
            pawn.genes.SetXenotype(xenotype);
            pawn.TrySwapToXenotypeThingDef();
        }
    }

}
