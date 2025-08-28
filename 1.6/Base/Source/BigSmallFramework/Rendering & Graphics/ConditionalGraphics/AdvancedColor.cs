using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public class ColorSettingDef : Def
    {
        public ColorSetting color = new();
    }

    public class ColorSetting : ConditionalGraphic
    {
        public const string clrOneKey = "someKeyStringClrOne";
        public const string clrTwoKey = "clrTwoKeyString";
        public const string clrThreeKey = "zomgClrThree";
        public static Color playerClr = new(0.6f, 0.6f, 1f);
        public static Color enemyClr = new(1f, 0.2f, 0.2f);
        public static Color neutralClr = new(0.45f, 0.8f, 1f);
        public static Color slaveClr = new(1f, 0.9f, 0.4f);
        public static List<Color> allLeatherColors = null;

        public ColorSettingDef replacementDef = null;
        public List<ColorSettingDef> altDefs = [];

        public Color? color = null;
        public bool hairColor = false;
        public bool skinColor = false;
        public bool factionColor = false;
        public bool ideologyColor = false;
        public bool primaryIdeologyColor = false;
        public bool hostilityStatus = false;
        public bool invisibleIfDead = false;
        public bool invisibleIfUnconcious = false;
        public bool apparelColorOrFavorite = false;
        public bool favoriteColor = false;
        public List<Color> colourRange = null;
        public bool apparelColorA = false; // Only works if the item is apparel.
        public bool apparelStuff = false; 
        public bool randomLeatherColor = false;

        // Customizable. These can be changed after the game has started.
        public Color? customColorA = null;  // Value will be the default if no other setting exists.
        public Color? customColorB = null;
        public Color? customColorC = null;

        // Similar to the above but tied to a pawn's specific genes/hediff (since they aren't Things).
        public FlagString customClrTagA = null;
        public FlagString customClrTagB = null;
        public FlagString customClrTagC = null;

        // Color transformation.
        public float? saturation = null;
        public float? hue = null;
        public float? hueRotate = null;
        public float? brightness = null;
        public float? brightnessFlat = null;
        

        // TODO: Swap these for FloatRange in 1.6.
        public float minBrightness = 0;
        public float maxBrightness = 1;
        public float minSaturation = 0;
        public float maxSaturation = 1;

        // Transform Complex
        public bool invertBrightness = false;
        public float? invertValueIfBelow = null;
        public float? invertValueIfAbove = null;
        public float? temperatureComplementary = null;
        public float? temperatureAnalogous = null;

        /*
        Optional overrides for inverts.These can usually be left at default.
         
        Darkness will Lerp towards minimum invertDarknessValueRange.
        Lightness will Lerp towards maximum invertLightnessValueRange.
        */
        public float makeDarkLerp = 0.65f;
        public float makeDarkSaturationScale = 1.2f;
        public FloatRange makeDarkValueRange = new(0.35f, 1);
        public FloatRange makeDarkSaturationRange = new(0, 1);

        public float makeLightLerp = 0.85f;
        public float makeLightSaturationScale = 0.78f;
        public FloatRange makeLightValueRange = new(0, 1.0f);
        public FloatRange makeLightSaturationRange = new(0, 1);

        // By default, colors are averaged. If this is false, they are multiplied together instead.
        public bool averageColors = true;

        public List<ColorSetting> alts = [];

        [Unsaved(false)]
        private readonly static Dictionary<string, Color> randomClrPerId = [];

        public List<ColorSettingDef> AltDefs => replacementDef == null ? [.. altDefs] : [.. altDefs, replacementDef];
        /// <summary>
        /// A list of all loaded leathers in the game.
        /// </summary>
        public List<Color> AllLeatherColors => allLeatherColors ??= DefDatabase<ThingDef>.AllDefsListForReading
            .Where(x => x.IsLeather && x.graphic?.Color != null).Select(x => x.graphic.Color).ToList();
        public Color GetColor(PawnRenderNode renderNode, Color oldClr, string hashOffset, bool useOldColor = false)
        {
            var pawn = renderNode.tree.pawn;
            if (pawn.Drawer.renderer.StatueColor is Color statueClr)
            {
                return statueClr;
            }
            foreach (var alt in alts.Where(x => x.GetState(pawn, node: renderNode)))
            {
                if (alt.GetColor(renderNode, oldClr, hashOffset, useOldColor) is Color altClr)
                {
                    return altClr;
                }
            }
            foreach (var altDef in AltDefs.Where(x => x.color.GetState(pawn, node: renderNode)))
            {
                if (altDef.color.GetColor(renderNode, oldClr, hashOffset, useOldColor) is Color altClr)
                {
                    return altClr;
                }
            }

            Color? subDefResult = null;
            foreach (var gfxOverride in GetGraphicOverrides(pawn))
            {
                gfxOverride.graphics.OfType<ColorSetting>().Where(x => x != null).Do(x =>
                {
                    subDefResult = x.GetColor(renderNode, oldClr, hashOffset, useOldColor);
                    if (subDefResult != null) oldClr = subDefResult.Value;  // Carry over values in case we have many.
                });
            }
            if (subDefResult != null)
            {
                return subDefResult.Value;
            }

            if (invisibleIfDead && pawn.Dead)
            {
                return new(0, 0, 0, 0);
            }
            if (invisibleIfUnconcious && pawn.Downed && !pawn.health.CanCrawl)
            {
                return new(0, 0, 0, 0);
            }

            var id = pawn.thingIDNumber;
            var apparel = renderNode.GetApparelFromNode();
            if (apparel != null) id = apparel.thingIDNumber;
            var clrId = hashOffset + id;

            bool didSet = false;
            List<Color> colorsAdded = [];
            //Color finalClr = useOldColor ? oldClr : Color.white;
            if (pawn?.story != null)
            {
                if (hairColor)
                {
                    //finalClr *= pawn.story.HairColor;
                    colorsAdded.Add(pawn.story.HairColor);
                    didSet = true;
                }
                if (skinColor)
                {
                    //finalClr *= pawn.story.SkinColor;
                    colorsAdded.Add(pawn.story.SkinColor);
                    didSet = true;
                }
            }
            if (factionColor) GetFactionColor(pawn, ref didSet, ref colorsAdded);
            if (ideologyColor)
            {
                if (pawn.Ideo?.Color is Color iColor)
                {
                    colorsAdded.Add(iColor);
                    didSet = true;
                }
                else GetFactionColor(pawn, ref didSet, ref colorsAdded);
            }
            if (primaryIdeologyColor)
            {
                if (pawn.Faction?.ideos?.PrimaryIdeo?.Color is Color piColor)
                {
                    colorsAdded.Add(piColor);
                    didSet = true;
                }
                else GetFactionColor(pawn, ref didSet, ref colorsAdded);

            }
            if (favoriteColor)
            {
                if (pawn.story?.favoriteColor != null)
                {
                    colorsAdded.Add(pawn.story.favoriteColor.color);
                    didSet = true;
                }
                else GetHostilityStatus(pawn, ref didSet, ref colorsAdded);

            }
            if (apparelStuff || apparelColorA)
            {
                if (apparelColorA && apparel.DrawColor is Color drawColor)
                {
                    colorsAdded.Add(drawColor);
                    didSet = true;
                }
                else if (apparel.Stuff is ThingDef stuffThing)
                {
                    colorsAdded.Add(apparel.def.GetColorForStuff(stuffThing));
                    didSet = true;
                }
            }
            if (randomLeatherColor)
            {
                using (new RandBlock(id))
                {
                    colorsAdded.Add(AllLeatherColors.RandomElement());
                }
            }
            if (apparelColorOrFavorite)
            {
                bool foundClredApparel = false;
                if (pawn.apparel.WornApparel.Count > 0)
                {
                    var allApparenClrs = pawn.apparel.WornApparel.Where(x => x.def?.graphic?.Color != null).Select(x => x.def.graphic.Color);
                    if (allApparenClrs.Count() > 0)
                    {
                        // Get the apparel number there is the highest amount of using IndistinguishableFrom
                        var grouppedClrs = allApparenClrs.Aggregate(new Dictionary<Color, int>(), (dict, color) =>
                        {
                            // Find a key that is indistinguishable from the current color
                            Color? match = dict.Keys.Any() ? dict.Keys.FirstOrDefault(existingColor => existingColor.IndistinguishableFrom(color)) : null;

                            if (match != null)
                            {
                                // Increment the count for the matching color group
                                dict[match.Value]++;
                            }
                            else
                            {
                                // Add a new group for this color
                                dict[color] = 1;
                            }

                            return dict;
                        });
                        var mostCommonClr = grouppedClrs.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                        colorsAdded.Add(mostCommonClr);
                        foundClredApparel = true;
                        didSet = true;
                    }
                }
                if (!foundClredApparel && pawn.story?.favoriteColor != null)
                {
                    colorsAdded.Add(pawn.story.favoriteColor.color);
                    didSet = true;
                }
                else GetHostilityStatus(pawn, ref didSet, ref colorsAdded);
            }
            if (color != null)
            {
                colorsAdded.Add(color.Value);
                didSet = true;
            }
            Thing customTarget = apparel ?? (Thing)pawn;
            if (customColorA != null)
            {
                var clr = CustomizableGraphic.Get(customTarget)?.colorA ?? customColorA.Value;
                colorsAdded.Add(clr);
                didSet = true;
            }
            if (customColorB != null)
            {
                var clr = CustomizableGraphic.Get(customTarget)?.colorB ?? customColorB.Value;
                colorsAdded.Add(clr);
                didSet = true;
            }
            if (customColorC != null)
            {
                var clr = CustomizableGraphic.Get(customTarget)?.colorC ?? customColorC.Value;
                colorsAdded.Add(clr);
                didSet = true;
            }
            if (customClrTagA != null && CustomizableGraphic.GetTagItemGraphic(pawn, customClrTagA)?.colorA is Color clrA)
            {
                colorsAdded.Add(clrA);
                didSet = true;
            }
            if (customClrTagB != null && CustomizableGraphic.GetTagItemGraphic(pawn, customClrTagB)?.colorB is Color clrB)
            {
                colorsAdded.Add(clrB);
                didSet = true;
            }
            if (customClrTagC != null && CustomizableGraphic.GetTagItemGraphic(pawn, customClrTagC)?.colorC is Color clrC)
            {
                colorsAdded.Add(clrC);
                didSet = true;
            }

            if (hostilityStatus) GetHostilityStatus(pawn, ref didSet, ref colorsAdded);
            if (colourRange != null)
            {
                
                if (randomClrPerId.TryGetValue(clrId, out Color savedClr))
                {
                    //finalClr *= savedClr;
                    colorsAdded.Add(savedClr);
                    didSet = true;
                }
                else
                {
                    string strToHash = hashOffset + id + id + id + id;

                    // This generates a deterministic value from 0 to 1 based on the id.
                    float randomValue = Mathf.Abs((strToHash.GetHashCode() % 200) / 200f);
                    float randomValue2 = Mathf.Abs((strToHash.GetHashCode() % 333) / 333f);
                    Color rngColor = GraphicsHelper.GetColorFromColourListRange(colourRange, randomValue, randomValue2);
                    randomClrPerId[clrId] = rngColor;
                    //finalClr *= rngColor;
                    colorsAdded.Add(rngColor);
                    didSet = true;
                }
            }
            //if (forcedSkinColorGene && GeneHelpers.GetAllActiveGenes(pawn) is HashSet<Gene> genes)
            //{
            //    foreach(var gene in genes)
            //    {
            //        if (gene.def.skinColorOverride != null)
            //        {
            //            // Set to inital value.
            //            //finalClr = useOldColor ? oldClr : Color.white;
            //            finalClr *= gene.def.skinColorOverride.Value;
            //            didSet = true;
            //            break;
            //        }
            //    }
            //}

            Color finalClr = useOldColor ? oldClr : Color.white;
            if (colorsAdded.Count > 0) {
                if (averageColors)
                {
                    float r = 0, g = 0, b = 0;
                    foreach (var color in colorsAdded)
                    {
                        r += color.r;
                        g += color.g;
                        b += color.b;
                    }
                    int count = colorsAdded.Count;
                    finalClr = new Color(r / count, g / count, b / count);

                    didSet = true;
                }
                else
                {
                    // Multiply together instead.
                    finalClr = colorsAdded.Aggregate((x, y) => x * y);
                    didSet = true;
                }
            }
            if (temperatureComplementary != null || temperatureAnalogous != null)
            {
                Color.RGBToHSV(finalClr, out float hue, out float sat, out float val);
                const float hueRot = 0.07f;
                const float redRange = 0.1f + hueRot;
                const float warmRange = 0.25f + hueRot; // Red -> Yellow.
                const float coolRangeMid = 0.45f + hueRot;
                hue = Mathf.Repeat(hue+hueRot, 1f);  // Rotate colors so warm-ish purple colors starts at 0.
                if (temperatureAnalogous != null)
                {
                    var orgHue = hue;
                    if (hue <= warmRange)
                    {
                        hue = hue < redRange ? Mathf.Lerp(hue, warmRange, 0.5f) : Mathf.Lerp(hue, hueRot, 0.75f);
                    }
                    else
                    {
                        hue = hue < coolRangeMid ? Mathf.Lerp(hue, 1, 0.5f) : Mathf.Lerp(hue, warmRange, 0.35f);
                    }
                    hue = Mathf.Lerp(orgHue, hue, temperatureAnalogous.Value);
                }
                if (temperatureComplementary != null)
                {
                    var orgHue = hue;
                    if (hue <= redRange)
                    {
                        hue -= 0.35f;
                    }
                    else if (hue <= warmRange)
                    {
                        hue += 0.35f;
                    }
                    else if (hue <= coolRangeMid)
                    {
                        hue -= 0.35f; 
                    }
                    else
                    {
                        hue += 0.35f;
                    }
                    hue = Mathf.Lerp(orgHue, hue, temperatureComplementary.Value);
                    hue = Mathf.Repeat(hue, 1f);
                }
                hue = Mathf.Repeat(hue - hueRot, 1f);
                finalClr = Color.HSVToRGB(hue, sat, val);
            }

            // Transform the final color.
            if (saturation != null || hue != null || hueRotate != null || brightness != null || brightnessFlat != null ||
                minBrightness != 0 || maxBrightness != 1 || invertBrightness || minSaturation != 0 || maxSaturation != 1 ||
                invertValueIfAbove != null || invertValueIfBelow != null)
            {
                Color.RGBToHSV(finalClr, out float hue, out float sat, out float val);
                float pBright = 0.21f * finalClr.r + 0.72f * finalClr.g + 0.07f * finalClr.b;

                if (brightness != null)
                {
                    val *= brightness.Value;
                    pBright *= brightness.Value;
                }
                if (brightnessFlat != null)
                {
                    val += brightnessFlat.Value;
                    pBright += brightnessFlat.Value;
                }

                pBright = Mathf.Clamp01(pBright);
                val = Mathf.Clamp01(val);

                float iPBright = 1.0f - pBright;

                if (invertBrightness)
                {
                    if (pBright < 0.55f)
                    {
                        MakeBright(ref sat, ref val);
                    }
                    else
                    {
                        MarkDark(pBright, iPBright, ref sat, ref val);
                    }
                }
                else if (invertValueIfBelow != null && invertValueIfBelow > pBright)
                {
                    MakeBright(ref sat, ref val);
                }
                else if (invertValueIfAbove != null && invertValueIfAbove < pBright)
                {
                    MarkDark(pBright, iPBright, ref sat, ref val);
                }

                if (saturation != null)
                    sat *= saturation.Value;
                if (this.hue != null)
                    hue = this.hue.Value;
                if (hueRotate != null)
                    hue = Mathf.Repeat(hue + hueRotate.Value, 1f);

                sat = Mathf.Max(minSaturation, sat);
                sat = Mathf.Min(maxSaturation, sat);
                val = Mathf.Max(minBrightness, val);
                val = Mathf.Min(maxBrightness, val);

                finalClr = Color.HSVToRGB(hue, sat, val);
                didSet = true;
            }

            if (didSet)
            {
                return finalClr;
            }
            else
            {
                return oldClr;
            }

            static void GetFactionColor(Pawn pawn, ref bool didSet, ref List<Color> finalClr)
            {
                if (pawn.Faction?.Color is Color fColor)
                {
                    finalClr.Add(fColor);
                    didSet = true;
                }
            }

            void MarkDark(float pBright, float iPBright, ref float sat, ref float val)
            {
                val = Mathf.Min(val * iPBright / pBright, Mathf.Lerp(val, makeDarkValueRange.min, makeDarkLerp));
                // Saturate so it lookss less greyed/shadowed.
                sat = makeDarkSaturationRange.ClampToRange(sat * makeDarkSaturationScale); 
            }

            void MakeBright(ref float sat, ref float val)
            {
                val = Mathf.Lerp(val, makeLightValueRange.max, makeLightLerp);
                val = makeLightValueRange.ClampToRange(val);
                // Desaturate to wash it out a bit and avoid oversaturation.
                sat = makeLightSaturationRange.ClampToRange(makeLightSaturationScale * sat);
            }

            static void GetHostilityStatus(Pawn pawn, ref bool didSet, ref List<Color> finalClr)
            {
                var pStatus = pawn.GuestStatus;
                if (pStatus == GuestStatus.Prisoner)
                {
                    finalClr.Add(slaveClr);
                    didSet = true;
                }
                else if (pStatus == GuestStatus.Slave)
                {
                    finalClr.Add(slaveClr);
                    didSet = true;
                }
                else if (pStatus == GuestStatus.Guest)
                {
                    finalClr.Add(neutralClr);
                    didSet = true;
                }
                else if (pawn.HostileTo(Faction.OfPlayer))
                {
                    finalClr.Add(enemyClr);
                    didSet = true;
                }
                else if (pawn.Faction != Faction.OfPlayer)
                {
                    finalClr.Add(playerClr);
                    didSet = true;
                }
            }
        }
        
    }
}
