using AlienRace;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using VariedBodySizes;
using Verse;
using static AlienRace.AlienPartGenerator;

namespace BigAndSmall
{
    public static partial class HarmonyPatches
    {
        public static Vector2 HAR_FloatV2_V2(object val)
            => val is Vector2 lifestageFactorV2 ? lifestageFactorV2
                                                : val is float lifestageFactorF ? new Vector2(lifestageFactorF, lifestageFactorF)
                                                : Vector2.one;

        [HarmonyPatch(typeof(AlienRace.HarmonyPatches), nameof(AlienRace.HarmonyPatches.GetHumanlikeBodySetForPawnHelper))]
        public static class GetHumanlikeBodySetForPawnHelper_Patch
        {
            public static void Prefix(ref object lifestageFactor, ref Pawn pawn)
            {
                lifestageFactor = HAR_FloatV2_V2(lifestageFactor) * HumanoidPawnScaler.GetBSDict(pawn).bodyRenderSize;
            }
        }

        [HarmonyPatch(typeof(AlienRace.HarmonyPatches), nameof(AlienRace.HarmonyPatches.GetHumanlikeHeadSetForPawnHelper))]
        public static class GetHumanlikeHeadSetForPawnHelper_Patch
        {
            public static void Prefix(ref object lifestageFactor, ref Pawn pawn)
            {
                lifestageFactor = HAR_FloatV2_V2(lifestageFactor) * HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;
            }
        }

        [HarmonyPatch(typeof(AlienRace.HarmonyPatches), nameof(AlienRace.HarmonyPatches.GetHumanlikeHairSetForPawnHelper))]
        public static class GetHumanlikeHairSetForPawnHelper_Patch
        {
            public static void Prefix(ref Vector2 headFactor, ref Pawn pawn)
            {
                headFactor *= HumanoidPawnScaler.GetBSDict(pawn).headRenderSize;
            }
        }

        //[HarmonyPatch(typeof(AlienRace.HarmonyPatches), nameof(AlienRace.HarmonyPatches.GetBodyOverlayMeshSetPostfix))]
        //public static class GetBodyOverlayMeshSetPostfix_Patch
        //{
        //    public static void Prefix(PawnRenderer instance, Pawn pawn, ref GraphicMeshSet result)
        //    {
        //        if (BigSmall.performScaleCalculations && BigSmall.humnoidScaler != null)
        //        {
        //            lifestageFactor = HAR_FloatV2_V2(lifestageFactor) * HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //        }
        //    }
        //}

        //Apply scale to body addons.

        //[HarmonyPatch(typeof(AlienRace.HarmonyPatches), "DrawAddons.DrawAddon")]
        //public static void Prefix(Pawn pawn, BodyAddon addon, Rot4 rot, ref Graphic graphic, ref Vector3 offsetVector, ref float angle, ref Material mat)
        //{
        //    Debug.Log($"DrawAddonsFinalHook: {pawn.Name} {addon.bodyPart} {addon.drawSize} {addon.angle}");
        //    addon.drawSize *= HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //    graphic.drawSize *= HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //    if (BigSmall.performScaleCalculations && BigSmall.humnoidScaler != null)
        //    {
        //        addon.drawSize *= HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //        graphic.drawSize *= HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //        //offsetVector.x *= HumanoidPawnScaler.GetHeadRenderSize(pawn);
        //        //offsetVector.z *= HumanoidPawnScaler.GetHeadRenderSize(pawn);
        //    }
        //}

        //[HarmonyPatch(typeof(AlienRace.HarmonyPatches), nameof(AlienRace.HarmonyPatches.DrawAddonsFinalHook))]
        // public static void Prefix(Pawn pawn, BodyAddon addon, Rot4 rot, ref Graphic graphic, ref Vector3 offsetVector, ref float angle, ref Material mat)
        // {
        //     Debug.Log($"DrawAddonsFinalHook: {pawn.Name} {addon.bodyPart} {addon.drawSize} {addon.angle}");
        //     addon.drawSize *= HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //     graphic.drawSize *= HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //     if (BigSmall.performScaleCalculations && BigSmall.humnoidScaler != null)
        //     {
        //         addon.drawSize *= HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //         graphic.drawSize *= HumanoidPawnScaler.GetBodyRenderSize(out _, pawn);
        //         //offsetVector.x *= HumanoidPawnScaler.GetHeadRenderSize(pawn);
        //         //offsetVector.z *= HumanoidPawnScaler.GetHeadRenderSize(pawn);
        //     }
        // }



        //[HarmonyPatch(typeof(AlienRace.HarmonyPatches), nameof(AlienRace.HarmonyPatches.GetBorderSizeForPawn))]
        //public static class GetBorderSizeForPawn_Patch
        //{
        //    public static void Postfix(ref float __result)
        //    {
        //        //if (BigSmall.performScaleCalculations && BigSmall.humnoidScaler != null)
        //        //{
        //        //    Pawn pawn = AlienRace.HarmonyPatches.createPawnAtlasPawn;
        //        //    float scaleValue = HumanoidPawnScaler.GetBodyRenderSize(pawn);
        //        //    Log.Message($"scale value is... {scaleValue}");
        //        //    scaleValue = Mathf.Max(1, scaleValue);
        //        //    if (scaleValue != 1)
        //        //    {
        //        //        __result *= scaleValue * 3;
        //        //        Log.Message($"Scaled to {scaleValue}");
        //        //    }
        //        //}
        //        __result *= 2;
        //    }
        //}
    }

}
