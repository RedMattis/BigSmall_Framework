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
        public Color? colorA;
        public Color? colorB;
        public Color? colorC;

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

        public void ExposeData()
        {
            Scribe_Values.Look(ref colorA, "colorA");
            Scribe_Values.Look(ref colorB, "colorB");
            Scribe_Values.Look(ref colorC, "colorC");
        }

        public override string ToString()
        {
            return $"[{nameof(CustomizableGraphic)}] - ColorA: {colorA}, ColorB: {colorB}, ColorC: {colorC}";
        }
    }
}
