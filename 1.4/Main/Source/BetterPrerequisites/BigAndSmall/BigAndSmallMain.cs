
using System.Linq;

using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Threading;
using BetterPrerequisites;

namespace BigAndSmall
{
    /// <summary>
    /// Main class of the "Big and Small Races" mod. This mod intends to add dwarves and ogres using the Rimworld Gene system.
    /// </summary>

    [StaticConstructorOnStartup]
    public static class BigSmall
    {
        public static Thread mainThread = null;

        public static Pawn activePawn;

        public static bool performScaleCalculations = true;

        public static readonly List<ThingDef> AllPawnTypes;

        public static readonly Dictionary<float, Mesh> CachedMeshes;

        public static readonly Dictionary<float, Mesh> CachedInvertedMeshes;

        public static Dictionary<Pawn, BSCache> BSCache = null;

        public static bool BSGenesActive = false;

        /// <summary>
        /// So we can query the BodySize from before our changes. You'll get infinite recursion without this btw. :3
        /// </summary>

        static BigSmall()
        {
            mainThread = Thread.CurrentThread;

            AllPawnTypes = (from def in DefDatabase<ThingDef>.AllDefsListForReading
                            where def.race != null
                            orderby def.label
                            select def).ToList();
            CachedMeshes = new Dictionary<float, Mesh>();
            CachedInvertedMeshes = new Dictionary<float, Mesh>();
            BSCache = new Dictionary<Pawn, BSCache>();

            BSGenesActive = ModLister.HasActiveModWithName("RedMattis.BigSmall.Core");
        }

        public static Mesh GetPawnMesh(float size, bool inverted)
        {
            if (inverted)
            {
                if (!CachedInvertedMeshes.ContainsKey(size))
                {
                    CachedInvertedMeshes[size] = MeshMakerPlanes.NewPlaneMesh(1.5f * size, flipped: true, backLift: true);
                }

                return CachedInvertedMeshes[size];
            }

            if (!CachedMeshes.ContainsKey(size))
            {
                CachedMeshes[size] = MeshMakerPlanes.NewPlaneMesh(1.5f * size, flipped: false, backLift: true);
            }

            return CachedMeshes[size];
        }
    }

    [HarmonyPatch]
    public static class NotifyEvents
    {
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPrefix]
        public static void PawnKillPrefix(Pawn __instance)
        {
            // Go over all hediffs
            foreach(var hediff in __instance.health.hediffSet.hediffs)
            {
                // Remove pilots from pawns if possible.
                if(hediff is Piloted piloted)
                {
                    piloted.RemovePilots();
                }
            }
        }
    }


}
