using JetBrains.Annotations;
using Prepatcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static BigAndSmall.HumanoidPawnScaler;

namespace BigAndSmall
{
    public static class BSCacheExtensions
    {
        public static bool prepatched = false;

        private static BSCache GetDefaultCache() => BSCache.GetDefaultCache();

        [ThreadStatic]
        private static BSCache _placeholderCache;
        [ThreadStatic]
        private static BSCache _placeholderCacheThreaded;

        /// <summary>
        /// Gets the cache in the fastest way possible. Can generate a new cache if needed on creation but never refreshes it.
        /// </summary>
        [PrepatcherField]
        [ValueInitializer(nameof(GetDefaultCache))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref BSCache GetCachePrepatched(this Pawn pawn)
        {
            _placeholderCache = GetCacheUltraSpeed(pawn, canRegenerate: true);
            return ref _placeholderCache;
        }

        /// <summary>
        /// The threaded version of GetCache is for use on rendering threads where we DON'T want to regenerate the cache.
        /// </summary>
        [PrepatcherField]
        [ValueInitializer(nameof(GetDefaultCache))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref BSCache GetCachePrepatchedThreaded(this Pawn pawn)
        {
            _placeholderCacheThreaded = GetCacheUltraSpeed(pawn, canRegenerate:false);
            return ref _placeholderCacheThreaded;
        }
        

        


    }
}
