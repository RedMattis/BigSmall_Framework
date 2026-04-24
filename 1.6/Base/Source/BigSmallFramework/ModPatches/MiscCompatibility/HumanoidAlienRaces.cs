using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class HARCompat
    {
        private static bool? harActive = null;
        private static bool harSetupCompleted = false;
        public static bool HARActive => harActive ??= 
            (ModLister.GetActiveModWithIdentifier("erdelf.HumanoidAlienRaces") != null) ||
            (ModLister.GetActiveModWithIdentifier("erdelf.HumanoidAlienRaces.dev") != null);

        /// <summary>
        /// HAR Races use a subclass of ThingDef which we don't have direct access to, so we need a wrapper class built from reflection.
        /// </summary>
        public static Dictionary<ThingDef, HARThingDefWrapper> harThings = [];

        public static void TrySetupHARThingsIfHARIsActive()
        {
            if (!HARActive || harSetupCompleted)
            {
                return;
            }
            harSetupCompleted = true;
            // Each which has a class name of "AlienRace.ThingDef_AlienRace"
            var allAlienHumanThings = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.GetType().Name == "ThingDef_AlienRace").ToList();
            Log.Message($"[Big and Small]: Found {allAlienHumanThings.Count} AlienRace.ThingDef_AlienRace ThingDefs." +
                $"\nThese are either from HAR races or B&S races automatically converted for compatibility.");
            foreach (var thing in allAlienHumanThings)
            {
                harThings[thing] = new HARThingDefWrapper(thing);
            }
        }

        public static HARThingDefWrapper TryGetHarWrapper(ThingDef thingDef)
        {
            TrySetupHARThingsIfHARIsActive();
            if (HARActive && harThings.TryGetValue(thingDef, out var harWrap) && harWrap is not null)
            {
                return harWrap;
            }
            return null;
        }

        public static List<BodyTypeDef> TryGetHarBodiesForThingdef(ThingDef thingDef)
        {
            TrySetupHARThingsIfHARIsActive();
            if (HARActive && harThings.TryGetValue(thingDef, out var harWrap) && harWrap.HasBodyDefs)
            {
                return harWrap.bodyDefs;
            }
            return null;
        }

        public static bool IsHarRaceWithExtendedBodyGraphics(ThingDef thingDef)
        {
            TrySetupHARThingsIfHARIsActive();
            if (HARActive && harThings.TryGetValue(thingDef, out var harWrap) && harWrap.hasExtendedBodyGraphics)
            {
                return true;
            }
            return false;
        }
    }

    public class HARThingDefWrapper
    {
        public ThingDef HARThingDef;

        public List<BodyTypeDef> bodyDefs = null;
        public bool hasExtendedBodyGraphics = false;

        public bool HasBodyDefs => bodyDefs != null && bodyDefs.Count > 0;
        public HARThingDefWrapper(ThingDef harThingDef)
        {
            HARThingDef = harThingDef;
            bodyDefs = GetBodyTypes_V2(harThingDef);
            hasExtendedBodyGraphics = UsingCustomGraphics_V2(harThingDef);
        }

        private List<BodyTypeDef> GetBodyTypes_V2(ThingDef harThingDef) =>
            Traverse.Create(harThingDef)
                .Field("alienRace")
                .Field("compatibility")
                .Property("AvailableBodyTypes")
                .GetValue<List<BodyTypeDef>>();


        private bool UsingCustomGraphics_V2(ThingDef harThingDef) =>
            Traverse.Create(harThingDef)
                .Field("alienRace")
                .Field("compatibility")
                .Property("UsingCustomGraphics")
                .GetValue<bool>();
    }
}
