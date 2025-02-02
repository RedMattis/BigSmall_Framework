using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall.ModPatches
{

    public static class FacialAnim_PatchDynamicRaces
    {
        public static void PatchFaceAdjustmentDict(List<ThingDef> racesToAdd)
        {
            if (!ModsConfig.IsActive("Nals.FacialAnimation")) { return; }
            // Get the "FacialAnimation.GraphicHelper" static class from the assembly
            var graphicHelperStaticClass = AccessTools.TypeByName("FacialAnimation.GraphicHelper");

            // Get the faceAdjustmentDict from it.
            var faceAdjustmentDictField = AccessTools.Field(graphicHelperStaticClass, "faceAdjustmentDict");
            var faceAdjustmentDict = faceAdjustmentDictField.GetValue(null);

            // Get the static "FacialAnimation.FaceAdjustmentDefOf.DefaultFaceSizeAndPositionDef" from the assembly
            var faceAdjustmentDefOfStaticClass = AccessTools.TypeByName("FacialAnimation.FaceAdjustmentDefOf");
            var defaultFaceSizeAndPositionDef = AccessTools.Field(faceAdjustmentDefOfStaticClass, "DefaultFaceSizeAndPositionDef").GetValue(null);

            // Get the indexer property of the dictionary
            var indexerProperty = faceAdjustmentDict.GetType().GetProperty("Item");

            // Add or update the default face size and position def for each race
            foreach (var race in racesToAdd)
            {
                indexerProperty.SetValue(faceAdjustmentDict, defaultFaceSizeAndPositionDef, new object[] { race.defName });
            }
        }
    }
}
