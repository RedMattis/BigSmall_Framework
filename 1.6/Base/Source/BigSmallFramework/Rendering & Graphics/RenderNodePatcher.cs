using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class RenderNodePatcher
    {
        public static void TryPatchPawnRenderNodeDefs()
        {
            if (BigSmallMod.settings?.makeDefsRecolorable != true)
            {
                return;
            }
            var allGenes = DefDatabase<GeneDef>.AllDefsListForReading
                .Where(x=>!x.RenderNodeProperties.NullOrEmpty()
                && x.modExtensions?.Any(x=>x is HasCustomizableGraphics) != true
                && x.modExtensions?.Any(x => x is GraphicsOverride) != true
            );
            
            var allHediffs = DefDatabase<HediffDef>.AllDefsListForReading
                .Where(x => !x.RenderNodeProperties.NullOrEmpty()
                && x.modExtensions?.Any(x => x is HasCustomizableGraphics) != true
                && x.modExtensions?.Any(x => x is GraphicsOverride) != true
            );

            List<PawnRenderingProps_Lite> newNodes = [];

            FlagString hornGraphics = new("HornGraphics");
            FlagString earGraphics = new("EarGraphics");
            FlagString wingGraphics = new("WingGraphics");
            FlagString tailGraphics = new("TailGraphics");
            FlagString haloGraphics = new("HaloGraphics");
            FlagString miscHeadGraphics = new("MiscHeadGraphics");

            FlagString miscBodyGraphics = new("MiscBodyGraphics");
            FlagString headBionicGraphics = new("HeadBionicGraphics");
            FlagString bodyBionicGraphics = new("BodyBionicGraphics");
            FlagString headFleshGraphics = new("HeadFleshGraphics");
            FlagString bodyFleshGraphics = new("BodyFleshGraphics");

            foreach (var gene in allGenes)
            {
                List<FlagString> flagsAdded = [];
                for (int idx = gene.RenderNodeProperties.Count - 1; idx >= 0; idx--)
                {
                    PawnRenderNodeProperties node = gene.RenderNodeProperties[idx];
                    if (node.GetType() != typeof(PawnRenderNodeProperties))
                    {
                        // We only want to patch the exact type, not derived types, since those might have custom behavior,
                        // or expect properties we don't set here.
                        continue;
                    }
                    if (typeof(PawnRenderNode_Fur).IsAssignableFrom(node.nodeClass))
                    {
                        continue;
                    }
                    if (node.nodeClass == typeof(PawnRenderNode_AttachmentHead) || node.parentTagDef == PawnRenderNodeTagDefOf.Head)
                    {
                        if (
                            (gene.defName?.Contains("horn", StringComparison.OrdinalIgnoreCase) == true)
                            || (gene.exclusionTags?.Any(x => x.Contains("horn", StringComparison.OrdinalIgnoreCase)) == true)
                            || (gene.displayCategory?.defName?.Contains("horn", StringComparison.OrdinalIgnoreCase) == true)
                        )
                        {
                            var replacement = ReplaceWithLite(node, hornGraphics, true);
                            flagsAdded.Add(hornGraphics);
                            gene.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                        else if (
                            (gene.defName?.Contains("ears", StringComparison.OrdinalIgnoreCase) == true)
                            || (gene.exclusionTags?.Any(x => x.Contains("ears", StringComparison.OrdinalIgnoreCase)) == true)
                            || (gene.displayCategory?.defName?.Contains("ears", StringComparison.OrdinalIgnoreCase) == true)
                        )
                        {
                            var replacement = ReplaceWithLite(node, earGraphics, true);
                            flagsAdded.Add(earGraphics);
                            gene.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                        else if (
                            (gene.defName?.Contains("halo", StringComparison.OrdinalIgnoreCase) == true)
                            || (gene.exclusionTags?.Any(x => x.Contains("halo", StringComparison.OrdinalIgnoreCase)) == true)
                            || (gene.displayCategory?.defName?.Contains("halo", StringComparison.OrdinalIgnoreCase) == true)
                        )
                        {
                            var replacement = ReplaceWithLite(node, haloGraphics, true);
                            flagsAdded.Add(haloGraphics);
                            gene.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                        else
                        {
                            var replacement = ReplaceWithLite(node, miscHeadGraphics, true);
                            flagsAdded.Add(miscHeadGraphics);
                            gene.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                    }
                    else if (node.nodeClass == typeof(PawnRenderNode))
                    {
                        if (
                            (gene.defName?.Contains("wing", StringComparison.OrdinalIgnoreCase) == true)
                            || (gene.exclusionTags?.Any(x => x.Contains("wing", StringComparison.OrdinalIgnoreCase)) == true)
                            || (gene.displayCategory?.defName?.Contains("wing", StringComparison.OrdinalIgnoreCase) == true)
                        )
                        {
                            var replacement = ReplaceWithLite(node, wingGraphics, true);
                            flagsAdded.Add(wingGraphics);
                            gene.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                        else if (
                            (gene.defName?.Contains("tail", StringComparison.OrdinalIgnoreCase) == true)
                            || (gene.exclusionTags?.Any(x => x.Contains("tail", StringComparison.OrdinalIgnoreCase)) == true)
                            || (gene.displayCategory?.defName?.Contains("tail", StringComparison.OrdinalIgnoreCase) == true)
                        )
                        {
                            var replacement = ReplaceWithLite(node, tailGraphics, false);
                            flagsAdded.Add(tailGraphics);
                            gene.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                        else
                        {
                            var replacement = ReplaceWithLite(node, miscBodyGraphics, false);
                            flagsAdded.Add(miscBodyGraphics);
                            gene.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                    }
                }
                foreach(var flag in flagsAdded.Distinct())
                {
                    gene.modExtensions ??= [];
                    gene.modExtensions.Add(new HasCustomizableGraphics() { Flag = flag, colorA=true, colorB=true });
                }
            }
            foreach (var hediff in allHediffs)
            {
                List<FlagString> flagsAdded = [];
                if (hediff.addedPartProps == null) continue;
                for (int idx = hediff.RenderNodeProperties.Count - 1; idx >= 0; idx--)
                {
                    PawnRenderNodeProperties node = hediff.RenderNodeProperties[idx];
                    if (node.GetType() != typeof(PawnRenderNodeProperties))
                    {
                        continue;
                    }
                    if (node.nodeClass == typeof(PawnRenderNode_AttachmentHead))
                    {
                        if (node.colorType == PawnRenderNodeProperties.AttachmentColorType.Custom)
                        {
                            var replacement = ReplaceWithLite(node, headBionicGraphics, true);
                            flagsAdded.Add(headBionicGraphics);
                            hediff.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                        else if (node.colorType == PawnRenderNodeProperties.AttachmentColorType.Skin)
                        {
                            var replacement = ReplaceWithLite(node, headFleshGraphics, true);
                            flagsAdded.Add(headFleshGraphics);
                            hediff.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                    }
                    else if (node.nodeClass == typeof(PawnRenderNode))
                    {
                        if (node.colorType == PawnRenderNodeProperties.AttachmentColorType.Custom)
                        {
                            var replacement = ReplaceWithLite(node, bodyBionicGraphics, true);
                            flagsAdded.Add(bodyBionicGraphics);
                            hediff.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                        else if (node.colorType == PawnRenderNodeProperties.AttachmentColorType.Skin)
                        {
                            var replacement = ReplaceWithLite(node, bodyFleshGraphics, true);
                            flagsAdded.Add(bodyFleshGraphics);
                            hediff.RenderNodeProperties[idx] = replacement;
                            newNodes.Add(replacement);
                        }
                    }
                    foreach (var flag in flagsAdded.Distinct())
                    {
                        hediff.modExtensions ??= [];
                        hediff.modExtensions.Add(new HasCustomizableGraphics() { Flag = flag, colorA = true, colorB = true });
                    }
                }
            }
            Log.Message($"[Big & Small] Patched {newNodes.Count} PawnRenderNodeProperties to use {nameof(PawnRenderingProps_Lite)} for recolorable graphics.\nRemember that you can always disable this again in the mod options.");
            foreach(var node in newNodes)
            {
                node.TrySetup(forceSetup: true);
                node.EnsureInitialized();
                node.ResolveReferencesRecursive();
            }
        }

        public static PawnRenderingProps_Lite ReplaceWithLite(PawnRenderNodeProperties original, FlagString tag, bool useHeadMesh)
        {
            var newProps = new PawnRenderingProps_Lite();
            var type = original.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);// | System.Reflection.BindingFlags.NonPublic);
            var properties = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);// | System.Reflection.BindingFlags.NonPublic);
            foreach (var prop in properties)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    var value = prop.GetValue(original);
                    if (value == null || value == default) continue;
                    try
                    {
                        prop.SetValue(newProps, value);
                    }
                    catch (ArgumentException)
                    {
                        Log.WarningOnce($"[Big & Small] Failed to copy property {prop.Name} of type {prop.PropertyType} from {type} to {nameof(PawnRenderingProps_Lite)}. Skipping.", 2654322);
                    }
                }
            }

            foreach (var field in fields)
            {
                var value = field.GetValue(original);
                if (value == null || value == default) continue;
                try
                {
                    field.SetValue(newProps, value);
                }
                catch (ArgumentException)
                {
                    Log.WarningOnce($"[Big & Small] Failed to copy field {field.Name} of type {field.FieldType} from {type} to {nameof(PawnRenderingProps_Lite)}. Skipping.", 2654321);
                }
            }

            newProps.tag = tag;
            newProps.nodeClass = typeof(RenderNodeLite);
            newProps.useHeadMesh = useHeadMesh;
            if (original.shaderTypeDef == null || original.shaderTypeDef == ShaderTypeDefOf.Cutout)
            {
                newProps.shader = ShaderTypeDefOf.Cutout;
            }
            else
            {
                newProps.shader = original.shaderTypeDef;
            }
            return newProps;
        }
    }
}
