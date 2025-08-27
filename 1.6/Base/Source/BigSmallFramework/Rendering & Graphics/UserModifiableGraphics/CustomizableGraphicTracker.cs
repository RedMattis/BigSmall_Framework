using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class CustomizableGraphicTracker : GameComponent
    {
        public static CustomizableGraphicTracker GInstance;
        public Dictionary<string, CustomizableGraphic> thingGraphics = [];

        public CustomizableGraphicTracker(Game game)
        {
            GInstance = this;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                CleanupDestroyedItems();
            }
            Scribe_Collections.Look(ref thingGraphics, "thingCustomGraphics", LookMode.Value, LookMode.Deep);
            thingGraphics ??= [];
        }

        private void CleanupDestroyedItems()
        {
            var toKeep = new List<string>();
            HashSet<Thing> allLiveThingsEverywhere = GetAllThingsEverywhere();
            foreach (var kvp in thingGraphics)
            {
                foreach (var thing in allLiveThingsEverywhere)
                {
                    if (thing.ThingID == kvp.Key)
                    {
                        toKeep.Add(kvp.Key);
                        break;
                    }
                }
            }
            thingGraphics = thingGraphics.Where(kvp => toKeep.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static HashSet<Thing> GetAllThingsEverywhere()
        {
            var allThingsInWorld = Current.Game.Maps.SelectMany(x => x.listerThings.AllThings).ToList();
            var allPawnsInWorld = Current.Game.Maps.SelectMany(x => x.mapPawns.AllPawns).ToList();
            var allPawnAndThings = allThingsInWorld.Concat(allPawnsInWorld).ToList();
            HashSet<Thing> allLiveThingsEverywhere = [];
            foreach (var thing in allPawnAndThings.Where(x => !x.Destroyed))
            {
                allLiveThingsEverywhere.Add(thing);
                if (thing is IThingHolder holder)
                {
                    if (holder.GetDirectlyHeldThings() != null)
                    {
                        foreach (var subThing in holder.GetDirectlyHeldThings().Where(x => !x.Destroyed))
                        {
                            allLiveThingsEverywhere.Add(subThing);
                        }
                    }
                }
                if (thing is Pawn pawn)
                {
                    if (pawn.apparel != null)
                    {
                        foreach (var apparel in pawn.apparel.WornApparel.Where(x => !x.Destroyed))
                        {
                            allLiveThingsEverywhere.Add(apparel);
                        }
                    }
                    if (pawn.equipment != null)
                    {
                        foreach (var eq in pawn.equipment.AllEquipmentListForReading.Where(x => !x.Destroyed))
                        {
                            allLiveThingsEverywhere.Add(eq);
                        }
                    }
                }
            }

            return allLiveThingsEverywhere;
        }
    }
}
