using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public class VFEGeneExtensionWrapper
    {
        private static bool? vfeLoaded = null;
        private static Type VFEGeneExtType = null;
        private static FieldInfo backgroundPathEndogenesInfo = null;
        private static FieldInfo backgroundPathXenogenesInfo = null;
        private static FieldInfo backgroundPathArchiteInfo = null;
        private static FieldInfo hideGeneInfo = null;

        public static bool IsVFEActive
        {
            get
            {
                vfeLoaded ??= ModsConfig.ActiveModsInLoadOrder.Any(x => x.PackageIdPlayerFacing == "OskarPotocki.VanillaFactionsExpanded.Core");
                return vfeLoaded.Value;
            }
        }


        public DefModExtension ext = null;
        public VFEGeneExtensionWrapper(DefModExtension existingInstance = null)
        {
            if (IsVFEActive == false)
            {
                Log.Warning("Attempted to load VFE Gene Extension Wrapper without VFE being active.");
                return;
            }
            Type type = GetExtensionType();
            if (type == null)
            {
                Log.Error("Big and Small: Could not find VanillaGenesExpanded.GeneExtension class.");
                return;
            }
            CacheData();


            if (existingInstance != null)
            {
                ext = existingInstance;
            }
            else
            {
                // Create instance of the class
                ext = (DefModExtension)Activator.CreateInstance(type);
            }
        }

        public static void CacheData()
        {
            backgroundPathEndogenesInfo ??= AccessTools.Field(VFEGeneExtType, "backgroundPathEndogenes");
            backgroundPathXenogenesInfo ??= AccessTools.Field(VFEGeneExtType, "backgroundPathXenogenes");
            backgroundPathArchiteInfo ??= AccessTools.Field(VFEGeneExtType, "backgroundPathArchite");
            hideGeneInfo ??= AccessTools.Field(VFEGeneExtType, "hideGene");
        }

        public static Type GetExtensionType()
        {
            if (VFEGeneExtType == null)
            {
                VFEGeneExtType = AccessTools.TypeByName("VanillaGenesExpanded.GeneExtension");
                if (VFEGeneExtType == null)
                {
                    Log.Error("Big and Small: Could not find VanillaGenesExpanded.GeneExtension class.");
                }
            }
            return VFEGeneExtType;
        }

        public string BackgroundPathEndogenes { get => (string)backgroundPathEndogenesInfo.GetValue(ext); set => backgroundPathEndogenesInfo.SetValue(ext, value); }
        public string BackgroundPathXenogenes { get => (string)backgroundPathXenogenesInfo.GetValue(ext); set => backgroundPathXenogenesInfo.SetValue(ext, value); }
        public string BackgroundPathArchite { get => (string)backgroundPathArchiteInfo.GetValue(ext); set => backgroundPathArchiteInfo.SetValue(ext, value); }
        public bool HideGene { get => (bool)hideGeneInfo.GetValue(ext); set => hideGeneInfo.SetValue(ext, value); }
    }
}