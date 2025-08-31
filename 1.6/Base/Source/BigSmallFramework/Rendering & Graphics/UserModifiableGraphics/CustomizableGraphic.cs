using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static BigAndSmall.CustomizableGraphicTracker;

namespace BigAndSmall
{
    public class CustomizableGraphic : IExposable
    {
        public class SubItemGraphic : IExposable
        {
            public FlagString flagString;
            public Color? colorA;
            public Color? colorB;
            public Color? colorC;

            public override string ToString() => $"[{nameof(SubItemGraphic)}] - Def: {flagString}, ColorA: {colorA}, ColorB: {colorB}, ColorC: {colorC}";
            public void ExposeData()
            {
                Scribe_Deep.Look(ref flagString, "flagString");
                Scribe_Values.Look(ref colorA, "colorA");
                Scribe_Values.Look(ref colorB, "colorB");
                Scribe_Values.Look(ref colorC, "colorC");
            }
        }

        public Color? colorA;
        public Color? colorB;
        public Color? colorC;

        public List<SubItemGraphic> flagItems = [];

        // Not yet implemented.
        //public string texturePath = null;
        //public string maskPath = null;

        // Used only by pawnkinds.
        public ColorGenerator colorAGenerator = null;  // Not yet implemented.
        public ColorGenerator colorBGenerator = null;
        public ColorGenerator colorCGenerator = null;

        public static void Replace(Thing t, CustomizableGraphic graphic)
        {
            if (t == null) { return; }
            if (graphic == null)
            {
                GInstance.thingGraphics.Remove(t.ThingID);
            }
            else
            {
                GInstance.thingGraphics[t.ThingID] = graphic;
            }
        }

        public static CustomizableGraphic Get(Thing t, bool createIfMissing = false)
        {
            if (t == null)
            {
                Log.WarningOnce("Tried to get graphic for null thing.", 893245);
                return null;
            }
            if (GInstance.thingGraphics.TryGetValue(t.ThingID, out var graphic))
            {
                return graphic;
            }
            else if (createIfMissing)
            {
                graphic = new CustomizableGraphic();
                GInstance.thingGraphics[t.ThingID] = graphic;
                return graphic;
            }
            return null;
        }

        public static SubItemGraphic GetFlagGraphic(Thing t, FlagString fStr, bool createIfMissing = false)
        {
            var graphic = Get(t, createIfMissing);
            if (graphic == null) { return null; }
            var subItem = graphic.flagItems?.FirstOrDefault(x => x.flagString == fStr);
            if (subItem == null && createIfMissing)
            {
                graphic.flagItems ??= [];
                subItem = new SubItemGraphic() { flagString = fStr };
                graphic.flagItems.Add(subItem);
            }
            return subItem;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref colorA, "colorA");
            Scribe_Values.Look(ref colorB, "colorB");
            Scribe_Values.Look(ref colorC, "colorC");
            Scribe_Collections.Look(ref flagItems, "tagItems", LookMode.Deep);
        }

        public override string ToString()
        {
            return $"[{nameof(CustomizableGraphic)}] - ColorA: {colorA}, ColorB: {colorB}, ColorC: {colorC}";
        }
    }

    public static class CustomizableGraphicExtensions
    {
        public static Color SetCustomColorA(this Thing t, Color color) =>
            (CustomizableGraphic.Get(t, true).colorA = color).Value;

        public static Color SetCustomColorB(this Thing t, Color color) =>
            (CustomizableGraphic.Get(t, true).colorB = color).Value;

        public static Color SetCustomColorC(this Thing t, Color color) =>
            (CustomizableGraphic.Get(t, true).colorC = color).Value;

        public static Color? GetCustomColorA(this Thing t) => CustomizableGraphic.Get(t)?.colorA;
        public static Color? GetCustomColorB(this Thing t) => CustomizableGraphic.Get(t)?.colorB;
        public static Color? GetCustomColorC(this Thing t) => CustomizableGraphic.Get(t)?.colorC;

        public static Color? GetFlagColor(this Thing t, FlagString fString, int colorIndex)
        {
            if (fString == null || t == null) return null;
            var tagItem = CustomizableGraphic.GetFlagGraphic(t, fString);
            if (tagItem == null) { return null; }
            return colorIndex switch
            {
                0 => tagItem.colorA,
                1 => tagItem.colorB,
                2 => tagItem.colorC,
                _ => throw new IndexOutOfRangeException($"requested color index {colorIndex}. Max index is 2."),
            };
        }

        public static Color SetFlagColor(this Thing t, FlagString fString, int colorIndex, Color color)
        {
            if (fString == null || t == null)
            {
                Log.ErrorOnce($"Tried to set subitem color with null def or thing. (Thing: {t}, Def: {fString})", 945612);
                return Color.magenta;
            }
            var subItem = CustomizableGraphic.GetFlagGraphic(t, fString, true);
            return colorIndex switch
            {
                0 => (subItem.colorA = color).Value,
                1 => (subItem.colorB = color).Value,
                2 => (subItem.colorC = color).Value,
                _ => throw new IndexOutOfRangeException($"requested color index {colorIndex}. Max index is 2."),
            };
        }
    }
}
