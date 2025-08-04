using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace BigAndSmall
{
    [DefOf]
    public static class BSEDefs
    {
        public static ThingCategoryDef BS_RobotCorpses;
    }

    [HarmonyPatch]
    public static class DefGeneratorPatches
    {
        public static ThingCategoryDef TrySetToRobotCorpse(ThingCategoryDef previousDef, ThingDef pawnDef)
        {
            if (pawnDef.IsMechanicalDef())
            {
                return BSEDefs.BS_RobotCorpses;
            }
            return previousDef;
        }

        [HarmonyPatch(typeof(ThingDefGenerator_Corpses), nameof(ThingDefGenerator_Corpses.GenerateCorpseDef))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GenerateCorpseDef_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var targetMethod = AccessTools.Method(typeof(DefGeneratorPatches), nameof(TrySetToRobotCorpse));

            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                yield return code;

                // Check for loading the CorpsesHumanlike field
                if (code.opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo field && field.Name == "CorpsesHumanlike")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // Load the PawnDef (argument 0)
                    yield return new CodeInstruction(OpCodes.Call, targetMethod);
                }
            }
        }

        /// <summary>
        /// This class generates genedefs for the game. We want to run it after the game has loaded all defs and any xenotypes that might be code-generated.
        /// </summary>
        /// <param name="values"></param>
        [HarmonyPatch(typeof(GeneDefGenerator), nameof(GeneDefGenerator.ImpliedGeneDefs))]
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPostfix]
        public static void ImpliedGeneDefs_Postfix(ref IEnumerable<GeneDef> __result)
        {
            var resultList = __result.ToList();
            
            if (BigSmall.BSTransformGenes && BigSmallMod.settings.generateDefs)
            {
                foreach (var geneDef in GenerateXenotypeGenes())
                {
                    resultList.Add(geneDef);
                }
                __result = resultList;
            }
        }

        public static List<GeneDef> GenerateXenotypeGenes()
        {
            var result = new List<GeneDef>();
            var allXenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;

            // Get the Metamorphosis GeneTemplate.
            var metTemplate = DefDatabase<GeneTemplate>.GetNamed("BS_MetamorphTemplate");
            var metDownTemplate = DefDatabase<GeneTemplate>.GetNamed("BS_RetromorphDownTemplate");
            if (metTemplate == null)
            {
                Log.Warning("Big and Small DefGen: GenerateXenotypeGenes: Could not find the Metamorphosis Template. Metamorphosis genes will not be generated." +
                    "\nIf using Big and Small Genes this likely means you need to resubscribe to the mod, or that you have a config that removes the required def.");
            }
            if (metDownTemplate == null)
            {
                Log.Warning("Big and Small DefGen: GenerateXenotypeGenes: Could not find the Metamorphosis Down Template. Retromorphosis genes will not be generated." +
                    "\nIf using Big and Small Genes this likely means you need to resubscribe to the mod, or that you have a config that removes the required def.");
            }

            try
            {
                foreach (var xeno in allXenotypes)
                {
                    if (metTemplate != null)
                    {
                        var geneExt = new PawnExtension
                        {
                            metamorphTarget = xeno,
                            hideInGenePicker = false
                        };

                        result.Add(GenerateXenoTypeGene(xeno, metTemplate, geneExt, [xeno.label]));
                    }

                    if (metDownTemplate != null)
                    {
                        var geneExtTarget = new PawnExtension
                        {
                            retromorphTarget = xeno,
                            hideInGenePicker = false
                        };

                        result.Add(GenerateXenoTypeGene(xeno, metDownTemplate, geneExtTarget, [xeno.label]));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Exception duing Big and Small DefGen: GenerateXenotypeGenes.\nGenerating the genes has been aborted.\n{e.Message}\n{e.StackTrace}");
            }

            return result;
        }

        public static FieldInfo iconColor = null;
        public static GeneDef GenerateXenoTypeGene(XenotypeDef xenoDef, GeneTemplate template, DefModExtension extension, List<string> descriptionKeys)
        {
            // Validate so nothing is null.
            if (xenoDef == null || template == null || extension == null || descriptionKeys == null)
            {
                Log.Error($"Big and Small DefGen: GenerateXenoTypeGene: One of the parameters was null." +
                    $"\nXenoDef: {xenoDef}, template: {template}, extension: {extension}, descriptionKeys: {descriptionKeys}");
                return null;
            }

            string defName = $"{xenoDef.defName}_{template.keyTag}";

            var geneDef = new GeneDef
            {
                defName = defName,
                label = $"{xenoDef.label} {template.label}",
                description = template.description,
                customEffectDescriptions = template.customEffectDescriptions,
                iconPath = xenoDef.iconPath,
                biostatCpx = 0,
                biostatMet = 0,
                displayCategory = template.displayCategory,
                canGenerateInGeneSet = template.canGenerateInGeneSet,
                selectionWeight = template.selectionWeight,
                modExtensions = [extension]
            };

            for (int idx = 0; idx < descriptionKeys.Count; idx++)
            {
                geneDef.description = geneDef.description.Replace("{" + idx + "}", descriptionKeys[idx]);
            }

            if (iconColor == null)
            {
                iconColor = AccessTools.Field(typeof(GeneDef), "iconColor");
            }
            if (template.iconColor != null)
            {
                iconColor.SetValue(geneDef, template.iconColor);
            }
            else
            {
                iconColor.SetValue(geneDef, new Color(0.75f, 0.75f, 0.75f));
            }


            //if (VFEGeneExtensionWrapper.IsVFEActive == true)
            //{
            //    var vfeg = new VFEGeneExtensionWrapper(null);
            //    if (vfeg != null)
            //    {


            //        string pathEndo = template.backgroundPathEndogenes ?? "GeneIcons/BS_BackEndogene";
            //        string pathXeno = template.backgroundPathXenogenes ?? "GeneIcons/BS_BackXenogene";
            //        string pathArchite = template.backgroundPathArchite ?? "GeneIcons/BS_BackArchite_1";

            //        vfeg.BackgroundPathEndogenes = pathEndo;
            //        vfeg.BackgroundPathXenogenes = pathXeno;
            //        vfeg.BackgroundPathArchite = pathArchite;
            //        geneDef.modExtensions.Add(vfeg.ext);
            //    }
            //}
            return geneDef;
        }
    }
}
