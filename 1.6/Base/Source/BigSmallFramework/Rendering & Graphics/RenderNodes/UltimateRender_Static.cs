using RimWorld;
using System;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;
using static BigAndSmall.RenderingLib;
using static System.Net.Mime.MediaTypeNames;

namespace BigAndSmall
{
    public interface IUltimateRendering
    {
        public PawnRenderNode Base { get; }
        public bool ScaleSet { get; set; }
        public Vector2 CachedScale { get; set; }
        public ShaderTypeDef ShaderOverride { get; set; }
    }

    public static class PRN_Ultimate
    {
        public static readonly string noImage = "BS_Blank";
        public static Graphic GraphicFor(Pawn pawn, IUltimateRendering uNode, PawnRenderingProps_Ultimate UProps)
        {
            var props = UProps;
            var node = uNode.Base;
            if (HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
            {
                var graphicSet = props.GraphicSet.GetGraphicsSet(cache);
                var texPath = graphicSet.GetPath(cache, noImage);
                var maskPath = graphicSet.GetMaskPath(cache, null);
                var conditionalProps = graphicSet.props.GetGraphicProperties(cache);

                if (conditionalProps.drawSize != Vector2.one)
                {
                    uNode.ScaleSet = true;
                    uNode.CachedScale = conditionalProps.drawSize;
                    uNode.ShaderOverride = conditionalProps.shader;
                }

                if (texPath.NullOrEmpty())
                {
                    Log.WarningOnce($"[BigAndSmall] No texture path for {pawn}. Returning empty image.", node.GetHashCode());
                    return GraphicDatabase.Get<Graphic_Single>(noImage);
                }
                if (UProps.autoBodyTypeMasks == true)
                {
                    maskPath ??= texPath; // In the unlikely event that the masks have bodytypes but the texPath doesn't.
                    maskPath = GetBodyTypedPath(pawn.story.bodyType, maskPath);
                }
                if (UProps.autoBodyTypePaths == true)
                {
                    texPath = GetBodyTypedPath(pawn.story.bodyType, texPath);
                }
                if (maskPath == texPath)  // Ensure that the default Ludeon logic for masks gets used. (e.g. `path + "_m"`)
                {
                    maskPath = null;
                }

                Color colorOne = graphicSet.ColorA.GetColor(node, Color.white, ColorSetting.clrOneKey);
                Color colorTwo = graphicSet.ColorB.GetColor(node, Color.white, ColorSetting.clrTwoKey);
                Color colorThree = graphicSet.ColorC.GetColor(node, Color.white, ColorSetting.clrThreeKey);
                Shader shader;
                if (uNode.ShaderOverride != null)
                {
                    shader = uNode.ShaderOverride.Shader;
                }
                else
                {
                    shader = props.shader?.Shader ?? ShaderTypeDefOf.CutoutComplex.Shader;
                    if (UProps.useSkinShader)
                    {
                        Shader skinShader = ShaderUtility.GetSkinShader(pawn);
                        if (skinShader != null)
                        {
                            shader = skinShader;
                        }
                    }
                }
                    
                
                
                return GetCachableGraphics(texPath, Vector2.one, shader, colorOne, colorTwo, colorThree, maskPath: maskPath);
            }

            Log.WarningOnce($"No cache found by {uNode} for {pawn}. Returning empty image.", node.GetHashCode());
            return GraphicDatabase.Get<Graphic_Single>(noImage);
        }

        public static string GetBodyTypedPath(BodyTypeDef bodyType, string basePath)
        {
            if (bodyType == null)
            {
                Log.Error("Attempted to get graphic with undefined body type.");
                bodyType = BodyTypeDefOf.Male;
            }
            if (basePath.NullOrEmpty())
            {
                return basePath;
            }
            return basePath + "_" + bodyType.defName;
        }
    }
}