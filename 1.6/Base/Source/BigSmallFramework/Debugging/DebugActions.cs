using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
