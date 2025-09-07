using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class RaceViewUIManager
    {
        private static readonly List<GeneDef> geneDefs = [];
        private static readonly List<IRacialFeature> racialFeatures = [];
        private static readonly List<HediffDef> displayedHediffs = [];

        private static readonly List<Gene> xenogenes = [];

        private static readonly List<Gene> endogenes = [];

        private static float genesHeight;

        private static float racialHeight;

        private static float scrollHeight;

        private static int gcx;

        private static int met;

        private static int arc;

        private static readonly CachedTexture RaceBackground_Bio = new("GeneIcons/BS_BackRaceBio");

        private static readonly CachedTexture RaceBackground_Mech = new("GeneIcons/BS_BackRaceMech");


        private const float OverriddenGeneIconAlpha = 0.75f;

        private const float XenogermIconSize = 34;

        private const float XenotypeLabelWidth = 140f;

        private const float GeneGap = 6f;

        private const float GeneSize = 90f;

        public const float BiostatsWidth = 38f;

        public static float BiostatsHeight() => Text.LineHeight * 3f;

        public static void DrawRacialInfo(Rect rect, Thing target, float initialHeight, ref Vector2 size, ref Vector2 scrollPosition, GeneSet pregnancyGenes = null)
        {
            size.y = initialHeight;
            Rect rect2 = rect;
            Rect position = rect2.ContractedBy(10f);
            if (Prefs.DevMode)
            {
                var debugRect = new Rect(rect2.xMax - 18f - 125f, 5f, 115f, Text.LineHeight);
                if (ModsConfig.BiotechActive)
                {
                    GeneUIUtility.DoDebugButton(new Rect(rect2.xMax - 18f - 125f, 5f, 115f, Text.LineHeight), target, pregnancyGenes);
                }
                Debugging.DebugUIPatches.DoGeneDebugButton(ref debugRect, target);
            }
            GUI.BeginGroup(position);
            float num = BiostatsHeight();
            Rect rect3 = new(0f, 0f, position.width, position.height - num - 12f);
            DrawFeatureSection(rect3, target, pregnancyGenes, ref scrollPosition);
            Rect rect4 = new(0f, rect3.yMax + GeneGap, position.width - XenotypeLabelWidth - 4f, num)
            {
                yMax = rect3.yMax + num + GeneGap
            };
            if (target is not Pawn)
            {
                rect4.width = position.width;
            }
            if (ModsConfig.BiotechActive)
            {
                BiostatsTable.Draw(rect4, gcx, met, arc, drawMax: false, ignoreLimits: false);
                TryDrawXenotype(target, rect4.xMax + 4f, rect4.y + Text.LineHeight / 2f);
            }
            if (Event.current.type == EventType.Layout)
            {
                genesHeight = 0f;
                racialHeight = 0f;
            }
            GUI.EndGroup();
        }

        private static void DrawFeatureSection(Rect rect, Thing target, GeneSet genesOverride, ref Vector2 scrollPosition)
        {
            RecacheEntries(target, genesOverride);
            GUI.BeginGroup(rect);
            Rect rect2 = new(0f, 0f, rect.width - 16f, scrollHeight);
            float curY = 0f;
            Widgets.BeginScrollView(rect.AtZero(), ref scrollPosition, rect2);
            Rect containingRect = rect2;
            containingRect.y = scrollPosition.y;
            containingRect.height = rect.height;
            if (target is Pawn pawn && HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                
                if (racialFeatures.Any())
                {
                    string title = cache.isMechanical ? "BS_RacialFeatures_Mech" : "BS_RacialFeatures";
                    DrawSection(rect, title, "BS_RacialFeatureDescription", racialFeatures.Count, ref curY, ref racialHeight, delegate (int i, Rect r)
                    {
                        DrawFeature(racialFeatures[i], cache, r);
                    }, containingRect);
                }

                if (ModsConfig.BiotechActive)
                {
                    if (endogenes.Any())
                    {
                        GeneUIUtility.DrawSection(rect, xeno: false, endogenes.Count, ref curY, ref genesHeight, delegate (int i, Rect r)
                        {
                            GeneUIUtility.DrawGene(endogenes[i], r, GeneType.Endogene);
                        }, containingRect);
                        curY += 12f;
                    }
                    if (xenogenes.Any())
                    {
                        GeneUIUtility.DrawSection(rect, xeno: true, xenogenes.Count, ref curY, ref genesHeight, delegate (int i, Rect r)
                        {
                            GeneUIUtility.DrawGene(xenogenes[i], r, GeneType.Xenogene);
                        }, containingRect);
                    }
                }
            }
            else
            {
                if (ModsConfig.BiotechActive)
                {
                    GeneType geneType = ((genesOverride == null && target is not HumanEmbryo) ? GeneType.Xenogene : GeneType.Endogene);
                    GeneUIUtility.DrawSection(rect, geneType == GeneType.Xenogene, geneDefs.Count, ref curY, ref genesHeight, delegate (int i, Rect r)
                    {
                        GeneUIUtility.DrawGeneDef(geneDefs[i], r, geneType);
                    }, containingRect);
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                scrollHeight = curY;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private static void RecacheEntries(Thing target, GeneSet genesOverride)
        {
            racialFeatures.Clear();
            //HashSet<string> featureKeysAdded = [];
            geneDefs.Clear();
            xenogenes.Clear();
            endogenes.Clear();
            gcx = 0;
            met = 0;
            arc = 0;

            if (target is Pawn pawn && HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                //if (cache.isMechanical)
                //{
                //    racialFeatures.AddDistinct(BSDefs.BS_Mechanical);
                //}
                racialFeatures.AddDistinctRange(cache.racialFeatures);

                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    var pawnExtensionsOnHediff = hediff.def.GetAllPawnExtensionsOnHediff();
                    var pawnExtensionsWithIcon = pawnExtensionsOnHediff.Where(x => x.traitIcon != null);
                    if (!pawnExtensionsWithIcon.Any())
                    {
                        continue;
                    }
                    var featureDef = new RacialFeatureDef
                    {
                        label = hediff.Label.CapitalizeFirst(),
                        description = hediff.Description,
                        iconPath = pawnExtensionsWithIcon.First().traitIcon,
                        hediffDescriptionSource = hediff.def,
                    };

                    try
                    {
                        pawnExtensionsOnHediff.TryGetDescription(out string extDescription);
                        if (!string.IsNullOrEmpty(extDescription))
                        {
                            featureDef.description += "\n\n" + extDescription;
                        }
                        racialFeatures.Add(featureDef);
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorOnce($"Failed to get PawnExt description{hediff.def.defName}: {ex} {ex.StackTrace}", 149782384);
                    }
                }

                GeneSet geneSet = (target as GeneSetHolderBase)?.GeneSet ?? genesOverride;
                if (pawn.genes != null)
                {
                    foreach (Gene xenogene in pawn.genes.Xenogenes)
                    {
                        if (!xenogene.Overridden)
                        {
                            AddBiostats(xenogene.def);
                        }
                        xenogenes.Add(xenogene);
                    }
                    foreach (Gene endogene in pawn.genes.Endogenes)
                    {
                        if (endogene.def.endogeneCategory != EndogeneCategory.Melanin || !pawn.genes.Endogenes.Any((Gene x) => x.def.skinColorOverride.HasValue))
                        {
                            if (!endogene.Overridden)
                            {
                                AddBiostats(endogene.def);
                            }
                            endogenes.Add(endogene);
                        }
                    }
                    xenogenes.SortGenes();
                    endogenes.SortGenes();
                }
                else
                {
                    if (geneSet == null)
                    {
                        return;
                    }
                    foreach (GeneDef item in geneSet.GenesListForReading)
                    {
                        geneDefs.Add(item);
                    }
                    gcx = geneSet.ComplexityTotal;
                    met = geneSet.MetabolismTotal;
                    arc = geneSet.ArchitesTotal;
                    geneDefs.SortGeneDefs();
                }
            }
            static void AddBiostats(GeneDef gene)
            {
                gcx += gene.biostatCpx;
                met += gene.biostatMet;
                arc += gene.biostatArc;
            }
            racialFeatures.Sort((a, b) => a.Label.CompareTo(b.Label));
        }

        private static void DrawSection(Rect rect, string title, string description, int count, ref float curY, ref float sectionHeight, Action<int, Rect> drawer, Rect containingRect)
        {
            Widgets.Label(10f, ref curY, rect.width, title.Translate().CapitalizeFirst(), description.Translate());
            float num = curY;
            Rect rect2 = new(rect.x, curY, rect.width, sectionHeight);
            Widgets.DrawMenuSection(rect2);
            float num2 = (rect.width - 12f - 630f - 36f) / 2f;
            curY += num2;
            int num3 = 0;
            int num4 = 0;
            for (int i = 0; i < count; i++)
            {
                if (num4 >= 6)
                {
                    num4 = 0;
                    num3++;
                }
                else if (i > 0)
                {
                    num4++;
                }
                Rect rect3 = new(num2 + num4 * GeneSize + num4 * GeneGap, curY + num3 * GeneSize + num3 * GeneGap, GeneSize, GeneSize);
                if (containingRect.Overlaps(rect3))
                {
                    drawer(i, rect3);
                }
            }
            curY += (num3 + 1) * GeneSize + num3 * GeneGap + num2;
            if (Event.current.type == EventType.Layout)
            {
                sectionHeight = curY - num;
            }
        }

        private static void TryDrawXenotype(Thing target, float x, float y)
        {
            if (target is not Pawn sourcePawn || sourcePawn.genes == null)
            {
                return;
            }
            Rect rect = new(x, y, XenotypeLabelWidth, Text.LineHeight);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(rect, sourcePawn.genes.XenotypeLabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            Rect position = new(rect.center.x - 17f, rect.yMax + 4f, XenogermIconSize, XenogermIconSize);
            GUI.color = XenotypeDef.IconColor;
            GUI.DrawTexture(position, sourcePawn.genes.XenotypeIcon);
            GUI.color = Color.white;
            rect.yMax = position.yMax;
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, () => ("Xenotype".Translate() + ": " + sourcePawn.genes.XenotypeLabelCap).Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + sourcePawn.genes.XenotypeDescShort, 883938493);
            }
            if (Widgets.ButtonInvisible(rect) && !sourcePawn.genes.UniqueXenotype)
            {
                Find.WindowStack.Add(new Dialog_InfoCard(sourcePawn.genes.Xenotype));
            }
        }

        /// <summary>
        /// Draw Race Feature
        /// </summary>
        public static void DrawFeature(IRacialFeature iRF, BSCache cache, Rect featureRect, bool doBackground = true, bool clickable = true)
        {
            DrawFeatureBasics(iRF, cache, featureRect, doBackground, clickable, false);
            if (Mouse.IsOver(featureRect))
            {
                string text = iRF.Label.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + iRF.DescriptionFull;
                text = text + "\n" + "ClickForMoreInfo".Translate().ToString().Colorize(ColoredText.SubtleGrayColor);
                TooltipHandler.TipRegion(featureRect, text);
            }
        }

        public static void FeatureDefIcon(Rect rect, IRacialFeature iRF, float scale = 1f, Color? color = null, Material material = null)
        {
            GUI.color = color ?? iRF.IconColor;
            Widgets.DrawTextureFitted(rect, iRF.Icon, scale, material);
            GUI.color = Color.white;
        }
        private static void DrawFeatureBasics(IRacialFeature iRF, BSCache cache, Rect featureRect, bool doBackground, bool clickable, bool overridden)
        {
            GUI.BeginGroup(featureRect);
            Rect rect = featureRect.AtZero();
            if (doBackground)
            {
                Widgets.DrawHighlight(rect);
                GUI.color = new Color(1f, 1f, 1f, 0.05f);
                Widgets.DrawBox(rect);
                GUI.color = Color.white;
            }
            float num = rect.width - Text.LineHeight;
            Rect rect2 = new(featureRect.width / 2f - num / 2f, 0f, num, num);
            Color iconColor = iRF.IconColor;
            if (overridden)
            {
                iconColor.a = OverriddenGeneIconAlpha;
                GUI.color = ColoredText.SubtleGrayColor;
            }
            CachedTexture cachedTexture = cache.isMechanical ? RaceBackground_Mech : RaceBackground_Bio;
            GUI.DrawTexture(rect2, cachedTexture.Texture);
            FeatureDefIcon(rect2, iRF, 0.9f, iconColor);
            Text.Font = GameFont.Tiny;
            float num2 = Text.CalcHeight(iRF.Label, rect.width);
            Rect rect3 = new(0f, rect.yMax - num2, rect.width, num2);
            GUI.DrawTexture(new Rect(rect3.x, rect3.yMax - num2, rect3.width, num2), TexUI.GrayTextBG);
            Text.Anchor = TextAnchor.LowerCenter;
            if (overridden)
            {
                GUI.color = ColoredText.SubtleGrayColor;
            }
            if (doBackground && num2 < (Text.LineHeight - 2f) * 2f)
            {
                rect3.y -= 3f;
            }
            Widgets.Label(rect3, iRF.Label.CapitalizeFirst());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            if (clickable)
            {
                if (Widgets.ButtonInvisible(rect))
                {
                    if (iRF is RacialFeatureDef racialFeatureDef)
                    Find.WindowStack.Add(new Dialog_InfoCard(racialFeatureDef));
                }
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
            }
            GUI.EndGroup();
        }

        private static void DrawStat(Rect iconRect, CachedTexture icon, string stat, float iconWidth)
        {
            GUI.DrawTexture(iconRect, icon.Texture);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.LabelFit(new Rect(iconRect.xMax, iconRect.y, 38f - iconWidth, iconWidth), stat);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawBiostats(int gcx, int met, int arc, ref float curX, float curY, float margin = GeneGap)
        {
            float num = GeneCreationDialogBase.GeneSize.y / 3f;
            float num2 = 0f;
            float num3 = Text.LineHeightOf(GameFont.Small);
            Rect iconRect = new(curX, curY + margin + num2, num3, num3);
            DrawStat(iconRect, GeneUtility.GCXTex, gcx.ToString(), num3);
            Rect rect = new(curX, iconRect.y, 38f, num3);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, "Complexity".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "ComplexityDesc".Translate());
            }
            num2 += num;
            if (met != 0)
            {
                Rect iconRect2 = new(curX, curY + margin + num2, num3, num3);
                DrawStat(iconRect2, GeneUtility.METTex, met.ToStringWithSign(), num3);
                Rect rect2 = new(curX, iconRect2.y, 38f, num3);
                if (Mouse.IsOver(rect2))
                {
                    Widgets.DrawHighlight(rect2);
                    TooltipHandler.TipRegion(rect2, "Metabolism".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "MetabolismDesc".Translate());
                }
                num2 += num;
            }
            if (arc > 0)
            {
                Rect iconRect3 = new(curX, curY + margin + num2, num3, num3);
                DrawStat(iconRect3, GeneUtility.ARCTex, arc.ToString(), num3);
                Rect rect3 = new(curX, iconRect3.y, 38f, num3);
                if (Mouse.IsOver(rect3))
                {
                    Widgets.DrawHighlight(rect3);
                    TooltipHandler.TipRegion(rect3, "ArchitesRequired".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "ArchitesRequiredDesc".Translate());
                }
            }
            curX += XenogermIconSize;
        }
    }

}
