using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public interface ICacheable
    {
        bool RegenerateCache();
    }

    /// <summary>
    /// A quick method for making a cache without having to rewrite the same verbose code over and over.
    /// </summary>
    /// <typeparam name="T">The value you want to act as the key of the dictionary</typeparam>
    /// <typeparam name="V">A class whcih implements the ICachable Interface</typeparam>
    public abstract class DictCache<T, V> where V : ICacheable
    {
        public static ConcurrentDictionary<T, V> Cache { get; set; } = new();
        protected readonly static ConcurrentDictionary<T, V> JunkCache = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="forceRefresh"></param>
        /// <param name="canRegenerate">The Cache will not be regenerated, if one does not exist it will simply return default values.</param>
        /// <returns></returns>
        protected static V GetCacheInner(T key, out bool newEntry, bool forceRefresh=false, bool canRegenerate=true)
        {
            newEntry = false;
            if (key == null)
                return default;
            if (Cache.TryGetValue(key, out V data))
            {
                // Check if the cache has timed out
                if (forceRefresh)
                {
                    data.RegenerateCache();
                    return data;
                }
                else
                {
                    return data;
                }
            }
            if (!forceRefresh && JunkCache.TryGetValue(key, out V junkData))
            {
                return junkData;
            }
            else
            {
                newEntry = true;
                V newData = (V)Activator.CreateInstance(typeof(V), key);
                
                if (canRegenerate)
                {
                    bool result = newData.RegenerateCache();
                    if (!result && Cache.ContainsKey(key))  // If we failed to generate and there already is an entry, just use that.
                    {
                        return Cache[key];
                    }
                    Cache[key] = newData;
                    return newData;
                }
                else
                {
                    JunkCache[key] = newData;
                    return newData;
                }
            }
        }
    }

}
