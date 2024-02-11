using BigAndSmall;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    [HarmonyPatch(typeof(NegativeInteractionUtility), nameof(NegativeInteractionUtility.NegativeInteractionChanceFactor))]
    public static class NegativeInteractionChanceFactor_Patch
    {
        public static void Postfix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (initiator.story.traits.HasTrait(BSDefs.BS_Gentle))
            {
                __result *= 0.05f;
            }
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_KindWords), nameof(InteractionWorker_KindWords.RandomSelectionWeight))]
    public static class InteractionWorker_KindWords_Patch
    {
        public static void Postfix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (initiator.story.traits.HasTrait(BSDefs.BS_Gentle))
            {
                __result = 0.01f;
            }
        }
    }

}
