﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class ModExtHelper
    {
        // public methods.
        public static List<PawnExtension> GetAllPawnExtensions(Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true)
        {
            return [.. GetHediffExtensions<PawnExtension>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetGeneExtensions<PawnExtension>(pawn, parentWhitelist, parentBlacklist, doSort)];
        }

        public static List<T> GetAllExtensions<T>(Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            return [.. GetHediffExtensions<T>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetGeneExtensions<T>(pawn, parentWhitelist, parentBlacklist, doSort)];
        }
        
        public static List<T> GetHediffExtensions<T>(Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            List<ModExtWrapper<T>> matches = GetAllMatchingExtensionsFromHediffSetWithSource<T>(pawn);
            return GetFilteredResult(matches, parentWhitelist, parentBlacklist, doSort);
        }

        public static List<T> GetGeneExtensions<T>(Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            List<ModExtWrapper<T>> matches = GetAllMatchingExtensionsFromActiveGenes<T>(pawn);
            return GetFilteredResult(matches, parentWhitelist, parentBlacklist, doSort);
        }

        // Private use by class.
        public class ModExtWrapper<T>(T extension, Type sourceType, int priority) where T : DefModExtension
        {
            public T extension = extension;
            public Type sourceType = sourceType;
            public int priority = priority;
        }

        private static List<T> GetFilteredResult<T>(List<ModExtWrapper<T>> matches, List<Type> parentWhitelist=null, List<Type> parentBlacklist = null, bool doSort=true) where T : DefModExtension
        {
            List<T> extensions = [];
            if (doSort)
            {
                matches.OrderByDescending(a => a.priority);
            }
            foreach (var match in matches)
            {
                if (parentWhitelist != null && !parentWhitelist.Contains(match.sourceType)) continue;
                if (parentBlacklist != null && parentBlacklist.Contains(match.sourceType)) continue;
                extensions.Add(match.extension);
            }
            return extensions;
        }
        private static List<ModExtWrapper<T>> GetAllMatchingExtensionsFromHediffSetWithSource<T>(Pawn pawn) where T : DefModExtension
        {
            List<ModExtWrapper<T>> extensions = [];
            if (pawn.health?.hediffSet == null) return extensions;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                extensions.AddRange(GetAllMatchingExtensions<T>(hediff.def, hediff.GetType()));
            }
            return extensions;
        }

        private static List<ModExtWrapper<T>> GetAllMatchingExtensionsFromActiveGenes<T>(Pawn pawn) where T : DefModExtension
        {
            List<ModExtWrapper<T>> extensions = [];
            if (pawn.genes == null) return extensions;
            var activeGenes = GeneHelpers.GetAllActiveGenes(pawn);
            foreach (var gene in activeGenes)
            {
                extensions.AddRange(GetAllMatchingExtensions<T>(gene.def, gene.GetType()));
            }
            return extensions;
        }

        private static List<ModExtWrapper<T>> GetAllMatchingExtensions<T>(Def def, Type source) where T : DefModExtension
        {
            List<ModExtWrapper<T>> extensions = [];
            if (def.modExtensions == null) return extensions;
            foreach (DefModExtension extension in def.modExtensions)
            {
                if (extension is T t)
                {
                    int defaultPriority = 0;
                    if (source == typeof(RaceTracker))
                    {
                        defaultPriority = -100; // Ensures that stuff grabbing "FirstOrDefault" will not get this if there are other options.
                    }
                    else if (def is GeneDef)
                    {
                        defaultPriority = 0;
                    }
                    else if (def is HediffDef)
                    {
                        defaultPriority = 100;
                    }
                    extensions.Add(new ModExtWrapper<T>(t, source, extension is PawnExtension p ? p.priority : defaultPriority));
                }
            }
            return extensions;
        }
    }
}