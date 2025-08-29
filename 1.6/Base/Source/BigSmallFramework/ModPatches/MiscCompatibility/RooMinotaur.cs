using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class Herculean_CanEquip_Postfix_Patch
    {
        private static readonly string[] rooMethods = new string[1]
        {
            "Herculean_Patches:CanEquip_Postfix",
        };

        public static bool Prepare()
        {
            string[] methods = rooMethods;
            for (int i = 0; i < methods.Length; i++)
            {
                if (!(AccessTools.Method(methods[i]) == null))
                {
                    return true;
                }
                else
                {
                    // Remove these warning later.
                    //Log.Warning($"DEBUG: Failed to find method to patch ({methods[i]})");
                }
            }
            return false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            string[] methods = rooMethods;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo methodInfo = AccessTools.Method(methods[i]);
                if (!(methodInfo == null))
                {
                    //Log.Message($"Big & Small found Roo's Minotaurs and Postfixed ({methods[i]})");
                    yield return methodInfo;
                }
                else
                {
                    // Remove these warning later.
                }
            }
        }

        //[HarmonyPrefix]
        public static void Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
        {
            if (__result == false && cantReason.Contains("Herculean"))
            {
                var matchingTraits = pawn.story.traits.allTraits.Where(x => x.def.defName == "BS_Giant");

                if (matchingTraits.Count() > 0)
                {
                    cantReason = "Probably a mod conflict :|";
                    __result = true;
                }
                else if (pawn?.BodySize >= 1.999f)
                {
                    cantReason = "Probably a mod conflict :|";
                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch]
    public static class RoosCrushedPelvis_Patch
    {
        /// <summary>
        /// For the sake of... consistency? Adds Roos Crushed Pelvis behaviour to giants as well.
        /// </summary>
        [HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
        [HarmonyPostfix]
        public static void MakeNewToils_Postfix(ref JobDriver_Lovin __instance, ref Verse.AI.TargetIndex ___PartnerInd)
        {
            try
            {
                Pawn Partner = (Pawn)__instance.job.GetTarget(___PartnerInd);

                if (Partner?.story != null && __instance?.pawn?.story != null)
                {
                    // Get pawn trait of name "BS_Giant"
                    var matchingTraits = Partner.story.traits.allTraits.Where(x => x.def.defName == "BS_Giant");

                    // If the pawn has the Gentle trait, abort.
                    bool isGentle = Partner.story.traits?.HasTrait(BSDefs.BS_Gentle) == true || Partner.story.traits?.HasTrait(TraitDefOf.Kind) == true;

                    // The nullifying traits/genes should make it fine to try (and fail) to apply it to other giants.
                    // If not we'll need to check for that.

                    if (matchingTraits?.Any() == true && !isGentle)
                    {
                        // Get list of all possible memories
                        List<ThoughtDef> allThoughts = DefDatabase<ThoughtDef>.AllDefsListForReading;
                        ThoughtDef crushedMasochist = allThoughts.Find(x => x.defName == "RBM_CrushedMasochist");
                        ThoughtDef crushed = allThoughts.Find(x => x.defName == "RBM_Crushed");


                        if (__instance.pawn.story.traits?.HasTrait(BSDefs.Masochist) == true && crushedMasochist != null)  //Give a positive version to masochists
                        {
                            __instance.pawn.needs?.mood?.thoughts?.memories.TryGainMemory(crushedMasochist);
                        }
                        else if (crushed != null)
                        {
                            __instance.pawn.needs?.mood?.thoughts?.memories.TryGainMemory(crushed);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"{e.Message}\n{e.StackTrace}");
            }
        }
    }
}
