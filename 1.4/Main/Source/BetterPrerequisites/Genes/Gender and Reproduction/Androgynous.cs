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
    public static partial class HarmonyPatches
    {
        [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
        public static class Pawn_GeneTracker__Notify_GenesChanged
        {
            [HarmonyPostfix]
            public static void Postfix(ref Pawn ___pawn, GeneDef addedOrRemovedGene)
            {
                if (___pawn != null && addedOrRemovedGene == BSDefs.Body_Androgynous && ___pawn.gender == Gender.Male)
                {
                    ___pawn.story.bodyType = Verse.PawnGenerator.GetBodyTypeFor(___pawn);

                    if (___pawn.story.bodyType == BodyTypeDefOf.Male)
                    {
                        ___pawn.story.bodyType = BodyTypeDefOf.Female;
                    }

                    ___pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();

                    if (___pawn.story.bodyType == BodyTypeDefOf.Male)
                    {
                        ___pawn.story.bodyType = BodyTypeDefOf.Female;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Verse.PawnGenerator), nameof(Verse.PawnGenerator.GetBodyTypeFor))]
        public static class PawnGenerator
        {
            [HarmonyPostfix]
            public static void GetBodyTypeFor(Pawn pawn, ref BodyTypeDef __result)
            {
                if (pawn != null && __result == BodyTypeDefOf.Male && pawn.genes != null && pawn.genes.HasGene(BSDefs.Body_Androgynous))
                {
                    __result = BodyTypeDefOf.Female;
                }
            }

        }
    }



}
