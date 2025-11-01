﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace BigAndSmall
{
    public static partial class HarmonyPatches
    {
        private static bool HasSOS => ModsConfig.IsActive("kentington.saveourship2");

        private static bool NotNull(params object[] input)
        {
            if (input.All(o => o != null))
            {
                return true;
            }

            //Log.Message("Signature match not found");
            foreach (var obj in input)
            {
                if (obj is MemberInfo memberObj)
                {
                    //Log.Message($"\tValid entry:{memberObj}");
                }
            }

            return false;
        }
    }

    public static class GameUtils
    {
        public static void RecacheStatsForThing(this Thing someThing)
        {
            DefDatabase<StatDef>.AllDefsListForReading
                .Where(x => x.immutable)
                .Do(x => x.Worker.ClearCacheForThing(someThing));
        }

        public static void UnhealingRessurection(Pawn pawn)
        {
            ////// Save Hediffs of all wounds on the pawn.
            List<(HediffDef, BodyPartRecord)> missingParts = [];

            // Foreach hediff per body part
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_MissingPart)
                {
                    var missingPart = hediff as Hediff_MissingPart;
                    var part = missingPart.Part;
                    missingParts.Add((hediff.def, part));
                }
            }

            // Ressurect the pawn

            ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams
            {
                restoreMissingParts = false,
            });

            //////// Re-apply the wounds. Counting backwards
            /// Eeeey! This is supported by vanilla now!
            /// The below code is probably not needed anymore.
            //for (int i = missingParts.Count - 1; i >= 0; i--)
            //{
            //    // Get the current hediff
            //    var hediff = missingParts[i];

            //    if (!pawn.health.hediffSet.PartIsMissing(hediff.Item2))
            //    {
            //        // Add the hediff back to the pawn
            //        pawn.health.AddHediff(hediff.Item1, part: hediff.Item2);
            //    }
            //}
        }
    }
}
