using HarmonyLib;
using RimWorld;
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
        public static Color playerClr = new(0.6f, 0.6f, 1f);
        public static Color enemyClr = new(1f, 0.2f, 0.2f);
        public static Color neutralClr = new(0.45f, 0.8f, 1f);
        public static Color slaveClr = new(1f, 0.9f, 0.4f);

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
        public bool apparelColorA = false;
        public bool apparelColorB = false;
        public bool apparelStuff = false; // Only works if the item is apparel.
        public Color? color = null;
        public float? saturation = null;
        public float? hue = null;
        public float? brightness = null;

        public float? minBrightness = 0;
        public float? maxBrightness = 1;

        // By default, colors are multiplied together. If this is true, they are averaged instead.
        public bool averageColors = true;

        public List<ColorSetting> alts = [];

        [Unsaved(false)]
        private readonly static Dictionary<string, Color> randomClrPerId = [];
        public Color GetColor(PawnRenderNode renderNode, Color oldClr, string hashOffset, bool useOldColor = false)
        {
            var pawn = renderNode.tree.pawn;
            foreach (var alt in alts.Where(x => x.GetState(pawn)))
            {
                if (alt.GetColor(renderNode, oldClr, hashOffset, useOldColor) is Color altClr)
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
                    colorsAdded.Add(pawn.story.favoriteColor.Value);
                    didSet = true;
                }
                else GetHostilityStatus(pawn, ref didSet, ref colorsAdded);

            }
            if (apparelStuff || apparelColorA || apparelColorB)
            {
                if (apparelColorA && renderNode.apparel.DrawColor is Color drawColor)
                {
                    colorsAdded.Add(drawColor);
                }
                if (apparelColorB && renderNode.apparel.DrawColor is Color drawColorB)
                {
                    colorsAdded.Add(drawColorB);
                }
                if (renderNode.apparel.Stuff is ThingDef stuffThing)
                {
                    colorsAdded.Add(renderNode.apparel.def.GetColorForStuff(stuffThing));
                    didSet = true;
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
                    colorsAdded.Add(pawn.story.favoriteColor.Value);
                    didSet = true;
                }
                else GetHostilityStatus(pawn, ref didSet, ref colorsAdded);
            }
            if (color != null)
            {
                colorsAdded.Add(color.Value);
                didSet = true;
            }
            if (hostilityStatus) GetHostilityStatus(pawn, ref didSet, ref colorsAdded);
            if (colourRange != null)
            {
                var id = pawn.thingIDNumber;
                var clrId = hashOffset + id;
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

            if (saturation != null || hue != null || brightness != null || minBrightness != null || maxBrightness != null)
            {
                Color.RGBToHSV(finalClr, out float hue, out float sat, out float val);
                if (saturation != null)
                    sat *= saturation.Value;
                if (this.hue != null)
                    hue = this.hue.Value;
                if (brightness != null)
                    val *= brightness.Value;
                if (minBrightness != null)
                    val = Mathf.Max(minBrightness.Value, val);
                if (maxBrightness != null)
                    val = Mathf.Min(maxBrightness.Value, val);

                sat = Mathf.Clamp01(sat);
                val = Mathf.Clamp01(val);

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
