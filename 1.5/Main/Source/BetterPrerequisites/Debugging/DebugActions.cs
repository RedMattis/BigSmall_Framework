using LudeonTK;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall.Debugging
{

    public static class BigAndSmallDebugActions
    {
        [DebugAction("Big & Small", "Transform To Race...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> TransformToRace()
        {
            List<DebugActionNode> list = [];
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
            {
                list.Add(new DebugActionNode(def.defName, DebugActionType.ToolMap, delegate
                {
                    foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
                    {
                        RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 100, force: false);
                    }
                }));
            }
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x?.race?.intelligence == Intelligence.Humanlike && !x.IsCorpse))
            {
                list.Add(new DebugActionNode(def.defName + " (force)", DebugActionType.ToolMap, delegate
                {
                    foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
                    {
                        RaceMorpher.SwapThingDef(pawn, def, true, targetPriority: 999, force: true, permitFusion: false);
                    }
                }));
            }
            return list;
        }

        [DebugAction("Big & Small", "Force-refresh cache on pawn", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ForceRefreshCacheOnPawn()
        {
            foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            {
                HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
            }
        }

        [DebugAction("Big & Small", "Clear Cache for pawn", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ClearCacheForPawn()
        {
            foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            {
                BigAndSmallCache.scribedCache.RemoveWhere(x => x.pawn == pawn);
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

            //list.Add(new DebugActionNode("Force-refresh cache on pawn", DebugActionType.ToolMap, delegate
            //{
            //    foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            //    {
            //        HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
            //    }
            //}));

            //list.Add(new DebugActionNode("Clear Cache for pawn", DebugActionType.ToolMap, delegate
            //{
            //    foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
            //    {
            //        BigAndSmallCache.scribedCache.RemoveWhere(x => x.pawn == pawn);
            //        BigAndSmallCache.refreshQueue.Clear();
            //        BigAndSmallCache.queuedJobs.Clear();
            //        BigAndSmallCache.schedulePostUpdate.Clear();
            //        BigAndSmallCache.scheduleFullUpdate.Clear();
            //        HumanoidPawnScaler.Cache.TryRemove(pawn, out _);
            //        HumanoidPawnScaler.GetCache(pawn, forceRefresh: true);
            //        HumanoidPawnScaler.canUseThreadStaticDict = false;
            //    }
            //}));

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
