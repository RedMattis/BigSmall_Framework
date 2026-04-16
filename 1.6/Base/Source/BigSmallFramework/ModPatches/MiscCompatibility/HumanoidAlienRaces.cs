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

        public static HARThingDefWrapper TryGetHarWrapper(ThingDef thingDef)
        {
            if (HARActive && harThings.TryGetValue(thingDef, out var harWrap) && harWrap is not null)
            {
                return harWrap;
            }
            return null;
        }

        public static List<BodyTypeDef> TryGetHarBodiesForThingdef(ThingDef thingDef)
        {
            if (HARActive && harThings.TryGetValue(thingDef, out var harWrap) && harWrap.HasBodyDefs)
            {
                return harWrap.bodyDefs;
            }
            return null;
        }

        public static bool IsHarRaceWithExtendedBodyGraphics(ThingDef thingDef)
        {
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
            bodyDefs = GetBodyTypes(harThingDef);
            hasExtendedBodyGraphics = HasCustomBody(harThingDef);
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
                    return null;
                }

                var alienPartGeneratorInstance = alienPartGeneratorField.GetValue(generalSettingsInstance);
                if (alienPartGeneratorInstance == null)
                {
                    return null;
                }

                var bodyTypesField = alienPartGeneratorInstance.GetType().GetField("bodyTypes", BindingFlags.Public | BindingFlags.Instance);
                if (bodyTypesField == null)
                {
                    return null;
                }
                var result = bodyTypesField.GetValue(alienPartGeneratorInstance) as List<BodyTypeDef>;

                return result.NullOrEmpty() ? null : result;
            }
            catch (Exception e)
            {
                Log.Error($"Exception occurred while retrieving bodyTypes:\n{e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        private bool HasCustomBody(ThingDef harThingDef)
        {
            try
            {
                // Navigate to alienRace
                var alienRaceField = harThingDef.GetType().GetField("alienRace", BindingFlags.Public | BindingFlags.Instance);
                if (alienRaceField == null)
                {
                    return false;
                }

                var alienRaceInstance = alienRaceField.GetValue(harThingDef);
                if (alienRaceInstance == null)
                {
                    return false;
                }
                if (HasExtendedBodyGraphicss(alienRaceInstance))
                    return true;

                if (HasBodyAddons(alienRaceInstance))
                    return true;
                
            }
            catch (Exception e)
            {
                Log.Error($"Exception occurred while checking for HasExtendedBodyGraphics:\n{e.Message}\n{e.StackTrace}");
                return true;
            }
            return false;
        }

        public bool HasBodyAddons(object alienRaceInstance)
        {
            var generalSettingsField = alienRaceInstance.GetType().GetField("generalSettings", BindingFlags.Public | BindingFlags.Instance);
            if (generalSettingsField == null)
            {
                return false;
            }

            var generalSettingsInstance = generalSettingsField.GetValue(alienRaceInstance);
            if (generalSettingsInstance == null)
            {
                return false;
            }

            var alienPartGeneratorField = generalSettingsInstance.GetType().GetField("alienPartGenerator", BindingFlags.Public | BindingFlags.Instance);
            if (alienPartGeneratorField == null)
            {
                return false;
            }

            var alienPartGeneratorInstance = alienPartGeneratorField.GetValue(generalSettingsInstance);
            if (alienPartGeneratorInstance == null)
            {
                return false;
            }

            var bodyAddonsField = alienPartGeneratorInstance.GetType().GetField("bodyAddons", BindingFlags.Public | BindingFlags.Instance);
            if (bodyAddonsField == null)
            {
                return false;
            }
            var bodyAddonsInstance = bodyAddonsField.GetValue(alienPartGeneratorInstance);
            if (bodyAddonsInstance is IList bodyAddonList)
            {
                if (bodyAddonList.Count !=0)
                    return true;
            }

            return false;
        }

        public bool HasExtendedBodyGraphicss(object alienRaceInstance)
        {
            var graphicPaths = alienRaceInstance.GetType().GetField("graphicPaths", BindingFlags.Public | BindingFlags.Instance);
            if (graphicPaths == null)
            {
                return false;
            }

            var graphicPathsInstance = graphicPaths.GetValue(alienRaceInstance);
            if (graphicPathsInstance == null)
            {
                return false;
            }

            var bodyField = graphicPathsInstance.GetType().GetField("body", BindingFlags.Public | BindingFlags.Instance);
            if (bodyField == null)
            {
                return false;
            }

            var bodyInstance = bodyField.GetValue(graphicPathsInstance);
            if (bodyInstance == null)
            {
                return false;
            }

            var extendedGraphicsField = bodyInstance.GetType().GetField("extendedGraphics", BindingFlags.Public | BindingFlags.Instance);
            if (extendedGraphicsField == null)
            {
                return false;
            }
            var extendedGraphicsInstance = extendedGraphicsField.GetValue(bodyInstance);
            var extendedGraphicsList = extendedGraphicsInstance as IList;
            foreach (var entry in extendedGraphicsList)
            {
                var conditionsField = entry.GetType().GetField("conditions");
                var conditionInstance = conditionsField.GetValue(entry);
                if (conditionInstance is IList conditionInstList)
                {
                    foreach (var condition in conditionInstList)
                    {
                        if (condition?.GetType()?.Name?.ToLower().Contains("conditionbodytype") == true)
                            return true;

                        if (condition?.GetType()?.Name?.ToLower().Contains("conditionbodypart") == true)
                            return true;


                    }
                }
            }
            return false;
        }

    }
}
