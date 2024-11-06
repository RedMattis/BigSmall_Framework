using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static BigAndSmall.RenderingLib;

namespace BigAndSmall
{
    public static class GraphicsHelpers
    {
        public static Graphic GetBlankMaterial(Pawn pawn) => GraphicDatabase.Get<Graphic_Multi>("UI/EmptyImage", ShaderUtility.GetSkinShader(pawn), Vector2.one, pawn.story.SkinColor);

        public static Color GetColorFromColourListRange(this List<Color> colorList, float rngValue, float rngValue2)
        {
            // If there is only one color, return it.
            if (colorList.Count == 1)
                return colorList[0];

            // Get two random adjacent colors from the list.
            int index1 = (int)Mathf.Lerp(0, colorList.Count - 2, rngValue);
            int index2 = index1 + 1;

            Color color1 = colorList[index1];
            Color color2 = colorList[index2];

            float interp = rngValue2;
            return color1 * (1 - interp) + color2 * interp;
        }

        public static Graphic_Multi TryGetCustomGraphics(Pawn pawn, string path, Color colorOne, Color colorTwo, Vector2 drawSize, CustomMaterial data)
        {
            if (data != null)
            {
                return data.GetGraphic(pawn, path, colorOne, colorTwo, drawSize, data);
            }
            else
            {
                return GetCachableGraphics(path, drawSize, ShaderTypeDefOf.Cutout, colorOne, colorTwo);
            }
        }
    }
    

    public static class RenderingLib
    {
        [Unsaved(false)]
        private readonly static List<KeyValuePair<(Color, Color), Graphic_Multi>> graphics = [];
        public static Graphic_Multi GetCachableGraphics(string path, Vector2 drawSize, ShaderTypeDef shader, Color colorOne, Color colorTwo)
        {
            shader ??= ShaderTypeDefOf.CutoutComplex;

            for (int i = 0; i < graphics.Count; i++)
            {
                var grap = graphics[i];
                var grapMult = grap.Value;
                if (grapMult.path == path && colorOne.IndistinguishableFrom(graphics[i].Key.Item1) && colorTwo.IndistinguishableFrom(grap.Key.Item2) && grap.Value.Shader == shader.Shader)
                {
                    return graphics[i].Value;
                }
            }

            Graphic_Multi graphic_Multi = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(path, shader.Shader, drawSize, colorOne, colorTwo);//, data:null, maskPath: null);

            //<T>(string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, GraphicData data, string maskPath = null) where T : Graphic, new()
            //Graphic Get(Type graphicClass, string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, string maskPath = null)
            graphics.Add(new KeyValuePair<(Color, Color), Graphic_Multi>((colorOne, colorTwo), graphic_Multi));
            return graphic_Multi;
        }

        public class ColorSetting
        {
            // Hello reader! Feel free to request more triggers if needed.
            public enum AltTrigger
            {
                Colonist,
                SlaveOfColony,
                PrisonerOfColony,
                SlaveOrPrisoner,
                OfColony,
                Unconcious,
                Dead,
                Rotted,
                Dessicated,
                HasForcedSkinColorGene,
                BiotechDLC,
                IdeologyDLC,
                AnomalyDLC,
            }

            public const string clrOneKey = "someKeyStringClrOne";
            public const string clrTwoKey = "clrTwoKeyString";
            public static Color playerClr = Color.white; // Literally no change...
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
            public Color? color = null;
            public float? saturation = null;
            public float? hue = null;
            public float? brightness = null;

            public float? minBrightness = 0;
            public float? maxBrightness = 1;

            public List<ColorSetting> alts = [];
            public List<AltTrigger> triggers = [];
            public float? chanceTrigger = null;

            //public float? alpha = null;

            public bool AltIsValid(Pawn pawn)
            {
                if (chanceTrigger != null)
                {
                    using (new RandBlock(pawn.thingIDNumber + pawn.def.defName.GetHashCode()))
                    {
                        if (Rand.Value > chanceTrigger.Value)
                        {
                            return false;
                        }
                    }
                }

                if (triggers.Count == 0) return true;
                return triggers.All(x=> x switch
                {
                    AltTrigger.Colonist => pawn.Faction == Faction.OfPlayer,
                    AltTrigger.SlaveOfColony => pawn.HostFaction == Faction.OfPlayer && pawn.IsSlave,
                    AltTrigger.PrisonerOfColony => pawn.HostFaction == Faction.OfPlayer && pawn.IsPrisoner,
                    AltTrigger.SlaveOrPrisoner => pawn.IsSlave || pawn.IsPrisoner,
                    AltTrigger.OfColony => pawn.HostFaction == Faction.OfPlayer || pawn.Faction == Faction.OfPlayer,
                    AltTrigger.Unconcious => pawn.Downed && !pawn.health.CanCrawl,
                    AltTrigger.Dead => pawn.Dead,
                    AltTrigger.Rotted => pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Rotting,
                    AltTrigger.Dessicated => pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated,
                    AltTrigger.HasForcedSkinColorGene => GeneHelpers.GetAllActiveGenes(pawn).Any(x => x.def.skinColorOverride != null),
                    AltTrigger.BiotechDLC => ModsConfig.BiotechActive,
                    AltTrigger.IdeologyDLC => ModsConfig.IdeologyActive,
                    AltTrigger.AnomalyDLC => ModsConfig.AnomalyActive,
                    _ => false,
                });
            }

