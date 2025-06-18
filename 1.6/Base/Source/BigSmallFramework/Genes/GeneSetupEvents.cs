using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch]
    public static class GeneSetupHarmonyPatches
    {

        [HarmonyPatch(typeof(PawnGenerator), "GenerateGenes")]
        [HarmonyPostfix]
        public static void GenerateGenes_Postfix(Pawn pawn, XenotypeDef xenotype, PawnGenerationRequest request)
        {
            
            if (ModsConfig.BiotechActive && xenotype.GetForcedRace() is (ThingDef forcedRace, bool force))
            {
                try
                {
                    pawn.SwapThingDef(forcedRace, state: true, targetPriority: 0, force: force);
                }
                catch (Exception e)
                {
                    Log.Error($"Error while trying to swap {pawn.Name} to {forcedRace.defName} during GenerateGenes step: {e.Message}");
                }
            }
        }

    }
}
