using RimWorld;
using System;
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
        public static bool HARActive => harActive ??= ModLister.GetActiveModWithIdentifier("erdelf.HumanoidAlienRaces") != null;

        /// <summary>
        /// HAR Races use a subclass of ThingDef which we don't have direct access to, so we need a wrapper class built from reflection.
        /// </summary>
        public static Dictionary<ThingDef, HARThingDefWrapper> harThings = [];

        public static void SetupHARThingsIfHARIsActive()
        {
            if (!HARActive)
            {
                return;
            }
            // Each which has a class name of "AlienRace.ThingDef_AlienRace"
            var allAlienHumanThings = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.GetType().Name == "ThingDef_AlienRace").ToList();
            Log.Message($"[Big and Small]: Found {allAlienHumanThings.Count} AlienRace.ThingDef_AlienRace ThingDefs." +
                $"\nThese are either from HAR races or B&S races automatically converted for compatibility.");
            foreach (var thing in allAlienHumanThings)
            {
                harThings[thing] = new HARThingDefWrapper(thing);
            }
        }

        public static List<BodyTypeDef> TryGetHarBodiesForThingdef(ThingDef thingDef)
        {
            if (HARActive && harThings.TryGetValue(thingDef, out var harWrap) && harWrap.HasBodyDefs)
            {
                return harWrap.bodyDefs;
            }
            return null;
        }
    }

    public class HARThingDefWrapper
    {
        public ThingDef HARThingDef;

        public List<BodyTypeDef> bodyDefs = null;

        public bool HasBodyDefs => bodyDefs != null && bodyDefs.Count > 0;
        public HARThingDefWrapper(ThingDef harThingDef)
        {
            HARThingDef = harThingDef;
            bodyDefs = GetBodyTypes(harThingDef);
        }

        private List<BodyTypeDef> GetBodyTypes(ThingDef harThingDef)
        {
            try
            {
                // Navigate to alienRace
                var alienRaceField = harThingDef.GetType().GetField("alienRace", BindingFlags.Public | BindingFlags.Instance);
                if (alienRaceField == null)
                {
                    return null;
                }

                var alienRaceInstance = alienRaceField.GetValue(harThingDef);
                if (alienRaceInstance == null)
                {
                    return null;
                }

                // Navigate to generalSettings
                var generalSettingsField = alienRaceInstance.GetType().GetField("generalSettings", BindingFlags.Public | BindingFlags.Instance);
                if (generalSettingsField == null)
                {
                    return null;
                }

                var generalSettingsInstance = generalSettingsField.GetValue(alienRaceInstance);
                if (generalSettingsInstance == null)
                {
                    return null;
                }

                var alienPartGeneratorField = generalSettingsInstance.GetType().GetField("alienPartGenerator", BindingFlags.Public | BindingFlags.Instance);
                if (alienPartGeneratorField == null)
                {
                    // alienPartGenerator field is missing; assume default behavior
                    return null;
                }

                var alienPartGeneratorInstance = alienPartGeneratorField.GetValue(generalSettingsInstance);
                if (alienPartGeneratorInstance == null)
                {
                    return null;
                }

                // Navigate to bodyTypes
                var bodyTypesField = alienPartGeneratorInstance.GetType().GetField("bodyTypes", BindingFlags.Public | BindingFlags.Instance);
                if (bodyTypesField == null)
                {
                    return null;
                }
                var result = bodyTypesField.GetValue(alienPartGeneratorInstance) as List<BodyTypeDef>;

                return result.NullOrEmpty() ? null : result;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception occurred while retrieving bodyTypes: {ex.Message}");
                return null;
            }
        }

    }
}