            [Unsaved(false)]
            private readonly static Dictionary<string, Color> randomClrPerId = new();
            public Color GetColor(Pawn pawn, Color oldClr, string hashOffset, bool useOldColor=false)
            {
                foreach (var alt in alts.Where(x=>x.AltIsValid(pawn)))
                {
                    if (alt.GetColor(pawn, oldClr, hashOffset, useOldColor) is Color altClr)
                    {
                        return altClr;
                    }
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
                Color finalClr = useOldColor ? oldClr : Color.white;
                if (pawn?.story != null)
                {
                    if (hairColor)
                    {
                        finalClr *= pawn.story.HairColor;
                        didSet = true;
                    }
                    if (skinColor)
                    {
                        finalClr *= pawn.story.SkinColor;
                        didSet = true;
                    }
                }
                if (factionColor) GetFactionColor(pawn, ref didSet, ref finalClr);
                if (ideologyColor)
                {
                    if (pawn.Ideo?.Color is Color iColor)
                    {
                        finalClr *= iColor;
                        didSet = true;
                    }
                    else GetFactionColor(pawn, ref didSet, ref finalClr);
                }
                if (primaryIdeologyColor)
                {
                    if (pawn.Faction?.ideos?.PrimaryIdeo?.Color is Color piColor)
                    {
                        finalClr *= piColor;
                        didSet = true;
                    }
                    else GetFactionColor(pawn, ref didSet, ref finalClr);

                }
                if (favoriteColor)
                {
                    if (pawn.story?.favoriteColor != null)
                    {
                        finalClr *= pawn.story.favoriteColor.Value;
                        didSet = true;
                    }
                    else GetHostilityStatus(pawn, ref didSet, ref finalClr);
                    
                }
                if (apparelColorOrFavorite)
                {
                    bool foundClredApparel = false;
                    if (pawn.apparel.WornApparel.Count > 0)
                    {
                        var allApparenClrs = pawn.apparel.WornApparel.Where(x=>x.def?.graphic?.Color != null).Select(x => x.def.graphic.Color);
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
                            finalClr *= mostCommonClr;
                            foundClredApparel = true;
                            didSet = true;
                        }
                    }
                    if (!foundClredApparel && pawn.story?.favoriteColor != null)
                    {
                        finalClr *= pawn.story.favoriteColor.Value;
                        didSet = true;
                    }
                    else GetHostilityStatus(pawn, ref didSet, ref finalClr);
                }
                if (color != null)
                {
                    finalClr *= color.Value;
                    didSet = true;
                }
                if (hostilityStatus) GetHostilityStatus(pawn, ref didSet, ref finalClr);
                if (colourRange != null)
                {
                    var id = pawn.thingIDNumber;
                    var clrId = hashOffset + id;
                    if (randomClrPerId.TryGetValue(clrId, out Color savedClr))
                    {
                        finalClr *= savedClr;
                        didSet = true;
                    }
                    else
                    {
                        string strToHash = hashOffset + id + id + id + id;

                        // This generates a deterministic value from 0 to 1 based on the id.
                        float randomValue = Mathf.Abs((strToHash.GetHashCode() % 200) / 200f);
                        float randomValue2 = Mathf.Abs((strToHash.GetHashCode() % 333) / 333f);
                        Color rngColor = GraphicsHelpers.GetColorFromColourListRange(colourRange, randomValue, randomValue2);
                        randomClrPerId[clrId] = rngColor;
                        finalClr *= rngColor;
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

                static void GetFactionColor(Pawn pawn, ref bool didSet, ref Color finalClr)
                {
                    if (pawn.Faction?.Color is Color fColor)
                    {
                        finalClr *= fColor;
                        didSet = true;
                    }
                }

                static void GetHostilityStatus(Pawn pawn, ref bool didSet, ref Color finalClr)
                {
                    var pStatus = pawn.GuestStatus;
                    if (pStatus == GuestStatus.Prisoner)
                    {
                        finalClr *= slaveClr;
                        didSet = true;
                    }
                    else if (pStatus == GuestStatus.Slave)
                    {
                        finalClr *= slaveClr;
                        didSet = true;
                    }
                    else if (pStatus == GuestStatus.Guest)
                    {
                        finalClr *= neutralClr;
                        didSet = true;
                    }
                    else if (pawn.HostileTo(Faction.OfPlayer))
                    {
                        finalClr *= enemyClr;
                        didSet = true;
                    }
                    else if (pawn.Faction != Faction.OfPlayer)
                    {
                        finalClr *= neutralClr;
                        didSet = true;
                    }
                }
            }
        }
    }
}
