using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace BigAndSmall.Debugging
{

    public static class BigAndSmallDebugActions
    {
        public const int lblPad = -0;

        [DebugAction("Big & Small", "Transform To Race...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> TransformToRace()
        {
            List<DebugActionNode> list = [];
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
            {
                list.Add(new DebugActionNode($"{def.defName,lblPad}\t ({def.LabelCap})", DebugActionType.ToolMap, delegate
                {
                    foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
                    {
                        RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 100, force: false);
                    }
                }));
            }
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
            {
                list.Add(new DebugActionNode($"[FORCE]: {def.defName,lblPad}\t ({def.LabelCap})", DebugActionType.ToolMap, delegate
                {
                    foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
                    {
                        RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 999, force: true, permitFusion: false);
                    }
                }));
            }
            return list;
        }

        [DebugAction("Big & Small - Spawn", "Colonist of Xenotype", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> SpawnColonistOfXeno()
        {
            List<DebugActionNode> list = [];
            foreach (XenotypeDef xeno in from x in DefDatabase<XenotypeDef>.AllDefs
                                         select x into kd
                                         orderby kd.defName
                                         select kd)
            {
                PawnKindDef localKindDef = PawnKindDefOf.Colonist;
                list.Add(new DebugActionNode($"{xeno.defName,lblPad}\t ({xeno.LabelCap})", DebugActionType.ToolMap)
                {
                    category = DebugToolsSpawning.GetCategoryForPawnKind(localKindDef),
                    action = delegate
                    {
                        Faction faction = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionDef);
                        Pawn pawn = PawnGenerator.GeneratePawn(localKindDef, faction);
                        GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
                        DebugToolsSpawning.PostPawnSpawn(pawn);
                        DebugUIPatches.SetXenotypeAndRace(pawn, xeno);
                        if (BS.Settings.recruitDevSpawned)
                        {
                            pawn.SetFaction(Faction.OfPlayer);
                        }
                    }
                });
            }
            return list;
        }

        [DebugAction("Big & Small - Spawn", "Villager of Xenotype", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> SpawnVillagerOfXeno()
        {
            List<DebugActionNode> list = [];
            foreach (XenotypeDef xeno in from x in DefDatabase<XenotypeDef>.AllDefs
                                         select x into kd
                                         orderby kd.defName
                                         select kd)
            {
                PawnKindDef localKindDef = PawnKindDefOf.Villager;
                list.Add(new DebugActionNode($"{xeno.defName,lblPad}\t ({xeno.LabelCap})", DebugActionType.ToolMap)
                {
                    category = DebugToolsSpawning.GetCategoryForPawnKind(localKindDef),
                    action = delegate
                    {
                        Faction faction = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionDef);
                        Pawn pawn = PawnGenerator.GeneratePawn(localKindDef, faction);
                        GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
                        DebugToolsSpawning.PostPawnSpawn(pawn);
                        DebugUIPatches.SetXenotypeAndRace(pawn, xeno);
                        if (BS.Settings.recruitDevSpawned)
                        {
                            pawn.SetFaction(Faction.OfPlayer);
                        }
                    }
                });
            }
            return list;
        }
        
        [DebugAction("Big & Small - Spawn", "Random Human Pawnkind", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnRandomHumanKind()
        {
            var randomKinds = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race?.race.Humanlike == true && x.race?.IsHumanlikeAnimal() == false);
            if (randomKinds.Any())
            {
                var kind = randomKinds.RandomElement();
                Pawn pawn = PawnGenerator.GeneratePawn(kind, null);
                GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
            }
        }

        [DebugAction("Big & Small - Spawn", "Random Animal Pawnkind", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnRandomAnimal()
        {
            var randomKinds = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race?.race.Animal == true);
            if (randomKinds.Any())
            {
                var kind = randomKinds.RandomElement();
                Pawn pawn = PawnGenerator.GeneratePawn(kind, null);
                GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
            }
        }

        [DebugAction("Big & Small - Spawn", "Random Sapient Animal", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnRandomSapientAnimal()
        {
            var randomKinds = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race?.race.Animal == true);
            if (randomKinds.Any())
            {
                var kind = randomKinds.RandomElement();
                Pawn pawn = PawnGenerator.GeneratePawn(kind, null);
                var sapientPawn = RaceMorpher.SwapAnimalToSapientVersion(pawn);
                GenSpawn.Spawn(sapientPawn, UI.MouseCell(), Find.CurrentMap);
            }
        }

        [DebugAction("Big & Small - Spawn", "Random Mechanoid Pawnkind", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnRandomMechanoid()
        {
            var randomKinds = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race?.race.IsMechanoid == true);
            if (randomKinds.Any())
            {
                var kind = randomKinds.RandomElement();
                Pawn pawn = PawnGenerator.GeneratePawn(kind, null);
                GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
            }
        }

        [DebugAction("Big & Small - Spawn", "Random Sapient Mechanoid", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnRandomSapientMechanoid()
        {
            var randomKinds = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race?.race.IsMechanoid == true);
            if (randomKinds.Any())
            {
                var kind = randomKinds.RandomElement();
                Pawn pawn = PawnGenerator.GeneratePawn(kind, null);
                var sapientPawn = RaceMorpher.SwapAnimalToSapientVersion(pawn);
                GenSpawn.Spawn(sapientPawn, UI.MouseCell(), Find.CurrentMap);
            }
        }

        [DebugAction("Big & Small - Spawn", "Random of Xenotype", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> SpawnRandomPawnOf()
        {
            List<DebugActionNode> list = [];
            foreach (XenotypeDef xeno in from x in DefDatabase<XenotypeDef>.AllDefs
                                         select x into kd
                                         orderby kd.defName
                                         select kd)
            {
                list.Add(new DebugActionNode($"{xeno.defName,lblPad}\t ({xeno.LabelCap})", DebugActionType.ToolMap)
                {
                    action = delegate
                    {
                        PawnKindDef localKindDef = DefDatabase<PawnKindDef>.AllDefs
                            .Where(x => x.race == ThingDefOf.Human && x.showInDebugSpawner && x.RaceProps?.Humanlike == true)
                            .RandomElementWithFallback(PawnKindDefOf.Colonist);
                        Faction faction = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionDef);
                        Pawn pawn = PawnGenerator.GeneratePawn(localKindDef, faction);
                        GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
                        DebugToolsSpawning.PostPawnSpawn(pawn);
                        DebugUIPatches.SetXenotypeAndRace(pawn, xeno);
                        if (BS.Settings.recruitDevSpawned)
                        {
                            pawn.SetFaction(Faction.OfPlayer);
                        }
                    }
                });
            }
            return list;
        }

        [DebugAction("Big & Small - Spawn", "Colonist of Race", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> SpawnColonistOfRace()
        {
            List<DebugActionNode> list = [];
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
            {
                PawnKindDef localKindDef = PawnKindDefOf.Villager;
                list.Add(new DebugActionNode($"{def.defName,lblPad}\t ({def.LabelCap})", DebugActionType.ToolMap)
                {
                    category = DebugToolsSpawning.GetCategoryForPawnKind(localKindDef),
                    action = delegate
                    {
                        Faction faction = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionDef);
                        Pawn pawn = PawnGenerator.GeneratePawn(localKindDef, faction);
                        GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
                        DebugToolsSpawning.PostPawnSpawn(pawn);
                        RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 9999, force: false);
                        if (BS.Settings.recruitDevSpawned)
                        {
                            pawn.SetFaction(Faction.OfPlayer);
                        }
                    }
                });
            }
            return list;
        }

        [DebugAction("Big & Small", "Force-refresh cache on pawn", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ForceRefreshCacheOnPawn()
        {
            foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            {
                HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
                Log.Message($"Forced refresh on {pawn.LabelCap}");
            }
        }

        [DebugAction("Big & Small", "Make animal Sapient", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void MakeSapientVersionOf()
        {
            foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            {
                RaceMorpher.SwapAnimalToSapientVersion(pawn);
            }
        }

        [DebugAction("Big & Small", "Clear Cache for pawn then refresh", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ClearCacheForPawn()
        {
            foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            {
                BigAndSmallCache.ScribedCache.RemoveWhere(x => x.pawn == pawn);
                BigAndSmallCache.refreshQueue.Clear();
                BigAndSmallCache.queuedJobs.Clear();
                BigAndSmallCache.schedulePostUpdate.Clear();
                BigAndSmallCache.scheduleFullUpdate.Clear();
                HumanoidPawnScaler.Cache.TryRemove(pawn, out _);
                HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
                //HumanoidPawnScaler.permitThreadedCaches = false;
            }
        }

        [DebugAction("Big & Small", "Clear Junk", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ClearJunk()
        {
            DebugToolsGeneral.GenericRectTool("Clear Junk", delegate (CellRect rect)
            {
                ClearAreaOfJunk(rect, Find.CurrentMap);
            });
        }

        [DebugAction("Big & Small", "Edit Graphics", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugEditCustomizableGraphic()
        {
            foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            {
                var window = new EditPawnWindow(pawn);
                Find.WindowStack.Add(window);
                return;
            }
        }


        [DebugAction("Big & Small", "Misc...",actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> MiscDebug()
        {
            List<DebugActionNode> list = [];

            list.Add(new DebugActionNode("Set Graphics Dirty", DebugActionType.ToolMap, delegate
            {
                foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
            }));

            list.Add(new DebugActionNode("Set Age to 0", DebugActionType.ToolMap, delegate
            {
                foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
                {
                    pawn.ageTracker.AgeBiologicalTicks = 0;
                }
            }));

            list.Add(new DebugActionNode("Remove Custom Graphics", DebugActionType.ToolMap, delegate
            {
                RemoveCustomGraphics();
            }));
            list.Add(new DebugActionNode("Set Custom Color A", DebugActionType.ToolMap, delegate
            {
                SetCustomColor(0);
            }));
            list.Add(new DebugActionNode("Set Custom Color B", DebugActionType.ToolMap, delegate
            {
                SetCustomColor(1);
            }));
            list.Add(new DebugActionNode("Set Custom Color C", DebugActionType.ToolMap, delegate
            {
                SetCustomColor(2);
            }));

            list.Add(new DebugActionNode("Randomise Faction", DebugActionType.ToolMap, delegate
            {
                DebugToolsGeneral.GenericRectTool("Randomize Faction", delegate (CellRect rect)
                {
                    foreach (IntVec3 item2 in rect)
                    {
                        foreach (Pawn item3 in item2.GetThingList(Find.CurrentMap).OfType<Pawn>())
                        {
                            if (Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out Faction nFaction, false))
                                item3.SetFaction(nFaction);
                        }
                    }
                });
            }));

            //list.Add(new DebugActionNode("Test Diet", DebugActionType.ToolMap, delegate
            //{
            //    foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            //    {
            //        PawnDiet.DebugTestAllowanceOnPawn(pawn);
            //    }
            //}));

            list.Add(new DebugActionNode("Test Apparel Restrictions", DebugActionType.ToolMap, delegate
            {
                foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
                {
                    ApparelRestrictions.DebugTestAllWearable(pawn);
                }
            }));

            //list.Add(new DebugActionNode("Clear Junk", DebugActionType.Action, delegate
            //{
            //    DebugToolsGeneral.GenericRectTool("Clear Junk", delegate (CellRect rect)
            //    {
            //        ClearAreaOfJunk(rect, Find.CurrentMap);
            //    });
            //}));

            return list;
        }

        public static void RemoveCustomGraphics()
        {
            IntVec3 cell = UI.MouseCell();
            foreach (Thing item in Find.CurrentMap.thingGrid.ThingsAt(cell).ToList())
            {
                if (item is Pawn pawn && pawn.apparel != null)
                {
                    foreach (Apparel appItem in pawn.apparel.WornApparel)
                    {
                        CustomizableGraphic.Replace(appItem, null);
                    }
                }
                else
                {
                    CustomizableGraphic.Replace(item, null);
                }
            }
            foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(cell).OfType<Pawn>())
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
        public static void SetCustomColor(int slot)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            IntVec3 cell = UI.MouseCell();
            list.Add(new FloatMenuOption("Random", delegate
            {
                SetColor_All(GenColor.RandomColorOpaque());
            }));
            foreach (Ideo i in Find.IdeoManager.IdeosListForReading)
            {
                if (!i.classicMode && i.Icon != BaseContent.BadTex)
                {
                    list.Add(new FloatMenuOption(i.name, delegate
                    {
                        SetColor_All(i.Color);
                    }, i.Icon, i.Color));
                }
            }
            foreach (ColorDef c in DefDatabase<ColorDef>.AllDefs)
            {
                list.Add(new FloatMenuOption(c.defName, delegate
                {
                    SetColor_All(c.color);
                }, BaseContent.WhiteTex, c.color));
            }
            foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(cell).OfType<Pawn>())
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
            Find.WindowStack.Add(new FloatMenu(list));
            void SetColor_All(Color color)
            {
                List<Thing> thingsToSet = [];
                foreach (Thing item in Find.CurrentMap.thingGrid.ThingsAt(cell))
                {
                    if (item is Pawn pawn && pawn.apparel != null)
                    {
                        foreach (Apparel appItem in pawn.apparel.WornApparel)
                        {
                            thingsToSet.Add(appItem);
                        }
                    }
                    else
                    {
                        thingsToSet.Add(item);
                    }
                }
                foreach (var thing in thingsToSet)
                {
                    var graphic = CustomizableGraphic.Get(thing, createIfMissing: true);
                    if (slot == 0) graphic.colorA = color;
                    else if (slot == 1) graphic.colorB = color;
                    else if (slot == 2) graphic.colorC = color;
                    Log.Message($"Debug-Set {thing.LabelCap}'s color slot {slot} to {color}.\nResult: {thing} - {graphic}");
                }
                foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(cell).OfType<Pawn>())
                {
                    pawn.Drawer.renderer.renderTree.SetDirty();
                }
            }
        }

        public static void ClearAreaOfJunk(CellRect r, Map map)
        {
            r.ClipInsideMap(map);
            foreach (IntVec3 item in r)
            {
                map.roofGrid.SetRoof(item, null);
            }
            foreach (IntVec3 item2 in r)
            {
                foreach (Thing item3 in item2.GetThingList(map).ToList())
                {
                    // Remove if haulable or filth
                    if (item3.def.destroyable && (item3.def.EverHaulable) || item3.def.filth != null)
                    {
                        item3.Destroy();
                    }
                }
            }
        }
    }

    

}
