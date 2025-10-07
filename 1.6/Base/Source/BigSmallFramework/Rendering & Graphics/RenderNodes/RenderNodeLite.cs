using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    /// <summary>
    /// Lightweight version of Ultimate rendering props for quicky setup.
    /// 
    /// TECHNICALLY supports all the same features as Ultimate, but is intended to be able to be set up quickly,
    ///     and auto-generate many properties.
    /// </summary>
    public class PawnRenderingProps_Lite : PawnRenderingProps_Ultimate //PawnRenderNodeProperties
    {
        
        [Flags]
        public enum EnumColorSource
        {
            None = 0,
            Custom = 1,
            Skin = 2,
            Hair = 4,
            Rotted = 8,
        }

        public FlagString tag = null;

        // Used by the RenderNodeAutoPatcher. Will usually be the DefName of the Def this is attached to.
        public string identifier = null;

        private Color? colorA = null;
        public Color? colorB = null;
        public Color? colorC = null;

        public List<ShaderTypeDef> userPickableShaders = [];

        // Can be used to simply link this to an "Ultimate" GraphicSetDef from XML. Will replace everything else.
        public GraphicSetDef link = null;

        public (Color? color, EnumColorSource source) GetMainColorData()
        {
            (Color? color, EnumColorSource source) result = (null, EnumColorSource.None);

            if (useRottenColor)
            {
                result.source |= EnumColorSource.Rotted;
            }
            if (colorType == AttachmentColorType.Skin)
            {
                result.source |= EnumColorSource.Skin;
            }
            else if (colorType == AttachmentColorType.Hair)
            {
                result.source |= EnumColorSource.Hair;
            }
            else if (colorType == AttachmentColorType.Custom)
            {
                result.source |= EnumColorSource.Custom;
            }
            if (colorA != null)
            {
                result.color = colorA;
            }
            else
            {
                result.color = color;
            }
            return result;
        }

        public void TrySetup(bool forceSetup = false)
        {
            if (generated != null && !forceSetup)
            {
                return;
            }
            CheckConfig();
            if (link != null)
            {
                generated = link.conditionalGraphics;
            }
            else if (GraphicSet != null)
            {
                generated = GraphicSet;
            }
            else
            {
                generated = GenerateGraphicSet();
            }
        }

        protected void CheckConfig()
        {
            if (GraphicSet != null)
            {
                Log.WarningOnce($"[BigAndSmall] {nameof(RenderNodeLite)} is being used with a full {nameof(ConditionalGraphicsSet)}." +
                    $"Consider using {nameof(PawnRenderNode_Ultimate)} instead.", 897348254);
            }
        }

        protected ConditionalGraphicsSet GenerateGraphicSet()
        {
            ColorSetting clrSetA;
            ColorSetting clrSetB = null;
            ColorSetting clrSetC = null;
            ConditionalGraphicProperties conditionalProps = null;
            if (identifier != null)
            {
                conditionalProps = new()
                {
                    shader = null,
                    replaceFlagMinPriority = 1000,
                    replaceFlags = [tag],
                    alts = [..
                        userPickableShaders.Select(shader=> new ConditionalGraphicProperties
                        {
                            shader = shader,
                            customTagGraphicIsSet = new ConditionalGraphic.HasTagGraphicOverride
                            {
                                tag = tag,
                                customFlags = [new (identifier, shader.defName)]
                            },
                        })
                    ]
                };
            }

            var (customColorA, source) = GetMainColorData();
            if (tag is FlagString flag)
            {
                clrSetA = new ColorSetting
                {
                    alts = [new ColorSetting
                    {
                        customTagGraphicIsSet = new() { tag = flag, colorA = true },
                        customClrTagA = flag,
                    }]
                };

                if (colorB != null)
                {
                    clrSetB = new ColorSetting
                    {
                        color = colorB.Value,
                        alts = [new ColorSetting { customTagGraphicIsSet = new() { tag = flag, colorB = true }, customClrTagB = flag }]
                    };
                }

                if (colorC != null)
                {
                    clrSetC = new ColorSetting
                    {
                        color = colorC.Value,
                        alts = [new ColorSetting { customTagGraphicIsSet = new() { tag = flag, colorC = true }, customClrTagC = flag }]
                    };
                }
                else
                {
                    clrSetC = new ColorSetting { color = clrSetA?.color ?? Color.white };
                }
            }
            else
            {
                clrSetA = new ColorSetting
                {
                    altDefs = [DefDatabase<ColorSettingDef>.GetNamed("BS_CustomGlobalA")],
                };

                if (colorB != null)
                {
                    clrSetB = new ColorSetting
                    {
                        color = colorB.Value,
                        altDefs = [DefDatabase<ColorSettingDef>.GetNamed("BS_CustomGlobalB")]
                    };
                }
                else
                {
                    clrSetB = new ColorSetting { color = clrSetB?.color ?? Color.white };
                }
                if (colorC != null)
                {
                    clrSetC = new ColorSetting
                    {
                        color = colorC.Value,
                        altDefs = [DefDatabase<ColorSettingDef>.GetNamed("BS_CustomGlobalC")]
                    };
                }
                else
                {
                    clrSetC = new ColorSetting { color = clrSetC?.color ?? Color.white };
                }
            }
            if (source.HasFlag(EnumColorSource.Custom))
            {
                clrSetA.color = customColorA ?? Color.white;
            }
            else
            {
                clrSetA.color = customColorA ?? Color.white;
                clrSetA.averageColors = false;
            }
            if (source.HasFlag(EnumColorSource.Skin))
            {
                clrSetA.skinColor = true;
            }
            if (source.HasFlag(EnumColorSource.Hair))
            {
                clrSetA.hairColor = true;
            }
            if (source.HasFlag(EnumColorSource.Rotted))
            {
                clrSetA.useRottedColor = true;
            }

            return new ConditionalGraphicsSet(clrSetA, clrSetB, clrSetC, conditionalProps);
        }
    }

    public class RenderNodeLite : PawnRenderNode_Ultimate
    {
        PawnRenderingProps_Lite LProps => (PawnRenderingProps_Lite)props;
        public override bool AllowTexPathFor => true;
        public RenderNodeLite(Pawn pawn, PawnRenderingProps_Lite props, PawnRenderTree tree) : base(pawn, props, tree)
        {
            LProps.TrySetup();
        }

        public RenderNodeLite(Pawn pawn, PawnRenderingProps_Lite props, PawnRenderTree tree, Apparel apparel) : base(pawn, props, tree, apparel)
        {
            LProps.TrySetup();
        }

        public RenderNodeLite(Pawn pawn, PawnRenderingProps_Lite props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh) : base(pawn, props, tree)
        {
            LProps.TrySetup();
        }

        public override Mesh GetMesh(PawnDrawParms parms)
        {
            if (parms.facing.IsHorizontal && LProps.invertEastWest)
            {
                parms.facing = parms.facing.Opposite;
            }
            return base.GetMesh(parms);
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            if (apparel == null)
            {
                return base.MeshSetFor(pawn);
            }
            if (Props.overrideMeshSize.HasValue)
            {
                return MeshPool.GetMeshSetForSize(Props.overrideMeshSize.Value.x, Props.overrideMeshSize.Value.y);
            }
            if (useHeadMesh)
            {
                return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn);
            }
            return HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn);
        }
    }
}
