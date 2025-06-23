using JetBrains.Annotations;
using Prepatcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static BigAndSmall.HumanoidPawnScaler;

namespace BigAndSmall
{
    public static class BSCacheExtensions
    {
        [ThreadStatic]
        private static BSCache _placeholderCache;
        [PrepatcherField]
        [ValueInitializer(nameof(BSCache.GetDefaultCache))]
        public static ref BSCache GetCachePrepatched(this Pawn pawn)
        {
            _placeholderCache = GetCacheUltraSpeed(pawn, canRegenerate:false);
            return ref _placeholderCache;
        }

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
