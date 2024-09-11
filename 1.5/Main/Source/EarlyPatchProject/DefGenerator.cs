using BetterPrerequisites;
using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BSXeno
{
    [HarmonyPatch]
    public static class DefGenerator
    {
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
            
            if (BigSmall.BSGenesActive && BigSmallMod.settings.generateDefs)
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

            foreach (var xeno in allXenotypes)
            {
                var geneExt = new GeneExtension
                {
                    metamorphTarget = xeno,
                    hideInXenotypeUI = true
                };

                result.Add(GenerateXenoTypeGene(xeno, metTemplate, geneExt, new List<string> { xeno.label }));

                var geneExtTarget = new GeneExtension
                {
                    retromorphTarget = xeno,
                    hideInXenotypeUI = true
                };

                result.Add(GenerateXenoTypeGene(xeno, metDownTemplate, geneExtTarget, new List<string> { xeno.label }));
            }

            return result;
        }

        public static FieldInfo iconColor = null;
        public static GeneDef GenerateXenoTypeGene(XenotypeDef xenoDef, GeneTemplate template, DefModExtension extension, List<string> descriptionKeys)
        {
            //const string prefix = "XM";

            string defName = $"{xenoDef.defName}_{template.keyTag}";

            var geneDef = new GeneDef
            {
                defName = defName,
                label = $"{xenoDef.label} {template.label}",
                description = template.description,
                iconPath = xenoDef.iconPath,
                biostatCpx = 0,
                biostatMet = 0,
                displayCategory = template.displayCategory,
                canGenerateInGeneSet = template.canGenerateInGeneSet,
                selectionWeight = template.selectionWeight,
                modExtensions = new List<DefModExtension> { extension }
            };

            for (int idx = 0; idx < descriptionKeys.Count; idx++)
            {
                geneDef.description.Replace("{" + $"{idx}" + "}" , descriptionKeys[idx]);
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


            if (VFEGeneExtensionWrapper.IsVFEActive == true)
            {
                var vfeg = new VFEGeneExtensionWrapper(null);
                if (vfeg != null)
                {
                    vfeg.BackgroundPathEndogenes = template.backgroundPathEndogenes ?? "GeneIcons/BS_BackEndogene";
                    vfeg.BackgroundPathXenogenes = template.backgroundPathXenogenes ?? "GeneIcons/BS_BackXenogene";
                    vfeg.BackgroundPathArchite = template.backgroundPathArchite ?? "GeneIcons/BS_BackArchite_1";
                    vfeg.HideGene = true;
                    geneDef.modExtensions.Add(vfeg.ext);
                }
            }
            return geneDef;
        }
    }
}
