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

        [ThreadStatic]
        private static BSCache _placeholderCache;
        [PrepatcherField]
        //[ValueInitializer("BigAndSmall.BSCache.GetDefaultCache")]
        [ValueInitializer(nameof(GetDefaultCache))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref BSCache GetCachePrepatched(this Pawn pawn)
        {
            _placeholderCache = GetCacheUltraSpeed(pawn, canRegenerate:false);
            return ref _placeholderCache;
        }
        private static BSCache GetDefaultCache() => BSCache.GetDefaultCache();

        //private static BSCache _placeholderCache2;
        //[PrepatcherField]
        //[ValueInitializer(nameof(BSCache.GetDefaultCache))]
        //public static ref BSCache GetCacheFast(this Pawn pawn)
        //{
        //    _placeholderCache2 = GetCache(pawn, canRegenerate: false);
        //    return ref _placeholderCache2;
        //}


    }
}
