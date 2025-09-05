using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BigAndSmall
{
    public static class ModExtHelper
    {
        // public methods.
        public static List<PawnExtension> GetAllPawnExtensions(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true, bool includeInactiveGenes = false)
        {
            if (includeInactiveGenes)
            {
                return [.. GetHediffExtensions<PawnExtension>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetAllGeneExtensions<PawnExtension>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetAllActiveTraitExtensions<PawnExtension>(pawn, parentWhitelist, parentBlacklist, doSort)];
            }
            else
            {
                return [.. GetHediffExtensions<PawnExtension>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetActiveGeneExtensions<PawnExtension>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetAllActiveTraitExtensions<PawnExtension>(pawn, parentWhitelist, parentBlacklist, doSort)
                    ];
            }
        }
        public static List<PawnExtension> GetAllPawnExtensionsOnHediff(this HediffDef hediffDef, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) =>
            ExtensionsOnDef<PawnExtension, HediffDef>(hediffDef, parentWhitelist, parentBlacklist, doSort);
        
        public static List<PawnExtension> GetAllPawnExtensionsOnGene(this GeneDef geneDef, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) =>
            ExtensionsOnDef<PawnExtension, GeneDef>(geneDef, parentWhitelist, parentBlacklist, doSort);
        
        public static List<PawnExtension> GetAllPawnExtensions(this Def def, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) =>
            ExtensionsOnDef<PawnExtension, Def>(def, parentWhitelist, parentBlacklist, doSort);

        public static List<T> GetAllExtensions<T>(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            return [.. GetHediffExtensions<T>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetActiveGeneExtensions<T>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetAllActiveTraitExtensions<T>(pawn, parentWhitelist, parentBlacklist, doSort)
                    ];
        }
        public static List<T> GetAllExtensionsPlusInactive<T>(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            return [.. GetHediffExtensions<T>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetAllGeneExtensions<T>(pawn, parentWhitelist, parentBlacklist, doSort),
                    .. GetAllATraitExtensions<T>(pawn, parentWhitelist, parentBlacklist, doSort)];
        }

        public static List<T> GetAllExtensionsOnBackStories<T>(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            List<BackstoryDef> backstories = [pawn.story?.Childhood, pawn.story?.Adulthood];
            List<T> extensions = [];
            foreach (var backstory in backstories)
            {
                if (backstory == null) continue;
                extensions.AddRange(ExtensionsOnDef<T, BackstoryDef>(backstory, parentWhitelist, parentBlacklist, doSort));
            }
            return extensions;
        }


        public static List<T> GetHediffExtensions<T>(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            List<ModExtWrapper<T>> matches = GetAllMatchingExtensionsFromHediffSetWithSource<T>(pawn);
            return GetFilteredResult(matches, parentWhitelist, parentBlacklist, doSort);
        }

        public static List<T> GetActiveGeneExtensions<T>(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            List<ModExtWrapper<T>> matches = GetAllMatchingExtensionsFromGenes<T>(pawn, true);
            return GetFilteredResult(matches, parentWhitelist, parentBlacklist, doSort);
        }

        /// <summary>
        /// Also includes inactive genes.
        /// </summary>
        public static List<T> GetAllGeneExtensions<T>(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            List<ModExtWrapper<T>> matches = GetAllMatchingExtensionsFromGenes<T>(pawn, true);
            return GetFilteredResult(matches, parentWhitelist, parentBlacklist, doSort);
        }

        public static List<T> GetAllActiveTraitExtensions<T>(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            List<T> extensions = [];
            if (pawn.story?.traits?.allTraits?.Any() != true) return extensions;
            return pawn.story.traits.allTraits.Where(x=>!x.Suppressed).Select(x=>x.def).ToList().ExtensionsOnDefList<T,TraitDef>(parentWhitelist, parentBlacklist, doSort);
        }

        public static List<T> GetAllATraitExtensions<T>(this Pawn pawn, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
        {
            List<T> extensions = [];
            if (pawn.story?.traits?.allTraits?.Any() != true) return extensions;
            return pawn.story.traits.allTraits.Select(x => x.def).ToList().ExtensionsOnDefList<T, TraitDef>(parentWhitelist, parentBlacklist, doSort);
        }

        // Private use by class.
        public class ModExtWrapper<T>(T extension, Type sourceType, int priority) where T : DefModExtension
        {
            public T extension = extension;
            public Type sourceType = sourceType;
            public int priority = priority;
        }

        //public static List<T> ExtensionsOnDefList<T>(this List<ThingDef> def, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true)
        //    where T : DefModExtension => ExtensionsOnDefList<T>(def, parentWhitelist, parentBlacklist, doSort);

        //public static List<T> ExtensionsOnDefList<T>(this List<HediffDef> def, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true)
        //    where T : DefModExtension => ExtensionsOnDefList<T>(def, parentWhitelist, parentBlacklist, doSort);

        //public static List<T> ExtensionsOnDefList<T>(this List<GeneDef> def, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true)
        //    where T : DefModExtension => ExtensionsOnDefList<T>(def, parentWhitelist, parentBlacklist, doSort);

        public static List<T> ExtensionsOnDefList<T, TDef>(this List<TDef> def, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true)
            where T : DefModExtension
            where TDef : Def
        {
            List<T> extensions = [];
            foreach (var d in def)
            {
                extensions.AddRange(ExtensionsOnDef<T, TDef>(d, parentWhitelist, parentBlacklist, doSort));
            }
            return extensions;
        }

        //public static List<T> ExtensionsOnDef<T>(this HediffDef hediffDef, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true)
        //    where T : DefModExtension => ExtensionsOnDef<T, HediffDef>(hediffDef, parentWhitelist, parentBlacklist, doSort);

        //public static List<T> ExtensionsOnDef<T>(this GeneDef geneDef, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true)
        //    where T : DefModExtension => ExtensionsOnDef<T, GeneDef>(geneDef, parentWhitelist, parentBlacklist, doSort);

        public static List<T> ExtensionsOnDef<T, TDef>(this TDef def, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true)
            where T : DefModExtension
            where TDef : Def
        {
            List<ModExtWrapper<T>> matches = GetAllMatchingExtensions<T>(def, def.GetType());
            return GetFilteredResult(matches, parentWhitelist, parentBlacklist, doSort);
        }

        private static List<T> GetFilteredResult<T>(List<ModExtWrapper<T>> matches, List<Type> parentWhitelist=null, List<Type> parentBlacklist = null, bool doSort=true)
            where T : DefModExtension
        {
            List<T> extensions = [];
            if (doSort)
            {
                matches.OrderByDescending(a => a.priority);
            }
            if (parentWhitelist == null && parentBlacklist == null)
            {
                return matches.Select(a => a.extension).ToList();
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

        private static List<ModExtWrapper<T>> GetAllMatchingExtensionsFromGenes<T>(Pawn pawn, bool active) where T : DefModExtension
        {
            List<ModExtWrapper<T>> extensions = [];
            if (pawn.genes == null) return extensions;
            var genes = active ? GeneHelpers.GetAllActiveGenes(pawn) : GeneHelpers.GetAllGenes(pawn).ToHashSet();
            foreach (var gene in genes)
            {
                extensions.AddRange(GetAllMatchingExtensions<T>(gene.def, gene.GetType()));
            }
            return extensions;
        }

        private static List<ModExtWrapper<T>> GetAllMatchingExtensions<T>(Def def, Type source) where T : DefModExtension
        {
            List<ModExtWrapper<T>> extensions = [];
            if (def?.modExtensions == null) return extensions;
            int defaultPriority = 0;
            if (source == typeof(RaceTracker))
            {
                defaultPriority = -100; // Ensures that stuff grabbing "FirstOrDefault" will not get this if there are other options.
            }
            else if (def is TraitDef trait)
            {
                defaultPriority = -10;
            }
            else if (def is GeneDef)
            {
                defaultPriority = 0;
            }
            else if (def is HediffDef)
            {
                defaultPriority = 100;
            }
            foreach (DefModExtension extension in def.modExtensions)
            {
                if (extension is T t)
                {
                    
                    extensions.Add(new ModExtWrapper<T>(t, source,
                        extension is PawnExtension p && p.priority != int.MinValue ? p.priority : defaultPriority));
                }
            }
            return extensions;
        }
    }
}
